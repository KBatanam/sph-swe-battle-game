Shader "SPH-SWE/GPU Particle"
{
    Properties
    {
        _FluidParticleColor("Fluid Particle Color", Color) = (0.1, 0.5, 1.0, 1.0)
        _BoundaryParticleColor("Boundary Particle Color", Color) = (0.3, 0.3, 0.3, 1.0)
        _ParticleRadius("Particle Radius", Float) = 0.03
        _GroundHeight("Ground Height", Float) = 0.0
        _FluidDepthHeightScale("Fluid Depth Height Scale", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardUnlit"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_local _ SPH_SWE_PARTICLE_BUFFER_AVAILABLE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Compute/SphSweGpuParticle.hlsl"
            #include "SphSweGpuParticleMaterialProperties.hlsl"

            #if defined(SPH_SWE_PARTICLE_BUFFER_AVAILABLE)
                StructuredBuffer<SphSweGpuParticle> _Particles;
            #endif
            
            static const int BoundaryParticleType = 1;

            static const float2 QuadCoordinates[6] =
            {
                float2(-1.0f, -1.0f),
                float2(-1.0f,  1.0f),
                float2( 1.0f,  1.0f),
                float2(-1.0f, -1.0f),
                float2( 1.0f,  1.0f),
                float2( 1.0f, -1.0f)
            };

            struct VertexInput
            {
                uint vertexId : SV_VertexID;
                uint instanceId : SV_InstanceID;
            };

            struct FragmentInput
            {
                float4 positionCS : SV_POSITION;
                float2 particleCoordinate : TEXCOORD0;
                float4 color : COLOR;
            };
            
            FragmentInput Vertex(VertexInput input)
            {
                FragmentInput output;

            #if !defined(SPH_SWE_PARTICLE_BUFFER_AVAILABLE)
                output.positionCS = float4(2.0f, 2.0f, 2.0f, 1.0f);
                output.particleCoordinate = 0.0f;
                output.color = 0.0f;
                return output;
            #else
                SphSweGpuParticle particle = _Particles[input.instanceId];

                float displayedFluidDepth = particle.Type == BoundaryParticleType
                    ? 1.0f
                    : particle.FluidDepth;

                float particleHeight =
                    _GroundHeight + displayedFluidDepth * _FluidDepthHeightScale;

                float3 localParticlePosition = float3(
                    particle.Position.x,
                    particleHeight,
                    particle.Position.y
                );

                float3 worldParticlePosition =
                    mul(_SimulationLocalToWorld, float4(localParticlePosition, 1.0f)).xyz;

                float2 quadCoordinate = QuadCoordinates[input.vertexId];
                float3 worldBillboardOffset =
                    TransformViewToWorldDir(float3(quadCoordinate * _ParticleRadius, 0.0f));
                float3 worldVertexPosition =
                    worldParticlePosition + worldBillboardOffset;

                output.positionCS = TransformWorldToHClip(worldVertexPosition);
                output.particleCoordinate = quadCoordinate;
                output.color = particle.Type == BoundaryParticleType
                    ? _BoundaryParticleColor
                    : _FluidParticleColor;

                return output;
            #endif
            }

            half4 Fragment(FragmentInput input) : SV_Target
            {
                float squaredDistanceFromCenter = dot(input.particleCoordinate, input.particleCoordinate);
                clip(1.0f - squaredDistanceFromCenter);

                return input.color;
            }

            ENDHLSL
        }
    }
}