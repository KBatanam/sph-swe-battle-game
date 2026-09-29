Shader "SPH-SWE/Lightning Beam"
{
    Properties
    {
        [MainTexture] _BaseMap("Lightning Texture", 2D) = "white" {}
        [MainColor] [HDR] _BeamColor("Beam Color", Color) = (1.0, 0.8, 0.1, 1.0)
        _Intensity("Intensity", Float) = 2.0
        _ScrollSpeed("Scroll Speed", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "LightningBeam"

            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            ColorMask RGB

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "SphSweLightningBeamMaterialProperties.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            struct VertexInput
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct FragmentInput
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            FragmentInput Vertex(VertexInput input)
            {
                FragmentInput output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.uv.x += _Time.y * _ScrollSpeed;
                return output;
            }

            half4 Fragment(FragmentInput input) : SV_Target
            {
                half4 lightningTexture = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                half lightningMask = lightningTexture.a;
                half3 beamColor = _BeamColor.rgb * _Intensity;

                return half4(beamColor, lightningMask * _BeamColor.a);
            }

            ENDHLSL
        }
    }
}