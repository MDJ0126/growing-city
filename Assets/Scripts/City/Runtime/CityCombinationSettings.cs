using System;
using UnityEngine;

namespace GrowingCity
{
    [CreateAssetMenu(menuName="Growing City/Building Combination Rules")]
    public sealed class CityCombinationSettings : ScriptableObject
    {
        [Serializable] public sealed class Recipe
        {
            public BuildingKind first,second,result;
            [Min(1)]public int tier=1,developmentValue=40,maximumArea=2;
        }
        public Recipe[] recipes;
        [Min(0)]public int commercialLinkBonus=12;
        public BuildingKind districtFirst=BuildingKind.Shop,districtSecond=BuildingKind.Office;
        public void Apply(CitySimulation city)
        {
            city.EnableCombinations=true;city.DistrictLinkBonus=commercialLinkBonus;city.DistrictFirst=districtFirst;city.DistrictSecond=districtSecond;
            if(recipes==null||recipes.Length==0)return;
            city.EvolutionRules.Clear();foreach(var recipe in recipes)if(recipe!=null)city.EvolutionRules.Add(new EvolutionRule(recipe.first,recipe.second,recipe.result,recipe.tier,recipe.developmentValue,recipe.maximumArea));
        }
    }
}
