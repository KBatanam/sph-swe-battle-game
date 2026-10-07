using SphSwe.Gameplay;
using UnityEngine;
using SphSwe.Validation;

namespace SphSwe.Rendering
{
    [DisallowMultipleComponent]
    public sealed class CharacterStunVisualController : MonoBehaviour
    {
        [SerializeField, Required]
        private StunStatus stunStatus;

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
                CharacterShaderPropertyIds.StunEffectStrength,
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
