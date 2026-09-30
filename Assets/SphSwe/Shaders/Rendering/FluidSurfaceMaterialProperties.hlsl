#ifndef SPH_SWE_FLUID_SURFACE_MATERIAL_PROPERTIES_INCLUDED
#define SPH_SWE_FLUID_SURFACE_MATERIAL_PROPERTIES_INCLUDED

CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    half _Metallic;
    half _Smoothness;
    float _GroundHeight;
    float _FluidDepthHeightScale;
CBUFFER_END

#endif