using System;
using GrowingCity;
static class AirstrikeChecks
{
    public static int Run()
    {
        int passed=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name);};
        Action<Action,string> rejected=(action,name)=>{bool failed=false;try{action();}catch(ArgumentException){failed=true;}check(failed,name);};
        rejected(()=>new AirstrikePlan(0,0,0,0,16,false),"single clicks have no bombing plan");
        rejected(()=>new AirstrikePlan(0,0,3,0,16,false),"tiny ground drags are rejected");
        rejected(()=>new AirstrikePlan(0,0,20,0,0,false),"invalid cell size is rejected");
        rejected(()=>new AirstrikePlan(0,0,double.NaN,0,16,false),"nonfinite path coordinates are rejected");
        var run=new AirstrikePlan(-32,16,128,16,16,false);
        check(run.TargetX(0)==-32&&run.TargetX(run.BombCount-1)==128&&run.TargetZ(0)==16,"bombs cover the selected line endpoints");
        bool chronological=true,onLine=true;for(int i=0;i<run.BombCount;i++){
            chronological&=run.ReleaseAt(i)>0&&run.ImpactAt(i)>run.ReleaseAt(i)&&run.ReleaseAt(i)<run.Duration;
            if(i>0)chronological&=run.ReleaseAt(i)>run.ReleaseAt(i-1);
            onLine&=run.TargetZ(i)==16&&run.TargetX(i)>=-32&&run.TargetX(i)<=128;
        }
        check(chronological,"all releases and impacts are chronological and delayed after entry");
        check(onLine,"every target remains on the drag segment");
        var reverse=new AirstrikePlan(128,16,-32,16,16,false);
        check(reverse.TargetX(0)==128&&reverse.TargetX(reverse.BombCount-1)==-32,"reverse drags reverse the flight and bomb order");
        var diagonal=new AirstrikePlan(-16,-16,144,144,16,false);bool aligned=true;for(int i=0;i<diagonal.BombCount;i++)aligned&=diagonal.TargetX(i)==diagonal.TargetZ(i);
        check(aligned,"diagonal drags preserve diagonal target alignment");
        var longRun=new AirstrikePlan(0,0,16000,0,16,false);check(longRun.Length==512&&longRun.BombCount<=24,"extreme drags cap path length and normal bomb count");
        var nuke=new AirstrikePlan(0,0,512,0,16,true);check(nuke.BombCount<=8&&nuke.BombCount<longRun.BombCount,"nuclear runs use fewer spaced drops");
        check(!run.Complete(run.ImpactAt(run.BombCount-1))&&run.Complete(run.Duration+run.FallTime+1),"run cleanup waits for the last bomb impact");
        var city=new CitySimulation(2);city.Start();for(int x=1;x<=10;x++)city.Build(new Cell(x,0),false);city.Build(new Cell(5,5),false);
        var corridor=new AirstrikePlan(0,0,160,0,16,false);for(int i=0;i<corridor.BombCount;i++)city.Blast(corridor.TargetX(i)/16,corridor.TargetZ(i)/16,1.05,100+i);
        bool cleared=true;for(int x=0;x<=10;x++)cleared&=!city.Living.ContainsKey(new Cell(x,0));
        check(cleared&&city.Living.ContainsKey(new Cell(5,5))&&city.Score>0,"bombing clears the selected corridor, preserves distant lots and awards destruction score");
        return passed;
    }
}
