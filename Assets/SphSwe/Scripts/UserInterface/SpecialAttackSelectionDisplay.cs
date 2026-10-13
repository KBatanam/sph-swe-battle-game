using System;
using SphSwe.Gameplay;
using SphSwe.Validation;
using TMPro;
using UnityEngine;

namespace SphSwe.UserInterface
{
    /// <summary>
    /// 選択中の必殺技と、その前後にある必殺技を表示する。
    /// スロットの端では反対側へ循環する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpecialAttackSelectionDisplay : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SpecialAttackController specialAttackController;

        [SerializeField, Required]
        private TMP_Text previousSpecialAttackText;

        [SerializeField, Required]
        private TMP_Text selectedSpecialAttackText;

        [SerializeField, Required]
        private TMP_Text nextSpecialAttackText;

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            specialAttackController.SelectedSlotChanged += UpdateDisplay;
            UpdateDisplay();
        }

        private void OnDisable()
        {
            specialAttackController.SelectedSlotChanged -= UpdateDisplay;
        }

        private void UpdateDisplay()
        {
            var selectedSlotIndex = specialAttackController.SelectedSlotIndex;

            previousSpecialAttackText.SetText(
                specialAttackController.GetDisplayName(
                    selectedSlotIndex - 1
                )
            );

            selectedSpecialAttackText.SetText(
                specialAttackController.GetDisplayName(
                    selectedSlotIndex
                )
            );

            nextSpecialAttackText.SetText(
                specialAttackController.GetDisplayName(
                    selectedSlotIndex + 1
                )
            );
        }

        private void ValidateReferences()
        {
            if (specialAttackController == null)
            {
                throw new InvalidOperationException("Special Attack Controller is not assigned.");
            }

            if (previousSpecialAttackText == null)
            {
                throw new InvalidOperationException("Previous Special Attack Text is not assigned.");
            }

            if (selectedSpecialAttackText == null)
            {
                throw new InvalidOperationException("Selected Special Attack Text is not assigned.");
            }

            if (nextSpecialAttackText == null)
            {
                throw new InvalidOperationException("Next Special Attack Text is not assigned.");
            }
        }
    }
}