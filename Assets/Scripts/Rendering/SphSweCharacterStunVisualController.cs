using Gameplay;
using UnityEngine;
using Validation;

namespace Rendering
{
    [DisallowMultipleComponent]
    public sealed class SphSweCharacterStunVisualController : MonoBehaviour
    {
        [SerializeField, Required]
        private SphSweStunStatus stunStatus;

        [SerializeField, Required]
        private Renderer characterRenderer;

        [SerializeField, Required]
        private ParticleSystem characterStunParticleSystem;

        private MaterialPropertyBlock materialPropertyBlock;

        private void OnEnable()
        {
            materialPropertyBlock ??= new MaterialPropertyBlock();

            stunStatus.StunStateChanged += HandleStunStateChanged;
            SetStunVisualEnabled(stunStatus.IsStunned);
        }

        private void OnDisable()
        {
            stunStatus.StunStateChanged -= HandleStunStateChanged;
            SetStunVisualEnabled(false);
        }

        private void HandleStunStateChanged(bool isStunned)
        {
            SetStunVisualEnabled(isStunned);
        }

        private void SetStunVisualEnabled(bool isEnabled)
        {
            SetCharacterEmissionEnabled(isEnabled);
            SetCharacterStunParticlesEnabled(isEnabled);
        }

        private void SetCharacterEmissionEnabled(bool isEnabled)
        {
            characterRenderer.GetPropertyBlock(materialPropertyBlock);

            materialPropertyBlock.SetFloat(
                SphSweCharacterShaderPropertyIds.StunEffectStrength,
                isEnabled ? 1f : 0f
            );

            characterRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        private void SetCharacterStunParticlesEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                characterStunParticleSystem.Play(true);
                return;
            }

            characterStunParticleSystem.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }
}