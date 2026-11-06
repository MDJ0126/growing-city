using System;
namespace GrowingCity
{
    public static class CityProgression
    {
        public static double GrowthRate(double elapsed)=>Math.Min(8,1+Math.Max(0,elapsed)/60);
        public static int BirthTier(double elapsed,double roll){double progress=Math.Min(1,Math.Max(0,(elapsed-90)/210));return roll<progress*.75?2:roll<progress*.95?1:0;}
        public static bool CanLaunchMissiles(Lot lot)=>lot!=null&&lot.Level>=3;
        public static bool IsSpecial(BuildingKind kind)=>kind==BuildingKind.FuelDepot||kind==BuildingKind.AmmunitionDepot||kind==BuildingKind.PowerStation;
        public static double ExplosionRadius(BuildingKind kind)=>kind==BuildingKind.FuelDepot?2.1:kind==BuildingKind.AmmunitionDepot?2.7:kind==BuildingKind.PowerStation?3.2:0;
        public static double ExplosionDamage(BuildingKind kind)=>kind==BuildingKind.FuelDepot?90:kind==BuildingKind.AmmunitionDepot?140:kind==BuildingKind.PowerStation?200:0;
    }
    public sealed class ChainExplosion
    {
        public readonly Cell Cell;public readonly BuildingKind Kind;public readonly int Depth;
        public double Radius=>CityProgression.ExplosionRadius(Kind);
        public double Damage=>CityProgression.ExplosionDamage(Kind);
        public ChainExplosion(Lot lot,int depth){Cell=lot.Cell;Kind=lot.Kind;Depth=depth;}
    }
}
