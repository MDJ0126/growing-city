using System;
using GrowingCity;
static class UpgradeChecks
{
    static void Fund(CitySimulation city){for(int x=0;x<8;x++)city.Build(new Cell(x,0),false);city.Blast(3,0,12,0);}
    public static int Run()
    {
        int passed=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name);};
        var city=new CitySimulation(17);
        check(city.Gold==0&&city.Upgrades.RadiusLevel==0&&city.Upgrades.BombLevel==0,"new city starts with no gold and unpurchased upgrades");
        check(!city.BuyRadiusUpgrade()&&!city.BuyBombUpgrade()&&city.Gold==0,"unaffordable purchases do not alter gold or levels");
        check(city.Upgrades.RadiusCost==300&&city.Upgrades.BombCost==450,"first upgrade prices match native shop labels");
        var budget=new CitySimulation(8);Fund(budget);long initial=budget.Gold,spent=0;while(budget.Upgrades.RadiusCost<=budget.Gold&&budget.Upgrades.RadiusCost>0){long cost=budget.Upgrades.RadiusCost;if(!budget.BuyRadiusUpgrade())throw new Exception("Affordable purchase rejected");spent+=cost;}
        check(!budget.BuyRadiusUpgrade()&&budget.Gold==initial-spent&&budget.Gold>=0&&budget.Score==initial,"repeated purchases stop at the budget without negative gold or lifetime score loss");
        Fund(city);long earned=city.Score;
        check(city.Gold==earned&&earned>750,"destruction awards both wallet gold and lifetime score");
        long price=city.Upgrades.RadiusCost;check(city.BuyRadiusUpgrade()&&city.Gold==earned-price&&city.Score==earned,"range purchase charges exact gold without reducing lifetime score");
        check(city.Upgrades.RadiusLevel==1&&Math.Abs(city.Upgrades.RadiusMultiplier-1.15)<1e-8&&city.Upgrades.RadiusCost>price,"range level raises radius and next price");
        long before=city.Gold;price=city.Upgrades.BombCost;
        check(city.BuyBombUpgrade()&&city.Gold==before-price&&city.Upgrades.BombLevel==1&&city.Upgrades.BombCost>price,"bomb purchase charges only its own upgrade and raises next price");
        var basic=new AirstrikePlan(0,0,160,0,16,false);var upgraded=new AirstrikePlan(0,0,160,0,16,false,1);
        var nuke=new AirstrikePlan(0,0,160,0,16,true);var nukeUpgraded=new AirstrikePlan(0,0,160,0,16,true,1);
        check(upgraded.BombCount==basic.BombCount+2&&nukeUpgraded.BombCount==nuke.BombCount+1,"one count level adds two normal bombs and one nuclear bomb");
        check(upgraded.TargetX(0)==0&&upgraded.TargetX(upgraded.BombCount-1)==160,"extra bombs preserve the drag endpoints");
        var maxNormal=new AirstrikePlan(0,0,512,0,16,false,10);var maxNuke=new AirstrikePlan(0,0,512,0,16,true,10);
        check(maxNormal.BombCount==44&&maxNuke.BombCount==18,"maximum count level respects 44 normal and 18 nuclear drop caps");
        bool timed=true;for(int i=1;i<maxNormal.BombCount;i++)timed &= maxNormal.ReleaseAt(i)>maxNormal.ReleaseAt(i-1);
        check(timed&&maxNormal.Complete(maxNormal.Duration+2),"maximum upgraded runs keep ordered releases and finish after impacts");
        var snapshot=new AirstrikePlan(0,0,32,0,16,false,city.Upgrades.BombLevel);int oldCount=snapshot.BombCount;double radiusSnapshot=city.Upgrades.RadiusMultiplier;
        for(int i=0;i<90;i++){Fund(city);if(city.Upgrades.RadiusLevel<10)city.BuyRadiusUpgrade();if(city.Upgrades.BombLevel<10)city.BuyBombUpgrade();}
        check(snapshot.BombCount==oldCount&&radiusSnapshot==1.15,"existing flight plan and captured radius stay unchanged after further upgrades");
        check(city.Upgrades.RadiusLevel==10&&city.Upgrades.BombLevel==10&&city.Upgrades.RadiusMultiplier==2.5,"both upgrade paths reach their independent level caps");
        before=city.Gold;check(!city.BuyRadiusUpgrade()&&!city.BuyBombUpgrade()&&city.Gold==before&&city.Upgrades.RadiusCost==0&&city.Upgrades.BombCost==0,"max-level purchases cannot charge gold or increase levels");
        var restarted=new CitySimulation(17);check(restarted.Gold==0&&restarted.Score==0&&restarted.Upgrades.RadiusLevel==0&&restarted.Upgrades.BombLevel==0,"new city resets wallet and both upgrade levels");
        var radiusCity=new CitySimulation();radiusCity.Build(new Cell(0,0),false);radiusCity.Build(new Cell(2,0),false);var wider=radiusCity.DamageBlast(0,0,1.05*2.5,0,100);
        check(wider.Destroyed.Count==2,"upgraded radius can damage a building outside the original radius");
        bool rejected=false;try{new AirstrikePlan(0,0,32,0,16,false,11);}catch(ArgumentOutOfRangeException){rejected=true;}check(rejected,"invalid count levels are rejected");
        return passed;
    }
}
