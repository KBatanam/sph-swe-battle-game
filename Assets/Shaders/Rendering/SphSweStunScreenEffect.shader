Shader "SPH-SWE/Stun Screen Effect"
{
    Properties
    {
        _LightningTexture("Lightning Texture", 2D) = "black" {}
        [HDR] _LightningColor("Lightning Color", Color) = (1.0, 0.8, 0.1, 1.0)
        _EffectStrength("Effect Strength", Range(0.0, 1.0)) = 0.0
        _BorderWidth("Border Width", Range(0.01, 0.5)) = 0.15
        _TextureTiling("Texture Tiling", Float) = 2.0
        _ScrollSpeed("Scroll Speed", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "StunScreenEffect"

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "SphSweStunScreenEffectMaterialProperties.hlsl"

            TEXTURE2D(_LightningTexture);
            SAMPLER(sampler_LightningTexture);

            half SampleLightning(float2 textureCoordinate)
            {
                return SAMPLE_TEXTURE2D(
                    _LightningTexture,
                    sampler_LightningTexture,
                    textureCoordinate
                ).a;
            }
            
            half4 Fragment(Varyings input) : SV_Target
            {
                float2 screenUv = input.texcoord;

                half4 screenColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    screenUv
                );

                float borderWidth = max(_BorderWidth, 0.001);
                float scrollingCoordinate = _Time.y * _ScrollSpeed;

                float2 topTextureCoordinate = float2(
                    screenUv.x * _TextureTiling - scrollingCoordinate,
                    (1.0 - screenUv.y) / borderWidth
                );

                float2 bottomTextureCoordinate = float2(
                    screenUv.x * _TextureTiling + scrollingCoordinate,
                    screenUv.y / borderWidth
                );

                float2 leftTextureCoordinate = float2(
                    screenUv.y * _TextureTiling - scrollingCoordinate,
                    screenUv.x / borderWidth
                );

                float2 rightTextureCoordinate = float2(
                    screenUv.y * _TextureTiling + scrollingCoordinate,
                    (1.0 - screenUv.x) / borderWidth
                );

                half topLightning = SampleLightning(topTextureCoordinate) * step(1.0 - borderWidth, screenUv.y);
                half bottomLightning = SampleLightning(bottomTextureCoordinate) * step(screenUv.y, borderWidth);
                half leftLightning = SampleLightning(leftTextureCoordinate) * step(screenUv.x, borderWidth);
                half rightLightning = SampleLightning(rightTextureCoordinate) * step(1.0 - borderWidth, screenUv.x);

                half lightningMask = max(
                    max(topLightning, bottomLightning),
                    max(leftLightning, rightLightning)
                );

                half pulse = 0.75 + sin(_Time.y * 25.0) * 0.25;
                half effectStrength = lightningMask * pulse * _EffectStrength;

                screenColor.rgb += _LightningColor.rgb * effectStrength;

                return screenColor;
            }

            ENDHLSL
        }
    }
}
