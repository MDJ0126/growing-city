using System;
namespace GrowingCity
{
    public static class VFormation
    {
        // X is lateral offset, Y is distance behind the leader.
        public static RoutePoint Offset(int index,double spacing)
        {
            if(index<0||index>BombardmentUpgrades.MaximumFormationLevel||spacing<=0||double.IsNaN(spacing)||double.IsInfinity(spacing))throw new ArgumentOutOfRangeException();
            if(index==0)return new RoutePoint(0,0);
            int row=(index+1)/2;return new RoutePoint((index%2==1?-1:1)*row*spacing,row*spacing*1.25);
        }
    }
}
