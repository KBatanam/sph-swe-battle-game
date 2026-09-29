using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Validation;

namespace Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SphSwePlayerController : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweBattleCharacterMotor characterMotor;

        [SerializeField, Required]
        private SphSweGpuWaveGenerator waveGenerator;

        [Header("Input Actions")]

        [SerializeField, Required]
        private InputActionReference movementAction;

        [SerializeField, Required]
        private InputActionReference generateWaveAction;

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            movementAction.action.Enable();
            generateWaveAction.action.performed += OnGenerateWaveActionPerformed;
            generateWaveAction.action.Enable();
        }

        private void Update()
        {
            var movementInput = movementAction.action.ReadValue<Vector2>();
            characterMotor.SetHorizontalDirection(movementInput.x);
        }

        private void OnDisable()
        {
            characterMotor.SetHorizontalDirection(0f);
            generateWaveAction.action.performed -= OnGenerateWaveActionPerformed;
            movementAction.action.Disable();
            generateWaveAction.action.Disable();
        }

        private void OnGenerateWaveActionPerformed(
            InputAction.CallbackContext context)
        {
            var waveGenerationRequested = waveGenerator.TryGenerateWave();

#if UNITY_EDITOR
            if (waveGenerationRequested)
            {
                Debug.Log(
                    "Wave generation input performed.",
                    this
                );
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

        private void ValidateReferences()
        {
            if (characterMotor == null)
            {
                throw new InvalidOperationException("Character Motor is not assigned.");
            }

            if (waveGenerator == null)
            {
                throw new InvalidOperationException("Wave Generator is not assigned.");
            }

            if (movementAction == null)
            {
                throw new InvalidOperationException("Movement Action is not assigned.");
            }

            if (generateWaveAction == null)
            {
                throw new InvalidOperationException("Generate Wave Action is not assigned.");
            }
        }
    }
}