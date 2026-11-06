using System;
namespace GrowingCity
{
    public static class DragGuideGeometry
    {
        public static RoutePoint[] Arrow(RoutePoint start,RoutePoint end,double headLength)
        {
            double dx=end.X-start.X,dy=end.Y-start.Y,length=Math.Sqrt(dx*dx+dy*dy);
            if(length<1e-6||double.IsNaN(length)||double.IsInfinity(length)||headLength<=0)throw new ArgumentException();
            dx/=length;dy/=length;double head=Math.Min(headLength,length*.35),width=head*.55;
            return new[]{start,end,new RoutePoint(end.X-dx*head-dy*width,end.Y-dy*head+dx*width),new RoutePoint(end.X-dx*head+dy*width,end.Y-dy*head-dx*width)};
        }
    }
}
