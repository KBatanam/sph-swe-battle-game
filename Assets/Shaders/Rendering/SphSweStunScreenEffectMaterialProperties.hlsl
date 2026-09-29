#ifndef SPH_SWE_STUN_SCREEN_EFFECT_MATERIAL_PROPERTIES_INCLUDED
#define SPH_SWE_STUN_SCREEN_EFFECT_MATERIAL_PROPERTIES_INCLUDED

CBUFFER_START(UnityPerMaterial)
    half4 _LightningColor;
    float _EffectStrength;
    float _BorderWidth;
    float _TextureTiling;
    float _ScrollSpeed;
CBUFFER_END

#endif