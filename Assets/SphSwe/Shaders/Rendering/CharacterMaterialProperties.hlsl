#ifndef SPH_SWE_CHARACTER_MATERIAL_PROPERTIES_INCLUDED
#define SPH_SWE_CHARACTER_MATERIAL_PROPERTIES_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _StunColor;
    half _Metallic;
    half _Smoothness;
    float _StunEffectStrength;
    float _StunPulseSpeed;
CBUFFER_END

#endif