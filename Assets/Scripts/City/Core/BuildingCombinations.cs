namespace GrowingCity
{
    // The first four kinds are the repeating basic growth catalogue.
    public enum BuildingKind { HouseA, HouseB, Shop, Office, Apartment, Tower, FuelDepot, AmmunitionDepot, PowerStation }
    public sealed class EvolutionRule
    {
        public readonly BuildingKind First,Second,Result;
        public readonly int Tier,DevelopmentValue,MaximumArea;
        public EvolutionRule(BuildingKind first,BuildingKind second,BuildingKind result,int tier,int developmentValue,int maximumArea){First=first;Second=second;Result=result;Tier=tier;DevelopmentValue=developmentValue;MaximumArea=maximumArea;}
        public bool Matches(BuildingKind a,BuildingKind b)=>(a==First&&b==Second)||(a==Second&&b==First);
    }
    public readonly struct LotConnection
    {
        public readonly Cell A,B;
        public readonly bool District;
        public LotConnection(Cell a,Cell b,bool district=false){A=a;B=b;District=district;}
    }
}
