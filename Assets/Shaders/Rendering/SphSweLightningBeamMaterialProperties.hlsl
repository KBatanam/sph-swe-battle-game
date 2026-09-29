#ifndef SPH_SWE_LIGHTNING_BEAM_MATERIAL_PROPERTIES_INCLUDED
#define SPH_SWE_LIGHTNING_BEAM_MATERIAL_PROPERTIES_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BeamColor;
    float _Intensity;
    float _ScrollSpeed;
CBUFFER_END

#endif