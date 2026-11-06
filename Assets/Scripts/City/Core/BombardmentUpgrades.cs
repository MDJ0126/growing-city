using System;
namespace GrowingCity
{
    public sealed class BombardmentUpgrades
    {
        public const int MaximumLevel=10;
        public const int MaximumFormationLevel=6;
        public int RadiusLevel {get;private set;}
        public int BombLevel {get;private set;}
        public int AircraftLevel {get;private set;}
        public int DropRateLevel {get;private set;}
        public int FormationLevel {get;private set;}
        public int SpeedLevel {get;private set;}
        public int ArmorLevel {get;private set;}
        public int HealthLevel {get;private set;}
        public double SpeedMultiplier => 1+SpeedLevel*.12;
        public double Armor => ArmorLevel*15;
        public double AircraftHealth => 100+HealthLevel*25;
        public long SpeedCost => Cost(450,SpeedLevel);
        public long ArmorCost => Cost(500,ArmorLevel);
        public long HealthCost => Cost(550,HealthLevel);
        public double RadiusMultiplier => 1+RadiusLevel*.15;
        public long RadiusCost => Cost(300,RadiusLevel);
        public long BombCost => Cost(450,BombLevel);
        public long AircraftCost => Cost(600,AircraftLevel);
        public double DamageMultiplier => 1+AircraftLevel*.2;
        public double DropRate => 1+DropRateLevel*.5;
        public int AircraftCount => 1+FormationLevel;
        public long DropRateCost => Cost(350,DropRateLevel);
        public long FormationCost => FormationLevel>=MaximumFormationLevel?0:Cost(800,FormationLevel);
        static long Cost(int first,int level)=>level>=MaximumLevel?0:(long)(Math.Ceiling(first*Math.Pow(1.7,level)/50)*50);
        internal void RaiseRadius(){if(RadiusLevel>=MaximumLevel)throw new InvalidOperationException();RadiusLevel++;}
        internal void RaiseBombs(){if(BombLevel>=MaximumLevel)throw new InvalidOperationException();BombLevel++;}
        internal void RaiseAircraft(){if(AircraftLevel>=MaximumLevel)throw new InvalidOperationException();AircraftLevel++;}
        internal void RaiseDropRate(){if(DropRateLevel>=MaximumLevel)throw new InvalidOperationException();DropRateLevel++;}
        internal void RaiseFormation(){if(FormationLevel>=MaximumFormationLevel)throw new InvalidOperationException();FormationLevel++;}
        internal void RaiseSpeed(){if(SpeedLevel>=MaximumLevel)throw new InvalidOperationException();SpeedLevel++;}
        internal void RaiseArmor(){if(ArmorLevel>=MaximumLevel)throw new InvalidOperationException();ArmorLevel++;}
        internal void RaiseHealth(){if(HealthLevel>=MaximumLevel)throw new InvalidOperationException();HealthLevel++;}
    }
}
