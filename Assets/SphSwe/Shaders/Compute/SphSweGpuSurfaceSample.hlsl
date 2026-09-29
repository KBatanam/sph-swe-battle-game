#ifndef SPH_SWE_GPU_SURFACE_SAMPLE_INCLUDED
#define SPH_SWE_GPU_SURFACE_SAMPLE_INCLUDED

struct SphSweGpuSurfaceSample
{
    float2 FluidVelocity;
    float FluidDepth;
    uint IsValid;
};

#endif