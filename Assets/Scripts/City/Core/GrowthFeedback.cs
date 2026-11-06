using System;
namespace GrowingCity
{
    public static class GrowthFeedback
    {
        public static float EvolutionLift(float t) => t<=0 || t>=.7f ? 0 : (float)Math.Sin(t/.7f*Math.PI);
        public static float EvolutionScale(float t)
        {
            if(t<=0)return .001f;if(t>=1)return 1;
            if(t<.35f){float p=t/.35f;return .001f+1.159f*(1-(1-p)*(1-p)*(1-p));}
            if(t<.7f)return 1.16f-(t-.35f)/.35f*.16f;
            return 1-(float)Math.Sin((t-.7f)/.3f*Math.PI)*.09f;
        }
    }
}
