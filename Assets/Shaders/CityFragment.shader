Shader "GrowingCity/Building Fragment"
{
    Properties { _BaseMap("Surface",2D)="white"{} _BaseColor("Color",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor; float4 _BaseMap_ST; float4 _ChunkMin; float4 _ChunkMax; float4x4 _ChunkToFrame;
            CBUFFER_END
            struct Input { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Output { float4 positionCS:SV_POSITION; float3 frame:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; };
            Output Vert(Input v){Output o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.frame=mul(_ChunkToFrame,v.positionOS).xyz;o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);return o;}
            half4 Frag(Output i):SV_Target
            {
                clip(i.frame-_ChunkMin.xyz);clip(_ChunkMax.xyz-i.frame);
                Light light=GetMainLight();half diffuse=.32h+.68h*saturate(dot(normalize(i.normalWS),light.direction));
                half3 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                return half4(color*(.3h+light.color*diffuse),1);
            }
            ENDHLSL
        }
    }
}
