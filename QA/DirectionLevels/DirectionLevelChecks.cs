using System;
using GrowingCity;
static class DirectionLevelChecks
{
    static void Fund(CitySimulation city){for(int x=100;x<108;x++)city.Build(new Cell(x,0),false,BuildingKind.Shop);city.Blast(103,0,12,0);}
    public static int Run()
    {
        int passed=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name);};
        DirectionalRoute.AcrossRectangle(new RoutePoint(.45,.5),new RoutePoint(.55,.5),-.25,-.25,1.25,1.25,out var a,out var b);
        check(Math.Abs(a.X+.25)<1e-8&&Math.Abs(b.X-1.25)<1e-8,"short direction drags extend beyond both screen edges");
        DirectionalRoute.AcrossRectangle(new RoutePoint(.1,.5),new RoutePoint(.9,.5),-.25,-.25,1.25,1.25,out var longA,out var longB);
        check(Math.Abs(a.X-longA.X)<1e-8&&Math.Abs(b.X-longB.X)<1e-8,"flight endpoints depend on direction rather than drag length");
        DirectionalRoute.AcrossRectangle(new RoutePoint(.55,.5),new RoutePoint(.45,.5),-.25,-.25,1.25,1.25,out var reverseA,out var reverseB);
        check(Math.Abs(reverseA.X-b.X)<1e-8&&Math.Abs(reverseB.X-a.X)<1e-8,"reverse drags reverse offscreen entry and exit");
        bool outside=true;for(int i=0;i<360;i++){double angle=i*Math.PI/180;var start=new RoutePoint(.5,.5);var end=new RoutePoint(.5+Math.Cos(angle)*.05,.5+Math.Sin(angle)*.05);DirectionalRoute.AcrossRectangle(start,end,-.25,-.25,1.25,1.25,out var first,out var last);outside&=(first.X<0||first.X>1||first.Y<0||first.Y>1)&&(last.X<0||last.X>1||last.Y<0||last.Y>1);}
        check(outside,"all 360 direction samples enter and exit outside the unit viewport");
        bool rejected=false;try{DirectionalRoute.AcrossRectangle(new RoutePoint(0,0),new RoutePoint(0,0),0,0,1,1,out _,out _);}catch(ArgumentException){rejected=true;}check(rejected,"zero direction is rejected");
        var full=new AirstrikePlan(0,0,1600,0,16,false,0,true);check(full.Length==1600&&full.BombCount==24&&full.ReleaseAt(0)>0&&full.ReleaseAt(full.BombCount-1)<full.Duration,"across-view corridors keep full length while bounding drops and entry-exit time");
        var city=new CitySimulation(41){EnableCombinations=true};check(city.Upgrades.AircraftCost==600,"bomber price matches scene");
        check(!city.BuyAircraftUpgrade()&&city.Gold==0,"unaffordable bomber cannot mutate gold");Fund(city);long wallet=city.Gold,score=city.Score;
        check(city.BuyAircraftUpgrade()&&city.Gold==wallet-600&&city.Score==score,"bomber purchase exact gold only");
        double snapshot=city.Upgrades.DamageMultiplier;check(snapshot==1.2,"bomber damage increases");for(int i=0;i<180;i++){Fund(city);city.BuyAircraftUpgrade();}
        check(city.Upgrades.AircraftLevel==10&&city.Upgrades.DamageMultiplier==3,"bomber damage cap preserved");wallet=city.Gold;check(!city.BuyAircraftUpgrade()&&city.Gold==wallet,"capped bomber cannot spend gold");
        check(snapshot==1.2,"captured damage unchanged");var target=city.Build(new Cell(0,0),false,BuildingKind.HouseA);check(target.GoldValue==100&&target.Health==100,"manual growth value bonus removed");
        var strike=city.DamageBlast(0,0,.1,201,60*city.Upgrades.DamageMultiplier);check(strike.Destroyed.Count==1&&strike.Points==100,"bomber damage and base gold work together");
        check(new CitySimulation().Upgrades.AircraftLevel==0,"new city resets bomber level");
        return passed;
    }
}
