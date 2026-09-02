Shader "SphSwe/Lightning"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.65, 0.8, 1, 1)
        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _NoiseScale ("Noise Scale", Range(2, 60)) = 24
        _ScrollSpeed ("Scroll Speed", Range(0, 30)) = 10
        _Intensity ("Intensity", Range(0, 10)) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        // 加算合成: ピクセル値をそのまま画面に足す。
        // 黒い部分は透明に見えるので、稲妻や火花などの光の表現に向く。
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "AdditiveUnlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                // LineRenderer はC#から頂点カラーを設定できる（フェードに使う）。
                float4 color : COLOR;
                // LineRenderer のUV: x=線に沿って0-1、y=線幅方向に0-1。
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR0;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _CoreColor;
                half _NoiseScale;
                half _ScrollSpeed;
                half _Intensity;
            CBUFFER_END

            // 座標から擬似乱数(0-1)を作る。sinの小数部分を利用した定番の書き方。
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            // valueNoise: 格子点ごとの乱数を滑らにつなげた連続なノイズ。
            float valueNoise(float2 p)
            {
                float2 cellIndex = floor(p);
                float2 cellFraction = frac(p);
                // smoothstep相当の補間曲線で、格子間を滑らかにつなぐ。
                float2 blendWeight = cellFraction * cellFraction * (3.0 - 2.0 * cellFraction);
                float corner00 = hash(cellIndex);
                float corner10 = hash(cellIndex + float2(1, 0));
                float corner01 = hash(cellIndex + float2(0, 1));
                float corner11 = hash(cellIndex + float2(1, 1));
                return lerp(
                    lerp(corner00, corner10, blendWeight.x),
                    lerp(corner01, corner11, blendWeight.x),
                    blendWeight.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ノイズをx方向に流して、エネルギーが線を走る感じを出す。
                float t = _Time.y * _ScrollSpeed;
                float noise = valueNoise(float2(IN.uv.x * _NoiseScale - t, IN.uv.y * 2.0));

                // 線幅の中央ほど白く、外側ほど青くする。
                float core = 1.0 - abs(IN.uv.y * 2.0 - 1.0);   // 中央=1, 縁=0
                half3 rgb = lerp(_BaseColor.rgb, _CoreColor.rgb, core * core);
                rgb *= _Intensity * (0.55 + 0.45 * noise);

                // alphaにはノイズの明るさとC#側のフェード(IN.color.a)を掛ける。
                return half4(rgb, _BaseColor.a * (0.55 + 0.45 * noise)) * IN.color;
            }
            ENDHLSL
        }
    }
}
