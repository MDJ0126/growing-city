using System;
namespace GrowingCity
{
    public readonly struct CollapsePose
    {
        public readonly double Angle,Yaw,Travel,Lift,Drop;
        public CollapsePose(double angle,double yaw,double travel,double lift,double drop){Angle=angle;Yaw=yaw;Travel=travel;Lift=lift;Drop=drop;}
    }
    public static class DestructionMotion
    {
        public static int Mode(int seed)=>Math.Abs(seed%3);
        public static double Duration(int mode)=>mode==0?1.25:mode==1?1.05:.85;
        public static double BreakFraction(int mode)=>mode==0?.72:mode==1?.32:.14;
        public static CollapsePose Pose(int mode,double age,double height,double width)
        {
            double t=Math.Max(0,Math.Min(1,age/Duration(mode))),ease=t*t*(3-2*t);
            return new CollapsePose((mode==0?88:mode==1?42:22)*ease,mode==1?ease*38:0,width*t*t*.45,mode==2?height*.22*Math.Sin(t*Math.PI):0,height*t*t*.08);
        }
        public static double HorizontalSpeed(int index,int seed)=>1.25+Math.Abs((seed%11+index*7)%12)*.19;
        public static double UpSpeed(int index,int seed)=>1.4+Math.Abs((seed%7+index*5)%9)*.19;
    }
}
