using UnityEngine;
using UnityEngine.InputSystem;
using Validation;

namespace Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SphSweGpuWaveGenerationInput : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweGpuWaveGenerator waveGenerator;

        [SerializeField, Required]
        private InputActionReference generateWaveAction;

        private void OnEnable()
        {
            if (generateWaveAction == null)
            {
                return;
            }

            generateWaveAction.action.performed += OnGenerateWaveActionPerformed;
            generateWaveAction.action.Enable();
        }

        private void OnDisable()
        {
            if (generateWaveAction == null)
            {
                return;
            }

            generateWaveAction.action.performed -= OnGenerateWaveActionPerformed;
            generateWaveAction.action.Disable();
        }

        private void OnGenerateWaveActionPerformed(
            InputAction.CallbackContext context)
        {
            var waveGenerationRequested = waveGenerator.TryGenerateWave();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (waveGenerationRequested)
            {
                Debug.Log("Wave generation input performed.", this);
            }
            else
            {
                Debug.LogWarning(
                    "Wave generation input was performed, but the GPU request failed.",
                    this
                );
            }
#endif
        }
    }
}