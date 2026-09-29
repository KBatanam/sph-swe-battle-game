using System;
using Cysharp.Text;
using SphSwe.Gameplay;
using TMPro;
using UnityEngine;
using SphSwe.Validation;

namespace SphSwe.UserInterface
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class SpecialAttackCooldownDisplay : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private LightningCannon lightningCannon;

        [Header("Display")]

        [SerializeField]
        private string characterName = "PLAYER";

        [SerializeField]
        private Color cooldownTextColor = Color.white;

        [SerializeField]
        private Color readyTextColor = new(1f, 0.8f, 0.1f, 1f);

        private TMP_Text cooldownText;
        private int previousDisplayedTenths = -1;
        private bool wasReady;

        private void Awake()
        {
            ValidateReferences();
            cooldownText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            previousDisplayedTenths = -1;
            wasReady = false;
        }

        private void Update()
        {
            if (lightningCannon.IsReady)
            {
                UpdateReadyDisplay();
                return;
            }

            UpdateCooldownDisplay();
        }

        private void UpdateReadyDisplay()
        {
            if (wasReady)
            {
                return;
            }

            cooldownText.SetTextFormat(
                "{0} SPECIAL: READY",
                characterName
            );

            cooldownText.color = readyTextColor;
            previousDisplayedTenths = -1;
            wasReady = true;
        }

        private void UpdateCooldownDisplay()
        {
            var remainingCooldownTenths = Mathf.CeilToInt(lightningCannon.RemainingCooldownDuration * 10f);

            if (!wasReady
                && remainingCooldownTenths == previousDisplayedTenths)
            {
                return;
            }

            var displayedRemainingCooldown = remainingCooldownTenths * 0.1f;

            cooldownText.SetTextFormat(
                "{0} SPECIAL: {1:F1}",
                characterName,
                displayedRemainingCooldown
            );

            cooldownText.color = cooldownTextColor;
            previousDisplayedTenths = remainingCooldownTenths;
            wasReady = false;
        }

        private void ValidateReferences()
        {
            if (lightningCannon == null)
            {
                throw new InvalidOperationException(
                    "Lightning Cannon is not assigned."
                );
            }
        }
    }
}
