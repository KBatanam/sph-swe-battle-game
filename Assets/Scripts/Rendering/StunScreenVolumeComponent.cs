using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    [Serializable]
    [VolumeComponentMenu("Battle/Stun Screen Effect")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class StunScreenVolumeComponent : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("画面周辺の雷エフェクトの強度。")]
        public ClampedFloatParameter effectStrength = new(0f, 0f, 1f);

        [Tooltip("雷の表示色。")]
        public ColorParameter lightningColor =
            new(
                new Color(1f, 0.8f, 0.1f, 1f),
                true,
                true,
                true
            );

        [Tooltip("画面端から雷を表示する範囲。")]
        public ClampedFloatParameter borderWidth = new(0.15f, 0.01f, 0.5f);

        [Tooltip("雷テクスチャを繰り返す回数。")]
        public MinFloatParameter textureTiling = new(2f, 0.01f);

        [Tooltip("雷テクスチャの移動速度。")]
        public FloatParameter scrollSpeed = new(1f);

        /// <summary>
        /// 雷の表示強度が0より大きい場合のみ描画する。
        /// </summary>
        public bool IsActive()
        {
            return effectStrength.value > 0f;
        }

        /// <summary>
        /// 画面全体の入力テクスチャを使用するため、
        /// タイル内だけで完結する処理としては扱わない。
        /// </summary>
        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
