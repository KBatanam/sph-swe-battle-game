Shader "SphSwe/Lightning"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.65, 0.8, 1, 1)
        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _Amplitude ("Amplitude", Range(0, 0.4)) = 0.16
        _NoiseFrequency ("Noise Frequency", Range(2, 40)) = 14
        _RegenInterval ("Regen Interval", Range(0.02, 0.2)) = 0.045
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
                // LineRenderer を useWorldSpace で使うため、
                // positionOS には最初からワールド座標が入ってくる。
                float4 positionOS : POSITION;
                // C#がフェード用に設定する頂点カラー。
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
                half _Amplitude;
                half _NoiseFrequency;
                half _RegenInterval;
                half _NoiseScale;
                half _ScrollSpeed;
                half _Intensity;
            CBUFFER_END

            // C#から渡される稲妻の始点と終点（ワールド座標）。
            float3 _Start;
            float3 _End;

            // 座標から擬似乱数(0-1)を作る。sinの小数部分を利用した定番の書き方。
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            // valueNoise: 格子点ごとの乱数を滑らかにつなげた連続なノイズ。
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

                // 線分上の位置 (0=始点, 1=終点)。
                float t = IN.uv.x;

                // 時間を一定間隔で刻む。この値が変わる瞬間に稲妻の形が
                // 変わり、ちらつきとして見える。
                float seed = floor(_Time.y / _RegenInterval);

                // 線の方向と、それに垂直な2軸（ずらす方向）を作る。
                float3 direction = normalize(_End - _Start);
                float3 side1 = cross(direction, float3(0, 1, 0));
                if (dot(side1, side1) < 0.001)
                {
                    // 線が真上を向いているときはCrossが退化するので別軸を使う。
                    side1 = cross(direction, float3(1, 0, 0));
                }
                side1 = normalize(side1);
                float3 side2 = normalize(cross(direction, side1));

                // ノイズで2軸それぞれにランダムな揺れを作る。
                float wave1 = valueNoise(float2(t * _NoiseFrequency, seed)) * 2.0 - 1.0;
                float wave2 = valueNoise(float2(t * _NoiseFrequency, seed + 31.0)) * 2.0 - 1.0;

                // sinカーブで端ほど揺れを小さくする（両端が外れないように）。
                float envelope = sin(3.14159265 * t);

                // 距離が長いほど大きく揺らす。
                float shakeScale = distance(_Start, _End) * _Amplitude;

                // 一直線上の頂点を、揺れ分だけ横にずらしてジグザグを作る。
                float3 positionWS = IN.positionOS.xyz
                    + side1 * (wave1 * envelope * shakeScale)
                    + side2 * (wave2 * envelope * shakeScale);

                OUT.positionHCS = TransformWorldToHClip(positionWS);
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
