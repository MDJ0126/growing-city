using System;
using System.Collections.Generic;
using GrowingCity;
static class FormationCombatChecks
{
    static void Fund(CitySimulation city){for(int x=100;x<108;x++)city.Build(new Cell(x,0),false,BuildingKind.Shop);city.Blast(103,0,12,0);}
    public static int Run()
    {
        int passed=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name);};
        var baseline=new AirstrikePlan(0,0,960,0,16,false,0,true,1);
        bool spaced=true;for(int i=1;i<baseline.BombCount;i++)spaced&=Math.Abs(baseline.ReleaseAt(i)-baseline.ReleaseAt(i-1)-1)<1e-8;
        check(spaced&&baseline.BombCount>1,"baseline aircraft releases exactly one bomb per second");
        var fastDrop=new AirstrikePlan(0,0,960,0,16,false,0,true,3);
        check(Math.Abs(fastDrop.ReleaseAt(1)-fastDrop.ReleaseAt(0)-1.0/3)<1e-8&&fastDrop.BombCount>baseline.BombCount,"drop-rate upgrade shortens intervals and increases timed drops");
        var quick=new AirstrikePlan(0,0,960,0,16,false,0,true,1,2);
        check(quick.Duration<baseline.Duration&&Math.Abs(quick.ReleaseAt(1)-quick.ReleaseAt(0)-1)<1e-8,"flight speed changes travel duration without changing bombs per second");
        var shortRun=new AirstrikePlan(0,0,8,0,16,false,0,true,1,2.2);
        check(shortRun.BombCount==1&&Math.Abs(shortRun.Fraction(0)-.5)<1e-8&&!double.IsNaN(shortRun.TargetX(0)),"short fast flights safely drop one centered bomb");
        check(!shortRun.Complete(shortRun.Duration)&&shortRun.Complete(shortRun.ImpactAt(0)+1),"cleanup waits for the final bomb even after fast aircraft exit");
        var leader=VFormation.Offset(0,20);var left=VFormation.Offset(1,20);var right=VFormation.Offset(2,20);var outer=VFormation.Offset(3,20);
        check(leader.X==0&&leader.Y==0&&left.X==-right.X&&left.Y==right.Y&&left.Y>0,"V formation puts the leader first and mirrors the first wing pair");
        check(outer.X<left.X&&outer.Y>left.Y,"additional aircraft extend the next V row behind the leader");
        var positions=new HashSet<string>();for(int i=0;i<=6;i++){var p=VFormation.Offset(i,20);positions.Add(p.X+"/"+p.Y);}check(positions.Count==7,"all seven formation slots have distinct positions");
        var city=new CitySimulation(9){EnableCombinations=true};check(city.Upgrades.DropRate==1&&city.Upgrades.AircraftCount==1,"new city starts at one aircraft and one bomb per second");
        check(!city.BuyDropRateUpgrade()&&!city.BuyFormationUpgrade()&&!city.BuySpeedUpgrade()&&!city.BuyArmorUpgrade()&&!city.BuyHealthUpgrade(),"all new purchases reject an empty wallet");
        Fund(city);long before=city.Gold;check(city.BuyDropRateUpgrade()&&city.Gold==before-350&&city.Upgrades.DropRate==1.5,"drop-rate purchase spends exact gold and adds 0.5 per second");
        before=city.Gold;check(city.BuyFormationUpgrade()&&city.Gold==before-800&&city.Upgrades.AircraftCount==2,"formation purchase adds exactly one aircraft");
        Fund(city);before=city.Gold;check(city.BuySpeedUpgrade()&&city.Gold==before-450&&Math.Abs(city.Upgrades.SpeedMultiplier-1.12)<1e-8,"speed purchase spends exact gold and raises speed");
        before=city.Gold;check(city.BuyArmorUpgrade()&&city.Gold==before-500&&city.Upgrades.Armor==15,"armor purchase has independent cost and level");
        before=city.Gold;check(city.BuyHealthUpgrade()&&city.Gold==before-550&&city.Upgrades.AircraftHealth==125,"health purchase has independent cost and level");
        for(int i=0;i<400;i++){Fund(city);city.BuyDropRateUpgrade();city.BuyFormationUpgrade();city.BuySpeedUpgrade();city.BuyArmorUpgrade();city.BuyHealthUpgrade();}
        check(city.Upgrades.DropRateLevel==10&&city.Upgrades.DropRate==6&&city.Upgrades.FormationLevel==6&&city.Upgrades.AircraftCount==7,"drop rate and V formation stop at their explicit caps");
        check(city.Upgrades.SpeedLevel==10&&city.Upgrades.Armor==150&&city.Upgrades.AircraftHealth==350,"aircraft survivability upgrades reach their independent caps");
        before=city.Gold;check(!city.BuyDropRateUpgrade()&&!city.BuyFormationUpgrade()&&!city.BuySpeedUpgrade()&&!city.BuyArmorUpgrade()&&!city.BuyHealthUpgrade()&&city.Gold==before,"maximum purchases never spend more gold");
        var naked=new AircraftHealth(100,0);var armored=new AircraftHealth(100,100);naked.Hit(20);armored.Hit(20);check(naked.Current==80&&armored.Current==90,"armor reduces actual incoming missile damage");
        check(armored.Arrive()&&!armored.Arrive()&&armored.Hit(1000)==0,"arrival occurs once and escaped aircraft cannot be damaged");
        naked.Hit(1000);check(!naked.Alive&&naked.Current==0&&!naked.Arrive(),"shot-down aircraft have zero health and cannot award arrival credit");
        var charge=new SortieCharge(10);check(charge.Ready&&charge.Launch()&&!charge.Ready,"sortie starts ready and launching consumes a full use");
        charge.Arrive();check(charge.Charge==.5&&!charge.Ready&&charge.Remaining==5,"one arrival restores a fixed fifty percent");
        charge.Arrive();check(charge.Ready&&charge.Launch()&&charge.Charge==0,"two arrivals permit an immediate next sortie");
        charge.Tick(2);charge.Arrive();check(Math.Abs(charge.Charge-.7)<1e-9,"arrival adds fifty points rather than halving remaining time");
        var bank=new SortieCharge();bank.Launch();for(int i=0;i<4;i++)bank.Arrive();check(bank.Charge==2&&bank.Launch()&&bank.Launch()&&!bank.Launch(),"four arrivals bank two consecutive uses without wasting ready-state rewards");
        bank.Tick(100);bank.Tick(100);check(bank.Charge==1,"passive recharge fills one use without creating infinite idle stock");
        var low=new DefenseMissile(0,16,new AirVector(0,0,0),new AirVector(100,0,0));var high=new DefenseMissile(2,16,new AirVector(0,0,0),new AirVector(100,0,0));
        check(high.Damage>low.Damage&&high.Speed>low.Speed&&high.Radius>low.Radius,"building levels raise missile damage, speed and explosion radius");
        var oldDirection=low.Direction;low.Tick(.1,new AirVector(0,100,0),true);double turn=Math.Acos(Math.Max(-1,Math.Min(1,AirVector.Dot(oldDirection,low.Direction))));check(turn<=low.TurnRadians*.1+1e-8,"homing missiles cannot turn faster than the configured rate");
        check(Math.Abs((low.Position-low.Previous).Length-low.Speed*.1)<1e-8,"missiles travel at their level-defined speed");
        while(low.Age<1.3)low.Tick(.1,new AirVector(0,100,0),true);var locked=low.Goal;low.Tick(.1,new AirVector(999,999,999),true);check(low.Goal.X==locked.X&&low.Goal.Y==locked.Y&&low.Goal.Z==locked.Z,"missiles stop tracking new target positions after the limited tracking window");
        for(int i=0;i<100&&!high.Detonated;i++)high.Tick(.1,new AirVector(100,0,0),false);check(high.Detonated,"missiles terminate at their target or bounded lifetime");
        check(AirVector.SegmentDistance(new AirVector(-10,0,0),new AirVector(10,0,0),new AirVector(0,0,0))==0,"continuous collision distance catches movement through a target");
        var reset=new CitySimulation();check(reset.Upgrades.DropRateLevel==0&&reset.Upgrades.FormationLevel==0&&reset.Upgrades.SpeedLevel==0&&reset.Upgrades.ArmorLevel==0&&reset.Upgrades.HealthLevel==0,"new city resets the added progression paths");
        return passed;
    }
}
