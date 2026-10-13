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
        private SpecialAttackController specialAttackController;

        [Header("Input Actions")]

        [SerializeField, Required]
        private InputActionReference movementAction;

        [SerializeField, Required]
        private InputActionReference generateWaveAction;

        [SerializeField, Required]
        private InputActionReference useSpecialAttackAction;
        
        [SerializeField, Required]
        private InputActionReference selectSpecialAttackAction;

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
            selectSpecialAttackAction.action.performed += OnSelectSpecialAttackActionPerformed;
            selectSpecialAttackAction.action.Enable();
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
            selectSpecialAttackAction.action.performed -= OnSelectSpecialAttackActionPerformed;
            movementAction.action.Disable();
            generateWaveAction.action.Disable();
            useSpecialAttackAction.action.Disable();
            selectSpecialAttackAction.action.Disable();
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
            var specialAttackUsed = specialAttackController.TryUseSelectedSpecialAttack();

#if UNITY_EDITOR
            if (specialAttackUsed)
            {
                Debug.Log("Selected special attack used.", this);
            }
#endif
        }
        
        private void OnSelectSpecialAttackActionPerformed(InputAction.CallbackContext context)
        {
            var selectionDirection = context.ReadValue<float>();

            if (selectionDirection > 0f)
            {
                specialAttackController.SelectPreviousSlot();
            }
            else if (selectionDirection < 0f)
            {
                specialAttackController.SelectNextSlot();
            }
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

            if (specialAttackController == null)
            {
                throw new InvalidOperationException("Special Attack Controller is not assigned.");
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
            
            if (selectSpecialAttackAction == null)
            {
                throw new InvalidOperationException("Select Special Attack Action is not assigned.");
            }
        }
    }
}
