#ifndef SPH_SWE_GPU_PARTICLE_INCLUDED
#define SPH_SWE_GPU_PARTICLE_INCLUDED

struct SphSweGpuParticle
{
    float2 Position;
    float2 Velocity;
    float2 Acceleration;
    
    float Density;
    float Mass;
    float InitialMass;
    float EffectiveRadius;
    float FluidDepth;
    
    int Type;
};

#endif