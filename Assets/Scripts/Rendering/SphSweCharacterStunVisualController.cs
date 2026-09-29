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

        private MaterialPropertyBlock materialPropertyBlock;

        private void OnEnable()
        {
            materialPropertyBlock ??= new MaterialPropertyBlock();

            stunStatus.StunStateChanged += HandleStunStateChanged;
            SetStunEffectEnabled(stunStatus.IsStunned);
        }

        private void OnDisable()
        {
            stunStatus.StunStateChanged -= HandleStunStateChanged;
            SetStunEffectEnabled(false);
        }

        private void HandleStunStateChanged(bool isStunned)
        {
            SetStunEffectEnabled(isStunned);
        }

        private void SetStunEffectEnabled(bool isEnabled)
        {
            characterRenderer.GetPropertyBlock(materialPropertyBlock);

            materialPropertyBlock.SetFloat(
                SphSweCharacterShaderPropertyIds.StunEffectStrength,
                isEnabled ? 1f : 0f
            );

            characterRenderer.SetPropertyBlock(materialPropertyBlock);
        }
    }
}