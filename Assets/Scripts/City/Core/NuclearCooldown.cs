using System;
namespace GrowingCity
{
    // Independent of normal sortie credits and aircraft arrival rewards.
    public sealed class NuclearCooldown
    {
        public const double Duration=100;
        public double Remaining {get;private set;}
        public bool Ready=>Remaining<=0;
        public bool Launch(){if(!Ready)return false;Remaining=Duration;return true;}
        public void Tick(double seconds){if(seconds<0||double.IsNaN(seconds)||double.IsInfinity(seconds))throw new ArgumentOutOfRangeException(nameof(seconds));Remaining=Math.Max(0,Remaining-seconds);}
    }
}
