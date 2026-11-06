using System;
using System.Collections.Generic;
using GrowingCity;
static class SpecialChecks
{
 public static int Run(){int passed=0;Action<bool,string> a=(ok,msg)=>{if(!ok)throw new Exception(msg);passed++;Console.WriteLine("PASS: "+msg);};
 for(int tier=0;tier<5;tier++){var lot=new Lot(new Cell(tier,0),0,tier,tier+1);a(lot.Level==tier+1&&CityProgression.CanLaunchMissiles(lot)==(tier>=2),"missiles start at displayed level 3, tier "+tier);}
 a(!CityProgression.CanLaunchMissiles(null),"missing building cannot fire");
 a(CityProgression.GrowthRate(0)==1&&CityProgression.GrowthRate(60)==2&&CityProgression.GrowthRate(180)==4,"time accelerates growth");
 a(CityProgression.GrowthRate(420)==8&&CityProgression.GrowthRate(100000)==8,"automatic growth capped at eight");a(CityProgression.GrowthRate(-10)==1,"growth before start stays at one");
 bool monotone=true;for(int i=0;i<1000;i++)monotone&=CityProgression.GrowthRate(i+1)>=CityProgression.GrowthRate(i);a(monotone,"1000 automatic growth monotonic samples");
 var growing=new CitySimulation(1){AutomaticGrowth=true};growing.Start();int early=0,late=0;for(int i=1;i<=900;i++){var added=growing.Step(i*.1);if(added!=null){if(i<=300)early++;if(i>600)late++;}}a(late>early,"later time window builds more buildings");a(new CitySimulation{AutomaticGrowth=true}.CurrentGrowthRate(0)==1,"reset automatic growth rate");
 var generated=new CitySimulation(1){EnableCombinations=true,EnableSpecialBuildings=true};generated.Start();var kinds=new HashSet<BuildingKind>();for(int i=1;i<36;i++){var lot=generated.Step(i*2);if(lot.IsSpecial){kinds.Add(lot.Kind);a(lot.MaxHealth==60&&lot.Level==1,"volatile buildings have level1 and 60hp");}}a(kinds.Count==3,"all three special kinds appear in first36 births");
 var city=new CitySimulation(3);city.Start();var fuel=city.Build(new Cell(10,0),false,BuildingKind.FuelDepot);var ammo=city.Build(new Cell(12,0),false,BuildingKind.AmmunitionDepot);var power=city.Build(new Cell(14,0),false,BuildingKind.PowerStation);city.Build(new Cell(11,0),false,BuildingKind.HouseA);city.Build(new Cell(15,0),false,BuildingKind.HouseA);
 a(fuel.GoldValue==250&&ammo.GoldValue==350&&power.GoldValue==500,"special gold values");var hit=city.DamageBlast(10,0,.1,1,60);
 a(hit.Explosions.Count==3&&hit.Explosions[0].Depth==1&&hit.Explosions[1].Depth==2&&hit.Explosions[2].Depth==3,"fuel ammo power cascade has three depths");
 a(hit.Destroyed.Count==5&&hit.Hit.Count==5&&city.Buildings.Count==1,"chain damages outside original radius and preserves distant house");
 a(hit.Points==2600&&city.Gold==2600&&city.Score==2600,"one combo payout across full chain");a(new HashSet<Cell>(hit.DestroyedCells).Count==hit.DestroyedCells.Count,"no duplicate destroyed cells");a(city.DevelopmentScore==10&&hit.DevelopmentLost>0,"chain subtracts development accurately");a(city.Ruins.Count==5,"chain creates five rebuildable ruins");
 var repeat=city.DamageBlast(10,0,5,2,1000);a(repeat.Points==0&&repeat.Explosions.Count==0&&city.Gold==2600,"dead specials never explode or pay twice");var rebuilt=city.Build(new Cell(10,0),true,BuildingKind.FuelDepot);a(rebuilt.Health==60&&rebuilt.Generation!=fuel.Generation,"rebuilt special is a fresh volatile building");a(city.DamageBlast(10,0,.1,3,60).Explosions.Count==1,"rebuilt special can explode once again");
 var partial=new CitySimulation();partial.Start();var p=partial.Build(new Cell(9,9),false,BuildingKind.FuelDepot);a(partial.DamageBlast(9,9,.1,0,59).Explosions.Count==0&&p.Health==1,"nonlethal hit does not trigger special");a(partial.DamageBlast(9,9,.1,1,1).Explosions.Count==1,"last health point triggers special");
 var all=new CitySimulation();all.Start();all.Build(new Cell(1,0),false,BuildingKind.FuelDepot);all.Build(new Cell(2,0),false,BuildingKind.AmmunitionDepot);var instant=all.Blast(1,0,10,0);a(instant.Destroyed.Count==3&&instant.Explosions.Count==2,"overlapping initial blast triggers each special once");
 var combined=new CitySimulation{EnableCombinations=true};combined.Start();combined.Build(new Cell(1,0),false,BuildingKind.HouseB);combined.Build(new Cell(0,1),false,BuildingKind.HouseA);combined.Build(new Cell(1,1),false,BuildingKind.HouseB);combined.Build(new Cell(2,0),false,BuildingKind.FuelDepot);var shrink=combined.DamageBlast(2,0,.1,0,60);
 a(shrink.Downgrades.Count==1&&shrink.Downgrades[0].After.Health==135&&shrink.Downgrades[0].After.Tier==1,"chain hits a four-cell tower only once per wave and downgrades once");
 a(shrink.Destroyed.Count==1&&shrink.Hit.Count==2,"multi-cell chain preserves unique building hit accounting");
 a(typeof(CitySimulation).GetMethod("BuyGrowthUpgrade")==null&&typeof(BombardmentUpgrades).GetProperty("GrowthCost")==null,"manual growth purchase API removed");
 return passed;}
}
