using System;
using System.Collections.Generic;
using System.Linq;
using GrowingCity;
static class GrowthChecks
{
 static CitySimulation Create(int seed=104729)=>new CitySimulation(seed){EnableCombinations=true,EnableSpecialBuildings=true,AutomaticGrowth=true,ScatteredGrowth=true,ProgressiveBirthLevels=true,MapSize=24,GrowthBatchSize=4};
 public static int Run(){int passed=0;Action<bool,string> a=(v,m)=>{if(!v)throw new Exception(m);passed++;Console.WriteLine("PASS: "+m);};
 var c=Create();c.SeedCity(96);a(c.Living.Count==96&&c.TotalBuilt==96,"starts with96 occupied cells without waiting");a(c.Buildings.Count>40&&c.Buildings.Count<96,"seeded city has existing combinations");a(c.Gold==0&&c.Score==0,"prebuilt city never grants free destruction rewards");a(c.Buildings.Values.Any(b=>b.IsSpecial),"seeded city includes volatile targets");Console.WriteLine("INITIAL CITY: "+c.Buildings.Count+" buildings / "+c.Living.Count+" cells / "+c.TotalEvolutions+" evolutions");
 var same=Create();same.SeedCity(96);string Snapshot(CitySimulation x)=>string.Join(";",x.Living.OrderBy(p=>p.Key.X).ThenBy(p=>p.Key.Z).Select(p=>p.Key+":"+p.Value.Kind+":"+p.Value.Style));a(Snapshot(c)==Snapshot(same),"identical seed produces identical prepared city");
 var before=new HashSet<Cell>(c.Living.Keys);a(c.Step(1.2)!=null&&c.LastBatchCount==4&&c.Living.Count==100,"one growth tick adds4 cells");var born=c.Living.Keys.Where(p=>!before.Contains(p)).ToList();a(born.Count==4,"batch never duplicates a cell");bool spread=false;foreach(var x in born)foreach(var y in born)spread|=Math.Abs(x.X-y.X)+Math.Abs(x.Z-y.Z)>=4;a(spread,"batch appears at separated city edges");
 a(c.InsideMap(new Cell(-12,-12))&&c.InsideMap(new Cell(11,11))&&!c.InsideMap(new Cell(12,0))&&!c.InsideMap(new Cell(0,-13)),"24x24 map boundaries exact");bool rejected=false;try{c.Build(new Cell(12,0),false);}catch(ArgumentOutOfRangeException){rejected=true;}a(rejected,"direct out of bounds construction rejected");
 for(int i=2;i<2500;i++)c.Step(i*2);a(c.Living.Count==576&&c.Living.Keys.All(c.InsideMap),"long growth fills576 cells and never leaves map");a(c.Step(10000)==null&&c.LastBatchCount==0,"full map stops new construction");
 var blast=c.DamageBlast(0,0,1.5,10000,10000);a(blast.DestroyedCells.Count>0&&c.Living.Count<576,"full map remains destructible");a(c.Step(10001)==null,"full map holes respect recovery delay");c.Step(10010);a(c.LastBatchCount==4&&c.TotalRebuilt>=4,"full map damage recovers in batches");for(int i=0;i<300;i++)c.Step(10012+i*2);a(c.Living.Count==576,"destroyed map returns to full capacity");
 var capped=Create();capped.MaxLiving=98;capped.SeedCity(96);capped.Step(2);a(capped.LastBatchCount==2&&capped.Living.Count==98,"batch respects an optional smaller population limit");
 a(CityProgression.BirthTier(0,.01)==0&&CityProgression.BirthTier(90,.01)==0,"early construction remains basic");a(CityProgression.BirthTier(300,.1)==2&&CityProgression.BirthTier(300,.8)==1&&CityProgression.BirthTier(300,.99)==0,"late construction favors75percent level3 and20percent level2");
 var late=Create();late.Start();late.Step(300);a(late.Buildings.Values.Any(b=>b.Tier>0),"actual late growth creates advanced buildings directly");a(late.CurrentGrowthRate(300)==6,"batch growth preserves time-based acceleration");
 var wiped=Create();wiped.SeedCity(96);wiped.Blast(0,0,100,0);a(wiped.Living.Count==0&&wiped.Step(1.2)==null,"total destruction still waits for rebuild delay");wiped.Step(10);a(wiped.Living.Count==4&&wiped.TotalRebuilt==4,"total destruction recovers4 cells without resetting");
 var connected=Create(37);connected.SeedCity(96);var visited=new HashSet<Cell>();var q=new Queue<Cell>();var first=connected.Living.Keys.First();q.Enqueue(first);visited.Add(first);while(q.Count>0){var current=q.Dequeue();foreach(var next in CitySimulation.Adjacent(current))if(connected.Living.ContainsKey(next)&&visited.Add(next))q.Enqueue(next);}a(visited.Count==96,"irregular prepared city stays road-connectable");
 return passed;}
}
