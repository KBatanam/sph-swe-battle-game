using System;
using UnityEngine;
using UnityEngine.InputSystem;
using SphSwe.Validation;

namespace SphSwe.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private BattleCharacterMotor characterMotor;

        [SerializeField, Required]
        private WaveGenerator waveGenerator;

        [SerializeField, Required]
        private LightningCannon lightningCannon;

        [Header("Input Actions")]

        [SerializeField, Required]
        private InputActionReference movementAction;

        [SerializeField, Required]
        private InputActionReference generateWaveAction;

        [SerializeField, Required]
        private InputActionReference useSpecialAttackAction;

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            movementAction.action.Enable();
            generateWaveAction.action.performed += OnGenerateWaveActionPerformed;
            generateWaveAction.action.Enable();
            useSpecialAttackAction.action.performed += OnUseSpecialAttackActionPerformed;
            useSpecialAttackAction.action.Enable();
        }

        private void Update()
        {
            var movementInput = movementAction.action.ReadValue<Vector2>();

            characterMotor.SetHorizontalDirection(
                movementInput.x
            );
        }

        private void OnDisable()
        {
            characterMotor.SetHorizontalDirection(0f);
            generateWaveAction.action.performed -= OnGenerateWaveActionPerformed;
            useSpecialAttackAction.action.performed -= OnUseSpecialAttackActionPerformed;
            movementAction.action.Disable();
            generateWaveAction.action.Disable();
            useSpecialAttackAction.action.Disable();
        }

        private void OnGenerateWaveActionPerformed(InputAction.CallbackContext context)
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
#endif
        }

        private void OnUseSpecialAttackActionPerformed(InputAction.CallbackContext context)
        {
            var lightningCannonFired = lightningCannon.TryFireLightningCannon();

#if UNITY_EDITOR
            if (lightningCannonFired)
            {
                Debug.Log(
                    "Lightning cannon fired.",
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

            if (lightningCannon == null)
            {
                throw new InvalidOperationException("Lightning Cannon is not assigned.");
            }

            if (movementAction == null)
            {
                throw new InvalidOperationException("Movement Action is not assigned.");
            }

            if (generateWaveAction == null)
            {
                throw new InvalidOperationException("Generate Wave Action is not assigned.");
            }

            if (useSpecialAttackAction == null)
            {
                throw new InvalidOperationException("Use Special Attack Action is not assigned.");
            }
        }
    }
}
