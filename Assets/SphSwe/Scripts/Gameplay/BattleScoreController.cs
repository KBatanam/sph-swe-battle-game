using System;
using SphSwe.Simulation;
using SphSwe.Validation;
using UnityEngine;

namespace SphSwe.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BattleScoreController : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweSimulation simulation;

        [SerializeField, Required]
        private ObjectiveBall objectiveBall;

        [Header("Goal Detection")]

        [SerializeField, Min(0f)]
        private float goalDetectionDistanceFromBoundary = 0.05f;

        public int PlayerScore { get; private set; }
        public int EnemyScore { get; private set; }

        public event Action<int, int> ScoreChanged;

        private void Awake()
        {
            ValidateReferences();
        }

        private void Update()
        {
            DetectGoal();
        }

        private void DetectGoal()
        {
            var localBallPosition = simulation.transform.InverseTransformPoint(objectiveBall.transform.position);
            var halfSimulationAreaSize = simulation.SimulationAreaSize * 0.5f;
            var minimumPosition = simulation.SimulationCenter - halfSimulationAreaSize;
            var maximumPosition = simulation.SimulationCenter + halfSimulationAreaSize;

            if (localBallPosition.z >= maximumPosition.y - goalDetectionDistanceFromBoundary)
            {
                AddPlayerScore();
                return;
            }

            if (localBallPosition.z <= minimumPosition.y + goalDetectionDistanceFromBoundary)
            {
                AddEnemyScore();
            }
        }

        private void AddPlayerScore()
        {
            PlayerScore++;
            CompleteScoring();
        }

        private void AddEnemyScore()
        {
            EnemyScore++;
            CompleteScoring();
        }

        private void CompleteScoring()
        {
            objectiveBall.ResetToSimulationCenter();
            ScoreChanged?.Invoke(PlayerScore, EnemyScore);
        }

        private void ValidateReferences()
        {
            if (simulation == null)
            {
                throw new InvalidOperationException("Simulation is not assigned.");
            }

            if (objectiveBall == null)
            {
                throw new InvalidOperationException("Objective Ball is not assigned.");
            }
        }

        private void OnValidate()
        {
            goalDetectionDistanceFromBoundary = Mathf.Max(0f, goalDetectionDistanceFromBoundary);
        }
    }
}