using System;
using GrowingCity;
static class DragGuideChecks
{
    public static int Run()
    {
        int count=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL: "+name);count++;Console.WriteLine("PASS: "+name);};
        var arrow=DragGuideGeometry.Arrow(new RoutePoint(10,20),new RoutePoint(110,20),18);
        check(arrow[0].X==10&&arrow[0].Y==20&&arrow[1].X==110&&arrow[1].Y==20,"2D guide uses exact screen-space pointer endpoints");
        check(arrow[2].X<110&&arrow[3].X<110&&arrow[2].Y>20&&arrow[3].Y<20,"arrow head faces the cursor and has symmetric wings");
        bool valid=true;for(int i=0;i<360;i++){double angle=i*Math.PI/180,dx=Math.Cos(angle),dy=Math.Sin(angle);var a=new RoutePoint(70,90);var b=new RoutePoint(70+dx*100,90+dy*100);var points=DragGuideGeometry.Arrow(a,b,18);for(int j=2;j<4;j++){double along=(points[j].X-b.X)*dx+(points[j].Y-b.Y)*dy;valid&=along<0&&!double.IsNaN(points[j].X);}valid&=points[0].X==a.X&&points[1].X==b.X;}
        check(valid,"360 screen directions preserve cursor endpoints and backward arrow wings");
        arrow=DragGuideGeometry.Arrow(new RoutePoint(0,0),new RoutePoint(1,0),18);check(arrow[2].X>0&&arrow[2].X<1,"short drags clamp the arrowhead inside the drag length");
        bool rejected=false;try{DragGuideGeometry.Arrow(new RoutePoint(0,0),new RoutePoint(0,0),18);}catch(ArgumentException){rejected=true;}check(rejected,"zero-length guides avoid invalid rotation and geometry");
        return count;
    }
}
