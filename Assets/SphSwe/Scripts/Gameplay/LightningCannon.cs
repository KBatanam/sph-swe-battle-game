using System;
using SphSwe.Rendering;
using UnityEngine;
using SphSwe.Validation;

namespace SphSwe.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LightningCannon : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private LightningBeamEffect lightningBeamEffect;
        
        [SerializeField, Required]
        private StunStatus stunStatus;

        [Header("Cooldown")]

        [SerializeField, Min(0f)]
        private float cooldownDuration = 5f;

        private float remainingCooldownDuration;

        public bool IsReady => remainingCooldownDuration <= 0f;

        public float RemainingCooldownDuration => remainingCooldownDuration;
        
        private const int MaximumHitColliderCount = 16;

        [Header("Hit Detection")]

        [SerializeField, Min(0.01f)]
        private float hitBoxWidth = 0.4f;

        [SerializeField, Min(0.01f)]
        private float hitBoxHeight = 0.6f;

        [SerializeField, Min(0f)]
        private float stunDuration = 1f;

        [SerializeField]
        private LayerMask hitDetectionLayerMask = ~0;

        private readonly Collider[] hitColliders = new Collider[MaximumHitColliderCount];

        /// <summary>
        /// クールタイムが完了している場合に雷砲を発射する。
        /// 発射に成功した場合はtrueを返す。
        /// </summary>
        public bool TryFireLightningCannon()
        {
            if (stunStatus.IsStunned || !IsReady)
            {
                return false;
            }

            lightningBeamEffect.ShowLightningBeam();
            ApplyStunToHitCharacters();

            remainingCooldownDuration = cooldownDuration;

            return true;
        }
        
        private void ApplyStunToHitCharacters()
        {
            lightningBeamEffect.GetBeamPositions(out var beamStartPosition, out var beamEndPosition);
            
            var beamVector = beamEndPosition - beamStartPosition;
            var beamLength = beamVector.magnitude;

            if (beamLength <= 0f)
            {
                return;
            }

            var beamDirection = beamVector / beamLength;
            var hitBoxCenter = (beamStartPosition + beamEndPosition) * 0.5f;

            var hitBoxHalfExtents = new Vector3(
                hitBoxWidth * 0.5f,
                hitBoxHeight * 0.5f,
                beamLength * 0.5f
            );

            var hitBoxRotation = Quaternion.LookRotation(
                beamDirection,
                Vector3.up
            );

            var hitColliderCount = Physics.OverlapBoxNonAlloc(
                hitBoxCenter,
                hitBoxHalfExtents,
                hitColliders,
                hitBoxRotation,
                hitDetectionLayerMask,
                QueryTriggerInteraction.Ignore
            );

            for (var hitColliderIndex = 0; hitColliderIndex < hitColliderCount; hitColliderIndex++)
            {
                var hitCollider = hitColliders[hitColliderIndex];
                var hitStunStatus = hitCollider.GetComponentInParent<StunStatus>();

                if (hitStunStatus == null || hitStunStatus == stunStatus)
                {
                    continue;
                }

                hitStunStatus.ApplyStun(stunDuration);
            }
        }
        
        private void Awake()
        {
            ValidateReferences();

            // ゲーム開始直後には必殺技を発射できない仕様のため、
            // 初回クールタイムも最大値から開始する。
            remainingCooldownDuration = cooldownDuration;
        }

        private void Update()
        {
            if (IsReady)
            {
                return;
            }

            remainingCooldownDuration = Mathf.Max(0f, remainingCooldownDuration - Time.deltaTime);
        }

        private void ValidateReferences()
        {
            if (lightningBeamEffect == null)
            {
                throw new InvalidOperationException(
                    "Lightning Beam Effect is not assigned."
                );
            }
            
            if (stunStatus == null)
            {
                throw new InvalidOperationException(
                    "Stun Status is not assigned."
                );
            }
        }

        private void OnValidate()
        {
            cooldownDuration = Mathf.Max(0f, cooldownDuration);
            hitBoxWidth = Mathf.Max(0.01f, hitBoxWidth);
            hitBoxHeight = Mathf.Max(0.01f, hitBoxHeight);
            stunDuration = Mathf.Max(0f, stunDuration);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (lightningBeamEffect == null)
            {
                return;
            }

            lightningBeamEffect.GetBeamPositions(out var beamStartPosition, out var beamEndPosition);
          
            var beamVector = beamEndPosition - beamStartPosition;

            var beamLength = beamVector.magnitude;

            if (beamLength <= 0f)
            {
                return;
            }

            var hitBoxCenter = (beamStartPosition + beamEndPosition) * 0.5f;

            var hitBoxRotation = Quaternion.LookRotation(beamVector / beamLength, Vector3.up);

            var previousColor = Gizmos.color;
            var previousMatrix = Gizmos.matrix;

            Gizmos.color = new Color(1f, 0.8f, 0f, 0.15f);
            Gizmos.matrix = Matrix4x4.TRS(
                hitBoxCenter,
                hitBoxRotation,
                Vector3.one
            );

            var hitBoxSize = new Vector3(
                hitBoxWidth,
                hitBoxHeight,
                beamLength
            );

            Gizmos.DrawCube(Vector3.zero, hitBoxSize);

            Gizmos.color = new Color(1f, 0.8f, 0f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, hitBoxSize);

            Gizmos.color = previousColor;
            Gizmos.matrix = previousMatrix;
        }
    }
}
