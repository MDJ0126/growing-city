using System;
using System.Collections.Generic;
using GrowingCity;
static class CombatChecks
{
    public static int Run()
    {
        int passed=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name);};
        var city=new CitySimulation(19){EnableCombinations=true};var house=city.Build(new Cell(0,0),false,BuildingKind.HouseA);
        check(house.MaxHealth==100&&house.Health==100,"basic buildings start at 100 health");
        var apartment=city.Build(new Cell(1,0),false,BuildingKind.HouseB);
        check(apartment.MaxHealth==150&&apartment.Health==150,"first evolution has 1.5 times base health");
        city.Build(new Cell(0,1),false,BuildingKind.HouseA);var tower=city.Build(new Cell(1,1),false,BuildingKind.HouseB);
        check(tower.MaxHealth==225&&tower.Health==225,"second evolution compounds to 225 health");
        var hit=city.DamageBlast(.5,.5,2,0,60);
        check(hit.Hit.Count==1&&tower.Health==165,"one blast damages a multi-cell building only once");
        check(hit.Destroyed.Count==0&&hit.Downgrades.Count==0&&hit.Points==0&&city.Score==0,"surviving damage grants no gold or destruction reward");
        hit=city.DamageBlast(.5,.5,2,1,60);var small=hit.Downgrades[0].After;
        check(small.Tier==1&&small.Kind==BuildingKind.Apartment&&small.Health==105&&small.Cells.Count==2,"crossing 150 health restores an actual two-cell apartment");
        check(city.Living.Count==2&&city.Ruins.Count==2&&hit.Downgrades[0].FreedCells.Count==2,"downgrade releases only the discarded footprint into recovery");
        check(hit.DevelopmentLost==60&&city.DevelopmentScore==40&&hit.Points==0,"downgrade lowers development without granting gold");
        hit=city.DamageBlast(small.Cell.X,small.Cell.Z,.1,2,60);small=hit.Downgrades[0].After;
        check(small.Tier==0&&small.Kind==BuildingKind.HouseA&&small.Health==45&&small.Cells.Count==1,"crossing 100 health restores the original one-cell house");
        check(small.GoldValue==tower.Value&&city.Ruins.Count==3,"downgrade preserves the original evolved reward value");
        hit=city.DamageBlast(small.Cell.X,small.Cell.Z,.1,3,60);
        check(hit.Destroyed.Count==1&&hit.Points==tower.Value&&city.Score==tower.Value&&city.TotalDestroyed==1,"final destruction grants original gold exactly once");
        check(city.Living.Count==0&&city.Ruins.Count==4&&city.DevelopmentScore==0,"final destruction clears remaining occupancy and development");
        check(city.DamageBlast(0,0,4,4,500).Points==0,"repeated hits on rubble never pay gold twice");
        check(city.Step(20)!=null&&city.Living.Count==1,"freed and destroyed footprints can rebuild naturally");
        var skip=new CitySimulation(5){EnableCombinations=true};skip.Build(new Cell(0,0),false,BuildingKind.HouseA);skip.Build(new Cell(1,0),false,BuildingKind.HouseB);skip.Build(new Cell(0,1),false,BuildingKind.HouseA);var upper=skip.Build(new Cell(1,1),false,BuildingKind.HouseB);
        var nuke=skip.DamageBlast(0,0,.1,0,150);
        check(nuke.Downgrades.Count==1&&nuke.Downgrades[0].After.Tier==0&&nuke.Downgrades[0].After.Health==75&&nuke.Downgrades[0].FreedCells.Count==3,"large nonlethal damage may cross two evolution thresholds without healing");
        var survivor=nuke.Downgrades[0].After;skip.Build(new Cell(1,0),false,BuildingKind.HouseB);
        check(skip.Living[new Cell(0,0)].Generation==survivor.Generation&&skip.TotalEvolutions==3,"damaged downgraded buildings cannot instantly re-evolve to refill health");
        var edge=new CitySimulation();edge.Build(new Cell(0,0),false);var boundary=edge.DamageBlast(0,0,.1,0,100);
        check(boundary.Destroyed.Count==1&&boundary.Points==100,"exactly zero health destroys a basic building");
        bool rejected=false;try{edge.DamageBlast(0,0,1,0,double.NaN);}catch(ArgumentOutOfRangeException){rejected=true;}
        check(rejected&&edge.Score==100,"invalid damage is rejected without mutating gold");
        var stress=new CitySimulation(31){EnableCombinations=true};stress.Start();for(int i=1;i<=250;i++){
            stress.Step(i*2);stress.DamageBlast(0,0,2,i*2,17+i%4*20);
            var seen=new HashSet<Cell>();long expected=0;foreach(var lot in stress.Buildings.Values){if(lot.Health<=0||lot.Health>lot.MaxHealth)throw new Exception("Invalid live health");expected+=lot.DevelopmentValue;foreach(var cell in lot.Cells)if(!seen.Add(cell)||!object.ReferenceEquals(stress.Living[cell],lot)||stress.Ruins.ContainsKey(cell))throw new Exception("Invalid ownership");}foreach(var link in stress.SharedLinks)if(link.District)expected+=stress.DistrictLinkBonus;if(expected!=stress.DevelopmentScore)throw new Exception("Development mismatch");
        }
        check(true,"250 damage-growth cycles preserve health, exclusive occupancy and current development");
        return passed;
    }
}
