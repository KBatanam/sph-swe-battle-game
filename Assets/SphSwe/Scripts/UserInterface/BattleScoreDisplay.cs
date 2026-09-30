using System;
using Cysharp.Text;
using SphSwe.Gameplay;
using SphSwe.Validation;
using TMPro;
using UnityEngine;

namespace SphSwe.UserInterface
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class BattleScoreDisplay : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private BattleScoreController battleScoreController;

        private TMP_Text scoreText;

        private void Awake()
        {
            ValidateReferences();
            scoreText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            battleScoreController.ScoreChanged += UpdateScoreDisplay;
            UpdateScoreDisplay(battleScoreController.PlayerScore, battleScoreController.EnemyScore);
        }

        private void OnDisable()
        {
            battleScoreController.ScoreChanged -= UpdateScoreDisplay;
        }

        private void UpdateScoreDisplay(int playerScore, int enemyScore)
        {
            scoreText.SetTextFormat(
                "PLAYER  {0}  -  {1}  ENEMY",
                playerScore,
                enemyScore
            );
        }

        private void ValidateReferences()
        {
            if (battleScoreController == null)
            {
                throw new InvalidOperationException("Battle Score Controller is not assigned.");
            }
        }
    }
}