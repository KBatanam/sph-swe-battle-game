using Gameplay;
using UnityEngine;
using Validation;

namespace Rendering
{
    [DisallowMultipleComponent]
    public sealed class PlayerStunScreenEffectController : MonoBehaviour
    {
        [SerializeField, Required]
        private StunStatus playerStunStatus;

        [SerializeField, Required]
        private Material stunScreenEffectMaterial;

        private void OnEnable()
        {
            playerStunStatus.StunStateChanged += HandleStunStateChanged;
            SetEffectEnabled(playerStunStatus.IsStunned);
        }

        private void OnDisable()
        {
            playerStunStatus.StunStateChanged -= HandleStunStateChanged;
            SetEffectEnabled(false);
        }

        private void HandleStunStateChanged(bool isStunned)
        {
            SetEffectEnabled(isStunned);
        }

        private void SetEffectEnabled(bool isEnabled)
        {
            stunScreenEffectMaterial.SetFloat(
                StunScreenEffectShaderPropertyIds.EffectStrength,
                isEnabled ? 1f : 0f
            );
        }
    }
}