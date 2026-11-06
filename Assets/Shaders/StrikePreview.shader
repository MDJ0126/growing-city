Shader "GrowingCity/StrikePreview"
{
    Properties { _BaseColor("Path color", Color) = (1,0.78,0.18,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings Vert(Attributes input) { Varyings output; output.positionCS=TransformObjectToHClip(input.positionOS.xyz); return output; }
            half4 Frag(Varyings input) : SV_Target { return _BaseColor; }
            ENDHLSL
        }
    }
}
