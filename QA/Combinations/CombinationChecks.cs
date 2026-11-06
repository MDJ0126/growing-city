using System;
using System.Collections.Generic;
using GrowingCity;

static class CombinationChecks
{
    static int passed;
    static void Check(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name);}
    static CitySimulation City()=>new CitySimulation(11){EnableCombinations=true};
    public static int Run()
    {
        var city=City();city.Build(new Cell(0,0),false,BuildingKind.HouseA);
        Check(city.DevelopmentScore==10,"one new house contributes live development value");
        var apartment=city.Build(new Cell(1,0),false,BuildingKind.HouseB);
        Check(apartment.Kind==BuildingKind.Apartment && apartment.Tier==1,"adjacent House A + House B evolves to Apartment");
        Check(city.Buildings.Count==1 && city.Living.Count==2 && apartment.Cells.Count==2,"evolution replaces two entities but retains both occupied cells");
        Check(object.ReferenceEquals(city.Living[new Cell(0,0)],city.Living[new Cell(1,0)]),"both footprint cells refer to the same evolved building");
        Check(city.DevelopmentScore==40,"evolution development counts the new building exactly once");
        city.Build(new Cell(0,1),false,BuildingKind.HouseA);var tower=city.Build(new Cell(1,1),false,BuildingKind.HouseB);
        Check(tower.Kind==BuildingKind.Tower && tower.Cells.Count==4 && city.Buildings.Count==1,"two aligned apartments evolve into one 2x2 higher-level tower");
        Check(city.DevelopmentScore==100 && city.TotalEvolutions==3,"two apartment evolutions and one tower evolution have correct live value");
        var destroyed=city.Blast(1,1,.1,0);
        Check(destroyed.Destroyed.Count==1 && destroyed.DestroyedCells.Count==4,"hitting any tower cell destroys one building and clears its whole footprint");
        Check(city.DevelopmentScore==0 && destroyed.DevelopmentLost==100,"full evolved destruction subtracts development to zero");
        Check(city.Score==tower.Value && city.Blast(1,1,.1,0).Points==0,"multi-cell building awards destruction points only once");
        var recovered=city.Step(6);Check(recovered!=null && city.DevelopmentScore>0 && city.Ruins.Count==3,"ruined composite footprint rebuilds gradually and development returns");
        var diagonal=City();diagonal.Build(new Cell(0,0),false,BuildingKind.HouseA);diagonal.Build(new Cell(1,1),false,BuildingKind.HouseB);
        Check(diagonal.Buildings.Count==2 && diagonal.TotalEvolutions==0,"diagonal buildings never merge");
        var mismatch=City();mismatch.Build(new Cell(0,0),false,BuildingKind.HouseA);mismatch.Build(new Cell(1,0),false,BuildingKind.HouseA);
        Check(mismatch.Buildings.Count==2,"an unregistered pair does not merge");
        var district=City();district.Build(new Cell(1,0),false,BuildingKind.Shop);district.Build(new Cell(2,0),false,BuildingKind.Office);
        Check(district.Buildings.Count==2 && district.DistrictCount==1,"Shop + Office remain two buildings in one connected district");
        Check(district.DevelopmentScore==44,"commercial set adds the configured shared-link bonus");
        var roads=RoadTopology.Build(district.Living.Keys,2,district.SharedLinks);
        Check(!roads.Vertical.Contains(new Cell(1,0)),"connected district removes the road separating its blocks");
        long before=district.DevelopmentScore;var hit=district.Blast(2,0,.1,0);
        Check(district.DevelopmentScore==14 && hit.DevelopmentLost==before-14 && district.DistrictCount==0,"destroying one set member removes its value and the connection bonus");
        Check(RoadTopology.Build(district.Living.Keys,2,district.SharedLinks).Vertical.Contains(new Cell(1,0)),"destroying the link restores the surviving block perimeter road");
        var bridge=City();bridge.Build(new Cell(0,0),false,BuildingKind.Office);bridge.Build(new Cell(1,0),false,BuildingKind.Shop);bridge.Build(new Cell(2,0),false,BuildingKind.Office);
        Check(bridge.DistrictCount==1 && bridge.DevelopmentScore==74,"a connected three-building set has one district and two bonuses");
        bridge.Blast(1,0,.1,0);Check(bridge.DistrictCount==0 && bridge.DevelopmentScore==36,"destroying a middle member splits the set and removes both bonuses");
        var automatic=City();automatic.Start();long last=automatic.DevelopmentScore;
        for(int i=1;i<=100;i++){automatic.Step(i*2);if(automatic.DevelopmentScore<=last)throw new Exception("Development failed to increase on growth");last=automatic.DevelopmentScore;var cells=new HashSet<Cell>();long expected=0;foreach(var lot in automatic.Buildings.Values){expected+=lot.DevelopmentValue;foreach(var cell in lot.Cells){if(!cells.Add(cell)||!object.ReferenceEquals(automatic.Living[cell],lot))throw new Exception("Footprint overlap or owner mismatch");}}foreach(var link in automatic.SharedLinks)if(link.District)expected+=automatic.DistrictLinkBonus;if(expected!=automatic.DevelopmentScore)throw new Exception("Live development mismatch");}
        Check(automatic.TotalEvolutions>0 && automatic.DistrictCount>0,"automatic growth produces both evolutions and connected sets");
        Check(true,"100 growth events preserve nonoverlapping ownership and live development accounting");
        for(int i=101;i<150;i++){automatic.Blast(0,0,3,i*2);automatic.Step(i*2+6);if(automatic.DevelopmentScore<0)throw new Exception("Negative development");}
        Check(true,"repeated destruction and regrowth never produce negative development");
        return passed;
    }
}
