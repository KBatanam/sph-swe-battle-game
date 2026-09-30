Shader "SPH-SWE/GPU Fluid Surface"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color",Color) = (0.05, 0.4, 0.8, 1.0)
        _Metallic("Metallic",Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness",Range(0.0, 1.0)) = 0.8
        _GroundHeight("Ground Height",Float) = 0.0
        _FluidDepthHeightScale("Fluid Depth Height Scale",Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
        }

        Pass
        {
            Name "ForwardLit"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile_local _ SPH_SWE_SURFACE_VERTEX_BUFFER_AVAILABLE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "../Compute/SphSweGpuSurfaceVertex.hlsl"
            #include "FluidSurfaceMaterialProperties.hlsl"

            #if defined(SPH_SWE_SURFACE_VERTEX_BUFFER_AVAILABLE)
                StructuredBuffer<SphSweGpuSurfaceVertex> _SurfaceVertices;
            #endif

            struct VertexInput
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint vertexId : SV_VertexID;
            };

            struct FragmentInput
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            FragmentInput Vertex(VertexInput input)
            {
                FragmentInput output = (FragmentInput)0;

            #if !defined(SPH_SWE_SURFACE_VERTEX_BUFFER_AVAILABLE)
                output.positionCS = float4(2.0f, 2.0f, 2.0f, 1.0f);
                return output;
            #else
                SphSweGpuSurfaceVertex surfaceVertex = _SurfaceVertices[input.vertexId];
                float3 surfacePositionOS = input.positionOS;
                surfacePositionOS.y = _GroundHeight + surfaceVertex.FluidDepth * _FluidDepthHeightScale;

                // 水深をY方向へ拡大して描画する場合、
                // 法線のX、Z成分にも同じ倍率を反映する。
                float3 surfaceNormalOS = normalize(
                    float3(
                        surfaceVertex.Normal.x * _FluidDepthHeightScale,
                        surfaceVertex.Normal.y,
                        surfaceVertex.Normal.z * _FluidDepthHeightScale
                    )
                );

                VertexPositionInputs positionInputs =GetVertexPositionInputs(surfacePositionOS);
                VertexNormalInputs normalInputs =GetVertexNormalInputs(surfaceNormalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                return output;
            #endif
            }

            half4 Fragment(FragmentInput input) : SV_Target
            {
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);

                SurfaceData surfaceData = (SurfaceData)0;

                surfaceData.albedo = _BaseColor.rgb;
                surfaceData.alpha = _BaseColor.a;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = 0.0h;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
                surfaceData.occlusion = 1.0h;
                surfaceData.emission = 0.0h;
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 0.0h;

                InputData inputData = (InputData)0;

                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = VertexLighting(input.positionWS,normalWS);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);

                half4 color = UniversalFragmentPBR(inputData,surfaceData);
                color.rgb = MixFog(color.rgb,inputData.fogCoord);

                return color;
            }

            ENDHLSL
        }
    }
}