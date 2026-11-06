using System;
using System.Collections.Generic;
using GrowingCity;

static class Checks
{
    static int passed;
    static void Assert(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name);}
    static bool Adjacent(Cell a,Cell b)=>Math.Abs(a.X-b.X)+Math.Abs(a.Z-b.Z)==1;
    static void Main()
    {
        passed+=GrowthChecks.Run();passed+=SpecialChecks.Run();passed+=MotionChecks.Run();
        var city=new CitySimulation(7);var first=city.Start();
        Assert(city.Living.Count==1 && first.Cell.Equals(new Cell(0,0)),"starts with exactly one central building");
        Assert(city.Step(.5)==null,"waits for the growth interval");
        double now=0;
        for(int i=0;i<70;i++) {var old=new List<Cell>(city.Living.Keys);now+=1.2;var added=city.Step(now);bool adjacent=false;foreach(var cell in old)adjacent|=Adjacent(cell,added.Cell);if(!adjacent)throw new Exception("Disconnected growth");}
        Assert(city.Living.Count==71,"70 growth events produce 71 distinct adjacent lots");
        var blast=city.Blast(0,0,1.05,now);
        Assert(blast.Destroyed.Count==5 && city.Living.Count==66,"local blast removes only five cells in its radius");
        long basePoints=0;foreach(var lot in blast.Destroyed)basePoints+=lot.Value;
        Assert(blast.Points==basePoints*blast.Multiplier && city.Score==blast.Points,"score derives from destroyed lots and combo multiplier");
        Assert(city.Blast(0,0,1.05,now).Points==0,"hitting the same rubble never duplicates score");
        var beforeRecovery=city.Step(now+1.2);Assert(beforeRecovery==null || !beforeRecovery.Cell.Equals(new Cell(0,0)),"ruins do not rebuild before recovery delay");
        var rebuilt=city.Step(now+6);Assert(rebuilt!=null && rebuilt.Cell.Equals(new Cell(0,0)) && city.TotalRebuilt==1,"eligible central ruin rebuilds before outward growth");
        Assert(city.Score==blast.Points,"reconstruction itself awards no destruction score");
        now+=7;
        var emptyCity=new CitySimulation(9);emptyCity.Start();for(int i=1;i<20;i++)emptyCity.Step(i*2);
        var wipe=emptyCity.Blast(0,0,1000,50);Assert(emptyCity.Living.Count==0 && wipe.Destroyed.Count>0,"entire city can be destroyed");
        Assert(emptyCity.Step(51.2)==null,"total destruction waits for recovery rather than resetting");
        Assert(emptyCity.Step(56)!=null && emptyCity.Living.Count==1,"city recovers even after total destruction");
        for(int i=0;i<80;i++){now+=7;city.Step(now);if(i%9==0)city.Blast(0,0,2.65,now);foreach(var cell in city.Living.Keys)if(city.Ruins.ContainsKey(cell))throw new Exception("Living/ruin overlap");}
        Assert(true,"repeated growth/destruction/recovery maintains exclusive lot states");
        var capped=new CitySimulation(){MaxLiving=2};capped.Start();capped.Step(2);Assert(capped.Step(4)==null && capped.Living.Count==2,"optional inspector limit is respected");
        var a=new CitySimulation(123);var b=new CitySimulation(123);a.Start();b.Start();bool deterministic=true;for(int i=1;i<50;i++){var x=a.Step(i*2);var y=b.Step(i*2);deterministic&=x.Cell.Equals(y.Cell)&&x.Style==y.Style;}Assert(deterministic,"fixed seeds reproduce city growth");
        bool refused=false;try{city.Blast(0,0,double.NaN,now);}catch(ArgumentOutOfRangeException){refused=true;}Assert(refused,"invalid radius is rejected");
        for(int mask=1;mask<16;mask++){var junction=RoadTopology.Resolve(mask);int basis=junction.Shape==RoadShape.Cross?15:junction.Shape==RoadShape.Tee?11:junction.Shape==RoadShape.End?1:junction.Shape==RoadShape.Straight?5:9;if(RoadTopology.Rotate(basis,junction.QuarterTurns)!=mask)throw new Exception("Road rotation mismatch");}
        Assert(true,"all 15 road masks select the correct topology and asset rotation");
        var singleRoad=RoadTopology.Build(new[]{new Cell(0,0)});Assert(singleRoad.Horizontal.Count==2 && singleRoad.Vertical.Count==2 && singleRoad.Junctions.Count==4,"single block has four deduplicated road edges");
        var sameBlock=RoadTopology.Build(new[]{new Cell(0,0),new Cell(1,0),new Cell(0,1),new Cell(1,1)});Assert(sameBlock.Horizontal.Count+sameBlock.Vertical.Count==4,"four dense lots share the same perimeter roads");
        var nextBlock=RoadTopology.Build(new[]{new Cell(0,0),new Cell(2,0)});int tees=0;foreach(var junction in nextBlock.Junctions.Values)if(junction.Shape==RoadShape.Tee)tees++;Assert(tees==2,"outward growth converts shared corners into two T junctions");
        Assert(RoadTopology.FloorDivide(-1,2)==-1 && RoadTopology.FloorDivide(-2,2)==-1 && RoadTopology.FloorDivide(-3,2)==-2,"negative city coordinates group into correct street blocks");
        var restoredRoad=RoadTopology.Build(new[]{new Cell(0,0)});int corners=0;foreach(var junction in restoredRoad.Junctions.Values)if(junction.Shape==RoadShape.Corner)corners++;Assert(corners==4,"removing the neighbor block restores four corners");
        var crossing=RoadTopology.Build(new[]{new Cell(0,0),new Cell(2,0),new Cell(0,2),new Cell(2,2)});Assert(crossing.Junctions[new Cell(1,1)].Shape==RoadShape.Cross,"four city blocks naturally form a central four-way crossing");
        Assert(RoadTopology.Build(Array.Empty<Cell>()).Junctions.Count==0,"total destruction leaves no orphaned road nodes");
        passed+=CombinationChecks.Run();
        passed+=AirstrikeChecks.Run();
        passed+=CombatChecks.Run();
        passed+=UpgradeChecks.Run();
        passed+=DirectionLevelChecks.Run();
        passed+=DragGuideChecks.Run();
        passed+=FormationCombatChecks.Run();
        Assert(GrowthFeedback.EvolutionLift(0)==0 && GrowthFeedback.EvolutionLift(1)==0,"evolution starts and ends on the ground");
        Assert(GrowthFeedback.EvolutionLift(.35f)>.99f && GrowthFeedback.EvolutionScale(.35f)>1.15f,"evolution has a visible jump and scale overshoot");
        Assert(GrowthFeedback.EvolutionScale(1)==1 && GrowthFeedback.EvolutionScale(1.2f)==1,"evolution settles at original size without drift");
        bool finite=true;for(int i=0;i<=1100;i++){float t=i/1000f;finite &= GrowthFeedback.EvolutionScale(t)>0 && GrowthFeedback.EvolutionLift(t)>=0 && !float.IsNaN(GrowthFeedback.EvolutionScale(t));}
        Assert(finite,"1101 timeline samples have positive scales and nonnegative height");
        Console.WriteLine("PASS: "+passed+" scenario checks. These checks do not claim Unity rendering or browser execution.");
    }
}
