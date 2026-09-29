#ifndef SPH_SWE_GPU_PARTICLE_MATERIAL_PROPERTIES_INCLUDED
#define SPH_SWE_GPU_PARTICLE_MATERIAL_PROPERTIES_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float4 _FluidParticleColor;
    float4 _BoundaryParticleColor;
    float4x4 _SimulationLocalToWorld;
    float _ParticleRadius;
    float _GroundHeight;
    float _FluidDepthHeightScale;
CBUFFER_END

#endif