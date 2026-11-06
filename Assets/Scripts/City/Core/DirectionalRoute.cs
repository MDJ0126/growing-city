using System;
namespace GrowingCity
{
    public readonly struct RoutePoint
    {
        public readonly double X,Y;
        public RoutePoint(double x,double y){X=x;Y=y;}
    }
    public static class DirectionalRoute
    {
        public static void AcrossRectangle(RoutePoint a,RoutePoint b,double minX,double minY,double maxX,double maxY,out RoutePoint entry,out RoutePoint exit)
        {
            double dx=b.X-a.X,dy=b.Y-a.Y,length=Math.Sqrt(dx*dx+dy*dy);
            if(length<1e-8||double.IsInfinity(length)||double.IsNaN(length)||minX>=maxX||minY>=maxY)throw new ArgumentException("Invalid direction or bounds");
            dx/=length;dy/=length;double cx=(a.X+b.X)*.5,cy=(a.Y+b.Y)*.5,lo=double.NegativeInfinity,hi=double.PositiveInfinity;
            bool valid=Clip(cx,dx,minX,maxX,ref lo,ref hi)&&Clip(cy,dy,minY,maxY,ref lo,ref hi);
            if(!valid){double span=Math.Sqrt((maxX-minX)*(maxX-minX)+(maxY-minY)*(maxY-minY))*.5;lo=-span;hi=span;}
            entry=new RoutePoint(cx+dx*lo,cy+dy*lo);exit=new RoutePoint(cx+dx*hi,cy+dy*hi);
        }
        static bool Clip(double origin,double direction,double min,double max,ref double lo,ref double hi)
        {
            if(Math.Abs(direction)<1e-10)return origin>=min&&origin<=max;
            double a=(min-origin)/direction,b=(max-origin)/direction;if(a>b){double swap=a;a=b;b=swap;}lo=Math.Max(lo,a);hi=Math.Min(hi,b);return lo<=hi;
        }
    }
}
