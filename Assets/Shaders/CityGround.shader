Shader "GrowingCity/Ground Field"
{
    Properties { _SoilColor("Bare soil",Color)=(0.38,0.30,0.19,1) _GrassColor("Dry grass",Color)=(0.36,0.40,0.23,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Ground" Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _SoilColor;
                half4 _GrassColor;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float4 shadowCoord:TEXCOORD2;half fog:TEXCOORD3;};
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);}
            Varyings Vert(Attributes input){Varyings o;VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.shadowCoord=GetShadowCoord(p);o.fog=ComputeFogFactor(p.positionCS.z);return o;}
            half4 Frag(Varyings input):SV_Target
            {
                float2 p=input.positionWS.xz;float patches=smoothstep(.35,.72,Noise(p*.035));
                half3 soil=lerp(_SoilColor.rgb,_GrassColor.rgb,patches);
                soil*=.86+Noise(p*.8)*.2+Noise(p*5)*.07;
                Light light=GetMainLight(input.shadowCoord);
                half illumination=.45+.55*saturate(dot(normalize(input.normalWS),light.direction))*light.shadowAttenuation;
                return half4(MixFog(soil*illumination*light.color,input.fog),1);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
