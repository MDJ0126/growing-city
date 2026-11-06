using System;
namespace GrowingCity
{
    public sealed class AirstrikePlan
    {
        public readonly double StartX,StartZ,EndX,EndZ,Length,Lead,Duration,FallTime;
        public readonly int BombCount;
        public readonly double BombsPerSecond;
        public readonly double AttackStart,AttackEnd;
        public AirstrikePlan(double x,double z,double endX,double endZ,double cellSize,bool nuclear,int bombLevel=0,bool acrossView=false,double bombsPerSecond=0,double speedMultiplier=1)
        {
            if(double.IsNaN(bombsPerSecond)||double.IsInfinity(bombsPerSecond)||bombsPerSecond<0||bombsPerSecond>20)throw new ArgumentOutOfRangeException(nameof(bombsPerSecond));
            if(double.IsNaN(speedMultiplier)||double.IsInfinity(speedMultiplier)||speedMultiplier<=0)throw new ArgumentOutOfRangeException(nameof(speedMultiplier));
            if(bombLevel<0||bombLevel>BombardmentUpgrades.MaximumLevel)throw new ArgumentOutOfRangeException(nameof(bombLevel));
            foreach(var v in new[]{x,z,endX,endZ,cellSize})if(double.IsNaN(v)||double.IsInfinity(v))throw new ArgumentOutOfRangeException();
            if(cellSize<=0)throw new ArgumentOutOfRangeException(nameof(cellSize));
            double dx=endX-x,dz=endZ-z,distance=Math.Sqrt(dx*dx+dz*dz);
            if(double.IsNaN(distance)||double.IsInfinity(distance))throw new ArgumentOutOfRangeException();
            if(distance<cellSize*.25)throw new ArgumentException("Drag a longer line.");
            Length=acrossView?distance:Math.Min(distance,cellSize*32);StartX=x;StartZ=z;EndX=x+dx/distance*Length;EndZ=z+dz/distance*Length;
            Lead=acrossView?Math.Max(cellSize*2,Length*.35):cellSize*2;Duration=Math.Max(acrossView?2.8:2.1,(Length+Lead*2)/(cellSize*12));FallTime=.42;
            int extra=bombLevel*(nuclear?1:2);
            double count=Math.Ceiling(Length/(cellSize*(nuclear?4.5:1.4)))+1;
            BombCount=(int)Math.Min((nuclear?8:24)+extra,Math.Max(2,count)+extra);
            BombsPerSecond=bombsPerSecond;
            if(BombsPerSecond>0){AttackStart=.7/speedMultiplier;AttackEnd=AttackStart+Length/(cellSize*6*speedMultiplier);BombCount=Math.Min(BombCount,Math.Max(1,(int)Math.Min(10000,Math.Floor((AttackEnd-AttackStart)*BombsPerSecond)+1)));Duration=AttackEnd+.7/speedMultiplier;}
            else {AttackStart=Duration*Lead/(Length+Lead*2);AttackEnd=Duration*(Lead+Length)/(Length+Lead*2);}
        }
        public double Fraction(int index){if(index<0||index>=BombCount)throw new ArgumentOutOfRangeException(nameof(index));if(BombsPerSecond>0)return (ReleaseAtUnchecked(index)-AttackStart)/(AttackEnd-AttackStart);return index/(double)(BombCount-1);}
        public double TargetX(int index)=>StartX+(EndX-StartX)*Fraction(index);
        public double TargetZ(int index)=>StartZ+(EndZ-StartZ)*Fraction(index);
        double ReleaseAtUnchecked(int index)=>AttackStart+((AttackEnd-AttackStart)-(BombCount-1)/BombsPerSecond)*.5+index/BombsPerSecond;
        public double ReleaseAt(int index){if(index<0||index>=BombCount)throw new ArgumentOutOfRangeException(nameof(index));return BombsPerSecond>0?ReleaseAtUnchecked(index):Duration*(Lead+Length*Fraction(index))/(Length+Lead*2);}
        public double ImpactAt(int index)=>ReleaseAt(index)+FallTime;
        public bool Complete(double age)=>age>=Math.Max(Duration,ImpactAt(BombCount-1))+.15;
    }
}
