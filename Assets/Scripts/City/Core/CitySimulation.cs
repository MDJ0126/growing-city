using System;
using System.Collections.Generic;

namespace GrowingCity
{
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X, Z;
        public Cell(int x, int z) { X=x; Z=z; }
        public bool Equals(Cell other) => X==other.X && Z==other.Z;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => unchecked(X*73856093 ^ Z*19349663);
        public Cell Offset(int x,int z) => new Cell(X+x,Z+z);
        public override string ToString() => X+","+Z;
    }
    public sealed class Lot
    {
        public readonly Cell Cell;
        public readonly int Style, Tier, Value, Generation;
        public readonly BuildingKind Kind;
        public readonly IReadOnlyList<Cell> Cells;
        public int DevelopmentValue {get;internal set;}
        public int DistrictId {get;internal set;}
        public int Level=>Tier+1;
        public bool IsSpecial=>CityProgression.IsSpecial(Kind);
        public double MaxHealth => IsSpecial?60:100*Math.Pow(1.5,Tier);
        public double Health {get;internal set;}
        public int GoldValue {get;internal set;}
        public bool HasTakenDamage {get;internal set;}
        public readonly IReadOnlyList<Lot> Components;
        public Lot(Cell cell,int style,int tier,int generation,BuildingKind kind=BuildingKind.HouseA,IEnumerable<Cell> footprint=null,int developmentValue=0,IEnumerable<Lot> components=null) {
            Cell=cell;Style=style;Tier=tier;Generation=generation;Kind=kind;
            var cells=footprint==null?new List<Cell>{cell}:new List<Cell>(footprint);Cells=cells.AsReadOnly();
            Value=100*cells.Count+tier*75;DevelopmentValue=developmentValue>0?developmentValue:kind==BuildingKind.HouseA?10:kind==BuildingKind.HouseB?12:kind==BuildingKind.Shop?14:kind==BuildingKind.Office?18:kind==BuildingKind.Apartment?40:100;
            Health=MaxHealth;GoldValue=IsSpecial?(kind==BuildingKind.FuelDepot?250:kind==BuildingKind.AmmunitionDepot?350:500):Value;Components=new List<Lot>(components??Array.Empty<Lot>()).AsReadOnly();
        }
    }
    public sealed class BlastResult
    {
        public readonly List<Lot> Destroyed = new List<Lot>();
        public long Points;
        public int Multiplier;
        public readonly List<Cell> DestroyedCells=new List<Cell>();
        public long DevelopmentLost;
        public readonly List<Lot> Hit=new List<Lot>();
        public readonly List<ChainExplosion> Explosions=new List<ChainExplosion>();
        public readonly List<BuildingDowngrade> Downgrades=new List<BuildingDowngrade>();
    }
    public sealed class BuildingDowngrade
    {
        public readonly Lot Before,After;
        public readonly IReadOnlyList<Cell> FreedCells;
        public BuildingDowngrade(Lot before,Lot after,List<Cell> freed){Before=before;After=after;FreedCells=freed.AsReadOnly();}
    }
    // Engine-independent simulation: all time comes from the caller, never wall-clock time.
    public sealed class CitySimulation
    {
        readonly Dictionary<Cell,Lot> living=new Dictionary<Cell,Lot>();
        readonly Dictionary<int,Lot> buildings=new Dictionary<int,Lot>();
        readonly Dictionary<Cell,double> ruins=new Dictionary<Cell,double>();
        readonly HashSet<Cell> frontier=new HashSet<Cell>();
        readonly Random random;
        int generation;
        Cell lastPlacement;
        double nextGrowth,startTime;
        double currentTime;
        public int MapSize {get;set;}
        public int GrowthBatchSize {get;set;}=1;
        public bool ScatteredGrowth {get;set;}
        public bool ProgressiveBirthLevels {get;set;}
        public int LastBatchCount {get;private set;}
        public bool InsideMap(Cell c)=>MapSize<=0||(c.X>=-MapSize/2&&c.X<-MapSize/2+MapSize&&c.Z>=-MapSize/2&&c.Z<-MapSize/2+MapSize);
        public bool AutomaticGrowth {get;set;}
        public bool EnableSpecialBuildings {get;set;}
        public double CurrentGrowthRate(double now)=>AutomaticGrowth?CityProgression.GrowthRate(now-startTime):1;
        public IReadOnlyDictionary<Cell,Lot> Living => living;
        public IReadOnlyDictionary<int,Lot> Buildings => buildings;
        public IReadOnlyDictionary<Cell,double> Ruins => ruins;
        public long Score {get;private set;}
        public long Gold {get;private set;}
        public BombardmentUpgrades Upgrades {get;}=new BombardmentUpgrades();
        public bool BuyRadiusUpgrade(){long cost=Upgrades.RadiusCost;if(cost<=0||Gold<cost)return false;Gold-=cost;Upgrades.RaiseRadius();return true;}
        public bool BuyBombUpgrade(){long cost=Upgrades.BombCost;if(cost<=0||Gold<cost)return false;Gold-=cost;Upgrades.RaiseBombs();return true;}
        public bool BuyAircraftUpgrade(){long cost=Upgrades.AircraftCost;if(cost<=0||Gold<cost)return false;Gold-=cost;Upgrades.RaiseAircraft();return true;}
        public bool BuyDropRateUpgrade(){long cost=Upgrades.DropRateCost;if(cost<=0||Gold<cost)return false;Gold-=cost;Upgrades.RaiseDropRate();return true;}
        public bool BuyFormationUpgrade(){long cost=Upgrades.FormationCost;if(cost<=0||Gold<cost)return false;Gold-=cost;Upgrades.RaiseFormation();return true;}
        public bool BuySpeedUpgrade(){long cost=Upgrades.SpeedCost;if(cost<=0||Gold<cost)return false;Gold-=cost;Upgrades.RaiseSpeed();return true;}
        public bool BuyArmorUpgrade(){long cost=Upgrades.ArmorCost;if(cost<=0||Gold<cost)return false;Gold-=cost;Upgrades.RaiseArmor();return true;}
        public bool BuyHealthUpgrade(){long cost=Upgrades.HealthCost;if(cost<=0||Gold<cost)return false;Gold-=cost;Upgrades.RaiseHealth();return true;}
        public int TotalBuilt {get;private set;}
        public int TotalDestroyed {get;private set;}
        public int TotalRebuilt {get;private set;}
        public long DevelopmentScore {get;private set;}
        public int TotalEvolutions {get;private set;}
        public int DistrictCount {get;private set;}
        public bool EnableCombinations {get;set;}
        public int DistrictLinkBonus {get;set;}=12;
        public BuildingKind DistrictFirst {get;set;}=BuildingKind.Shop;
        public BuildingKind DistrictSecond {get;set;}=BuildingKind.Office;
        public IReadOnlyList<LotConnection> SharedLinks => sharedLinks.AsReadOnly();
        readonly List<LotConnection> sharedLinks=new List<LotConnection>();
        public IList<EvolutionRule> EvolutionRules {get;}=new List<EvolutionRule>{new EvolutionRule(BuildingKind.HouseA,BuildingKind.HouseB,BuildingKind.Apartment,1,40,2),new EvolutionRule(BuildingKind.Apartment,BuildingKind.Apartment,BuildingKind.Tower,2,100,4)};
        public double GrowthInterval {get;set;}=1.15;
        public double RecoveryDelay {get;set;}=4.5;
        public int MaxLiving {get;set;}=0; // 0 = no extra occupancy cap; MapSize still bounds construction.
        public CitySimulation(int seed=104729) { random=new Random(seed); frontier.Add(new Cell(0,0)); }
        public Lot Start(double now=0) { if(TotalBuilt!=0) throw new InvalidOperationException("Already started"); startTime=currentTime=now;nextGrowth=now+GrowthInterval; return Build(new Cell(0,0),false); }
        public void SeedCity(int cells,double now=0){Start(now);for(int i=1;i<cells;i++){if(MaxLiving>0&&living.Count>=MaxLiving)break;var added=GrowOne(now,new List<Cell>());if(added==null)break;}LastBatchCount=0;}
        public Lot Step(double now)
        {
            if(double.IsNaN(now)||double.IsInfinity(now)) throw new ArgumentOutOfRangeException(nameof(now));
            currentTime=now;LastBatchCount=0;
            if(now<nextGrowth || (MaxLiving>0 && living.Count>=MaxLiving)) return null;
            nextGrowth=now+Math.Max(.05,GrowthInterval/CurrentGrowthRate(now));
            Lot last=null;var selected=new List<Cell>();for(int i=0;i<Math.Max(1,Math.Min(8,GrowthBatchSize));i++){if(MaxLiving>0&&living.Count>=MaxLiving)break;var added=GrowOne(now,selected);if(added==null)break;last=added;selected.Add(lastPlacement);LastBatchCount++;}return last;
        }
        Lot GrowOne(double now,List<Cell> selected)
        {
            bool found=false; Cell best=default; double bestDistance=double.MaxValue;
            foreach(var ruin in ruins) if(ruin.Value<=now) {
                double priority=ruin.Key.X*(double)ruin.Key.X+ruin.Key.Z*(double)ruin.Key.Z;
                if(priority<bestDistance) {bestDistance=priority;best=ruin.Key;found=true;}
            }
            if(found) return Build(best,true);
            if(frontier.Count==0) return null; // Total destruction waits for actual recovery, never resets the city.
            if(ScatteredGrowth){
                var candidates=new List<Cell>(frontier);candidates.Sort((a,b)=>a.X!=b.X?a.X.CompareTo(b.X):a.Z.CompareTo(b.Z));
                // A compactness bias still permits fingers, gaps and growth from different edges.
                double total=0;var weights=new double[candidates.Count];
                for(int i=0;i<candidates.Count;i++){var c=candidates[i];int neighbors=0;foreach(var adjacent in Adjacent(c))if(living.ContainsKey(adjacent))neighbors++;double separation=1;foreach(var previous in selected)if(Math.Abs(previous.X-c.X)+Math.Abs(previous.Z-c.Z)<4)separation*=.08;weights[i]=(.35+neighbors*neighbors)*separation;total+=weights[i];}
                double choice=random.NextDouble()*total;for(int i=0;i<candidates.Count;i++){choice-=weights[i];if(choice<=0)return Build(candidates[i],false);}return Build(candidates[candidates.Count-1],false);
            }
            bestDistance=double.MaxValue;
            foreach(var cell in frontier) {
                double priority=cell.X*(double)cell.X+cell.Z*(double)cell.Z+random.NextDouble()*3.5;
                if(priority<bestDistance) {bestDistance=priority;best=cell;found=true;}
            }
            return found?Build(best,false):null;
        }
        internal Lot Build(Cell cell,bool rebuilding,BuildingKind? requestedKind=null)
        {
            if(!InsideMap(cell))throw new ArgumentOutOfRangeException(nameof(cell));
            if(living.ContainsKey(cell))throw new InvalidOperationException("Occupied building cell");
            lastPlacement=cell;
            int tier=TotalBuilt<6?0:TotalBuilt<22?random.Next(2):random.Next(3);
            var kind=requestedKind??(EnableCombinations?(BuildingKind)(TotalBuilt%4):tier==0?BuildingKind.HouseA:tier==1?BuildingKind.Apartment:BuildingKind.Tower);
            if(!requestedKind.HasValue&&EnableSpecialBuildings&&TotalBuilt>=11&&(TotalBuilt+1)%12==0)kind=(BuildingKind)((int)BuildingKind.FuelDepot+((TotalBuilt+1)/12-1)%3);
            int birthTier=ProgressiveBirthLevels&&!requestedKind.HasValue?CityProgression.BirthTier(currentTime-startTime,random.NextDouble()):0;
            if(birthTier>0&&!CityProgression.IsSpecial(kind)){tier=birthTier;kind=tier==2?BuildingKind.Tower:BuildingKind.Apartment;}
            else if(CityProgression.IsSpecial(kind)||EnableCombinations)tier=0;
            var lot=new Lot(cell,random.Next(100000),tier,++generation,kind);
            
            living.Add(cell,lot);buildings.Add(lot.Generation,lot);ruins.Remove(cell);frontier.Remove(cell);
            TotalBuilt++; if(rebuilding)TotalRebuilt++;
            AddFrontier(cell.Offset(1,0));AddFrontier(cell.Offset(-1,0));AddFrontier(cell.Offset(0,1));AddFrontier(cell.Offset(0,-1));
            if(EnableCombinations)lot=Evolve(lot);
            RefreshDevelopment();
            return lot;
        }
        Lot Evolve(Lot added)
        {
            bool again=true;
            while(again){again=false;var neighbors=new SortedDictionary<int,Lot>();
                foreach(var cell in added.Cells)foreach(var adjacent in Adjacent(cell))if(living.TryGetValue(adjacent,out var other)&&other.Generation!=added.Generation)neighbors[other.Generation]=other;
                foreach(var rule in EvolutionRules){foreach(var other in neighbors.Values){
                    if(added.HasTakenDamage||other.HasTakenDamage||!rule.Matches(added.Kind,other.Kind))continue;
                    var cells=new List<Cell>(added.Cells);cells.AddRange(other.Cells);
                    if(cells.Count>rule.MaximumArea || !Rectangular(cells))continue;
                    var anchor=cells[0];foreach(var c in cells)if(c.X<anchor.X || (c.X==anchor.X&&c.Z<anchor.Z))anchor=c;
                    buildings.Remove(added.Generation);buildings.Remove(other.Generation);
                    int value=Math.Max(rule.DevelopmentValue,added.DevelopmentValue+other.DevelopmentValue+1);
                    var evolved=new Lot(anchor,added.Style,rule.Tier,++generation,rule.Result,cells,value,new[]{added,other});
                    foreach(var c in cells)living[c]=evolved;buildings.Add(evolved.Generation,evolved);added=evolved;TotalEvolutions++;again=true;break;
                }if(again)break;}
            }
            return added;
        }
        static bool Rectangular(List<Cell> cells)
        {
            int minX=int.MaxValue,minZ=int.MaxValue,maxX=int.MinValue,maxZ=int.MinValue;
            foreach(var c in cells){minX=Math.Min(minX,c.X);minZ=Math.Min(minZ,c.Z);maxX=Math.Max(maxX,c.X);maxZ=Math.Max(maxZ,c.Z);}
            return (maxX-minX+1)*(maxZ-minZ+1)==cells.Count && maxX-minX<2 && maxZ-minZ<2;
        }
        internal static IEnumerable<Cell> Adjacent(Cell c){yield return c.Offset(1,0);yield return c.Offset(-1,0);yield return c.Offset(0,1);yield return c.Offset(0,-1);}
        void RefreshDevelopment()
        {
            sharedLinks.Clear();DevelopmentScore=0;DistrictCount=0;
            var graph=new Dictionary<int,List<int>>();foreach(var lot in buildings.Values){DevelopmentScore+=lot.DevelopmentValue;lot.DistrictId=0;}
            if(!EnableCombinations)return;
            foreach(var item in living)foreach(var offset in new[]{new Cell(1,0),new Cell(0,1)}){
                var next=item.Key.Offset(offset.X,offset.Z);if(!living.TryGetValue(next,out var other))continue;
                bool same=item.Value.Generation==other.Generation;
                bool district=!same&&((item.Value.Kind==DistrictFirst&&other.Kind==DistrictSecond)||(item.Value.Kind==DistrictSecond&&other.Kind==DistrictFirst));
                if(!same&&!district)continue;sharedLinks.Add(new LotConnection(item.Key,next,district));
                if(!district)continue;DevelopmentScore+=Math.Max(0,DistrictLinkBonus);
                if(!graph.TryGetValue(item.Value.Generation,out var a))graph[item.Value.Generation]=a=new List<int>();a.Add(other.Generation);
                if(!graph.TryGetValue(other.Generation,out var b))graph[other.Generation]=b=new List<int>();b.Add(item.Value.Generation);
            }
            var seen=new HashSet<int>();foreach(var id in graph.Keys){if(!seen.Add(id))continue;DistrictCount++;var queue=new Queue<int>();queue.Enqueue(id);while(queue.Count>0){int current=queue.Dequeue();buildings[current].DistrictId=id;foreach(var next in graph[current])if(seen.Add(next))queue.Enqueue(next);}}
        }
        void AddFrontier(Cell cell) { if(InsideMap(cell)&&!living.ContainsKey(cell) && !ruins.ContainsKey(cell)) frontier.Add(cell); }
        public BlastResult Blast(double x,double z,double radius,double now)
            => ResolveBlast(x,z,radius,now,null);
        public BlastResult DamageBlast(double x,double z,double radius,double now,double damage)
        {
            if(damage<=0||double.IsNaN(damage)||double.IsInfinity(damage))throw new ArgumentOutOfRangeException(nameof(damage));
            return ResolveBlast(x,z,radius,now,damage);
        }
        BlastResult ResolveBlast(double x,double z,double radius,double now,double? damage)
        {
            if(radius<=0 || double.IsNaN(radius)||double.IsInfinity(radius)||double.IsNaN(x)||double.IsNaN(z)||double.IsInfinity(x)||double.IsInfinity(z)||double.IsNaN(now)||double.IsInfinity(now)) throw new ArgumentOutOfRangeException(nameof(radius));
            var result=new BlastResult();
            long previousDevelopment=DevelopmentScore;var hitIds=new HashSet<int>();
            var waves=new Queue<(double X,double Z,double Radius,double? Damage,int Depth)>();waves.Enqueue((x,z,radius,damage,0));
            while(waves.Count>0){var wave=waves.Dequeue();var waveHits=new HashSet<int>();
                foreach(var item in living){var lot=item.Value;if(lot.Health<=0)continue;double dx=item.Key.X-wave.X,dz=item.Key.Z-wave.Z;
                    if(dx*dx+dz*dz>wave.Radius*wave.Radius||!waveHits.Add(lot.Generation))continue;
                    if(hitIds.Add(lot.Generation))result.Hit.Add(lot);lot.HasTakenDamage=true;lot.Health=wave.Damage.HasValue?Math.Max(0,lot.Health-wave.Damage.Value):0;
                    if(lot.Health<=0&&lot.IsSpecial){var chain=new ChainExplosion(lot,wave.Depth+1);result.Explosions.Add(chain);waves.Enqueue((chain.Cell.X,chain.Cell.Z,chain.Radius,chain.Damage,chain.Depth));}
                }
            }
            foreach(var lot in result.Hit){
                if(lot.Health<=0){result.Destroyed.Add(lot);continue;}
                if(lot.Tier==0||lot.Health>100*Math.Pow(1.5,lot.Tier-1))continue;
                var smaller=lot;
                while(smaller.Tier>0&&lot.Health<=100*Math.Pow(1.5,smaller.Tier-1)){
                    Lot component=null;foreach(var c in smaller.Components)if(component==null||c.Cell.X<component.Cell.X||(c.Cell.X==component.Cell.X&&c.Cell.Z<component.Cell.Z))component=c;
                    int tier=smaller.Tier-1;
                    smaller=new Lot(component?.Cell??smaller.Cell,lot.Style,tier,++generation,component?.Kind??(tier==0?BuildingKind.HouseA:BuildingKind.Apartment),component?.Cells??new[]{smaller.Cell},component?.DevelopmentValue??(tier==0?10:40),component?.Components);
                }
                smaller.Health=lot.Health;smaller.HasTakenDamage=true;smaller.GoldValue=lot.GoldValue;
                buildings.Remove(lot.Generation);buildings.Add(smaller.Generation,smaller);var kept=new HashSet<Cell>(smaller.Cells);var freed=new List<Cell>();
                foreach(var cell in lot.Cells){if(kept.Contains(cell))living[cell]=smaller;else{living.Remove(cell);ruins[cell]=now+RecoveryDelay;freed.Add(cell);}}
                result.Downgrades.Add(new BuildingDowngrade(lot,smaller,freed));
            }
            result.Multiplier=Math.Min(5,1+result.Destroyed.Count/4);
            foreach(var lot in result.Destroyed) {buildings.Remove(lot.Generation);foreach(var cell in lot.Cells){living.Remove(cell);ruins[cell]=now+RecoveryDelay;result.DestroyedCells.Add(cell);}result.Points+=lot.GoldValue;}
            result.Points*=result.Multiplier; Score+=result.Points;Gold+=result.Points;TotalDestroyed+=result.Destroyed.Count;
            frontier.Clear();
            foreach(var cell in living.Keys) {AddFrontier(cell.Offset(1,0));AddFrontier(cell.Offset(-1,0));AddFrontier(cell.Offset(0,1));AddFrontier(cell.Offset(0,-1));}
            RefreshDevelopment();result.DevelopmentLost=previousDevelopment-DevelopmentScore;
            return result;
        }
    }
}
