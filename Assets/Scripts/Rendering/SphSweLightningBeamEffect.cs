using System;
using UnityEngine;
using Validation;

namespace Rendering
{
    [DisallowMultipleComponent]
    public sealed class SphSweLightningBeamEffect : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private LineRenderer lightningBeamRenderer;

        [SerializeField, Required]
        private Transform firingOrigin;

        [Header("Beam")]

        [SerializeField, Min(0f)]
        private float beamStartOffset = 0.25f;

        [SerializeField, Min(0f)]
        private float beamLength = 4.9f;

        [SerializeField, Min(0.01f)]
        private float displayDuration = 0.25f;

        private float remainingDisplayDuration;

        /// <summary>
        /// 雷砲を指定時間表示する。
        /// 既に表示中の場合は、残り表示時間を最初から数え直す。
        /// </summary>
        public void ShowLightningBeam()
        {
            remainingDisplayDuration = displayDuration;
            UpdateBeamPositions();
            lightningBeamRenderer.enabled = true;
        }

        private void Awake()
        {
            ValidateReferences();

            lightningBeamRenderer.useWorldSpace = true;
            lightningBeamRenderer.positionCount = 2;
            lightningBeamRenderer.enabled = false;
        }

        private void LateUpdate()
        {
            if (!lightningBeamRenderer.enabled)
            {
                return;
            }

            UpdateBeamPositions();

            remainingDisplayDuration -= Time.deltaTime;

            if (remainingDisplayDuration <= 0f)
            {
                lightningBeamRenderer.enabled = false;
            }
        }

        private void OnDisable()
        {
            remainingDisplayDuration = 0f;

            if (lightningBeamRenderer != null)
            {
                lightningBeamRenderer.enabled = false;
            }
        }

        private void UpdateBeamPositions()
        {
            GetBeamPositions(out var beamStartPosition, out var beamEndPosition);
            lightningBeamRenderer.SetPosition(0, beamStartPosition);
            lightningBeamRenderer.SetPosition(1, beamEndPosition);
        }

        private void ValidateReferences()
        {
            if (lightningBeamRenderer == null)
            {
                throw new InvalidOperationException(
                    "Lightning Beam Renderer is not assigned."
                );
            }

            if (firingOrigin == null)
            {
                throw new InvalidOperationException(
                    "Firing Origin is not assigned."
                );
            }
        }

        private void OnValidate()
        {
            beamStartOffset = Mathf.Max(0f, beamStartOffset);
            beamLength = Mathf.Max(0f, beamLength);
            displayDuration = Mathf.Max(0.01f, displayDuration);
        }
        
        /// <summary>
        /// 現在の砲口位置から、雷砲の始点と終点を計算する。
        /// 描画と当たり判定で同じ線分を共有するために使用する。
        /// </summary>
        public void GetBeamPositions(
            out Vector3 beamStartPosition,
            out Vector3 beamEndPosition)
        {
            var beamDirection = firingOrigin.forward;
            beamStartPosition = firingOrigin.position + beamDirection * beamStartOffset;
            beamEndPosition = beamStartPosition + beamDirection * beamLength;
        }
    }
}