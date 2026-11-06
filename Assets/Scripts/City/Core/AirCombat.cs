using System;
namespace GrowingCity
{
    public readonly struct AirVector
    {
        public readonly double X,Y,Z;
        public AirVector(double x,double y,double z){X=x;Y=y;Z=z;}
        public double Length=>Math.Sqrt(X*X+Y*Y+Z*Z);
        public AirVector Unit=>Length<1e-9?new AirVector(0,0,1):this/Length;
        public static AirVector operator +(AirVector a,AirVector b)=>new AirVector(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static AirVector operator -(AirVector a,AirVector b)=>new AirVector(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static AirVector operator *(AirVector a,double n)=>new AirVector(a.X*n,a.Y*n,a.Z*n);
        public static AirVector operator /(AirVector a,double n)=>a*(1/n);
        public static double Dot(AirVector a,AirVector b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
        public static AirVector Cross(AirVector a,AirVector b)=>new AirVector(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        public static double SegmentDistance(AirVector a,AirVector b,AirVector point){var delta=b-a;double sq=Dot(delta,delta);double t=sq<1e-12?0:Math.Max(0,Math.Min(1,Dot(point-a,delta)/sq));return (point-(a+delta*t)).Length;}
    }
    public sealed class AircraftHealth
    {
        public readonly double Maximum,Armor;
        public double Current {get;private set;}
        public bool Escaped {get;private set;}
        public bool Alive=>Current>0;
        public AircraftHealth(double health,double armor){if(health<=0||armor<0||double.IsNaN(health)||double.IsInfinity(health)||double.IsNaN(armor)||double.IsInfinity(armor))throw new ArgumentOutOfRangeException();Maximum=Current=health;Armor=armor;}
        public double Hit(double damage){if(damage<=0||double.IsNaN(damage)||double.IsInfinity(damage))throw new ArgumentOutOfRangeException();if(!Alive||Escaped)return 0;double applied=Math.Min(Current,damage*100/(100+Armor));Current-=applied;return applied;}
        public bool Arrive(){if(!Alive||Escaped)return false;Escaped=true;return true;}
    }
    public sealed class SortieCharge
    {
        public readonly double RechargeSeconds;
        public double Charge {get;private set;}=1;
        public long Arrivals {get;private set;}
        public bool Ready=>Charge>=1-1e-9;
        public double Remaining=>Math.Max(0,1-Charge)*RechargeSeconds;
        public SortieCharge(double seconds=10){if(seconds<=0||double.IsNaN(seconds)||double.IsInfinity(seconds))throw new ArgumentOutOfRangeException();RechargeSeconds=seconds;}
        public void Tick(double dt){if(dt<0||double.IsNaN(dt)||double.IsInfinity(dt))throw new ArgumentOutOfRangeException();if(Charge<1)Charge=Math.Min(1,Charge+dt/RechargeSeconds);}
        public bool Launch(){if(!Ready)return false;Charge=Math.Max(0,Charge-1);return true;}
        public void Arrive(){Charge+=.5;Arrivals++;}
    }
    public sealed class DefenseMissile
    {
        public readonly int Level;
        public readonly double Damage,Speed,Radius,TurnRadians,TrackingSeconds=1.2,LifeSeconds=6;
        public AirVector Position {get;private set;}
        public AirVector Previous {get;private set;}
        public AirVector Direction {get;private set;}
        public AirVector Goal {get;private set;}
        public double Age {get;private set;}
        public bool Detonated {get;private set;}
        public DefenseMissile(int tier,double cellSize,AirVector start,AirVector goal){Level=Math.Max(1,Math.Min(3,tier+1));Damage=12*Math.Pow(1.5,Level-1);Speed=cellSize*(2+.5*Level);Radius=cellSize*(.25+.125*(Level-1));TurnRadians=35*Math.PI/180;Position=Previous=start;Goal=goal;Direction=(goal-start).Unit;}
        public bool Tick(double dt,AirVector target,bool track)
        {
            if(dt<0||double.IsNaN(dt)||double.IsInfinity(dt))throw new ArgumentOutOfRangeException();if(Detonated)return true;if(dt==0)return false;
            if(track&&Age<TrackingSeconds)Goal=target;Age+=dt;
            var desired=(Goal-Position).Unit;double angle=Math.Acos(Math.Max(-1,Math.Min(1,AirVector.Dot(Direction,desired)))),step=Math.Min(angle,TurnRadians*dt);
            if(angle<1e-6)Direction=desired;else if(Math.Abs(Math.Sin(angle))>.00001)Direction=(Direction*(Math.Sin(angle-step)/Math.Sin(angle))+desired*(Math.Sin(step)/Math.Sin(angle))).Unit;
            else {var axis=AirVector.Cross(Direction,new AirVector(0,1,0));if(axis.Length<1e-6)axis=AirVector.Cross(Direction,new AirVector(1,0,0));Direction=(Direction*Math.Cos(step)+AirVector.Cross(axis.Unit,Direction)*Math.Sin(step)).Unit;}
            Previous=Position;Position+=Direction*(Speed*dt);Detonated=AirVector.SegmentDistance(Previous,Position,Goal)<Radius*.3||Age>=LifeSeconds;return Detonated;
        }
        public void Detonate(){Detonated=true;}
    }
}
