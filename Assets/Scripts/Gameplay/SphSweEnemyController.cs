using System;
using Simulation;
using UnityEngine;
using Validation;

namespace Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SphSweEnemyController : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweSimulation simulation;

        [SerializeField, Required]
        private SphSweBattleCharacterMotor characterMotor;

        [SerializeField, Required]
        private SphSweGpuWaveGenerator waveGenerator;

        [SerializeField, Required]
        private SphSweLightningCannon lightningCannon;

        [SerializeField, Required]
        private Transform playerTransform;

        [SerializeField, Required]
        private SphSweObjectiveBall objectiveBall;

        [Header("Horizontal Movement")]

        [SerializeField, Min(0f)]
        private float attackHorizontalAlignmentTolerance = 0.2f;

        [SerializeField, Min(0f)]
        private float waveHorizontalAlignmentTolerance = 0.45f;

        [SerializeField, Min(0f)]
        private float patrolBoundaryInset = 0.25f;

        [Header("Wave Defense")]

        [SerializeField, Min(0f)]
        private float waveDefenseActivationDistanceBeforeCenter = 0.75f;

        [SerializeField, Min(0.01f)]
        private float minimumWaveGenerationInterval = 0.08f;

        [SerializeField, Min(0.01f)]
        private float maximumWaveGenerationInterval = 0.14f;

        private float remainingWaveGenerationInterval;
        private float patrolTargetPositionX;

        private void Awake()
        {
            ValidateReferences();
            ChooseNextPatrolTarget();
        }

        private void Update()
        {
            var simulationTransform = simulation.transform;
            var enemySimulationPosition = simulationTransform.InverseTransformPoint(transform.position);
            var playerSimulationPosition = simulationTransform.InverseTransformPoint(playerTransform.position);
            var objectiveBallSimulationPosition = simulationTransform.InverseTransformPoint(objectiveBall.transform.position);
            var isObjectiveBallOnEnemySide = IsObjectiveBallOnEnemySide(
                enemySimulationPosition,
                objectiveBallSimulationPosition
            );

            UpdateWaveDefense(
                Time.deltaTime,
                isObjectiveBallOnEnemySide,
                enemySimulationPosition.x,
                objectiveBallSimulationPosition.x
            );

            if (lightningCannon.IsReady)
            {
                AimAtAndAttackPlayer(
                    enemySimulationPosition.x,
                    playerSimulationPosition.x
                );
                return;
            }

            if (isObjectiveBallOnEnemySide)
            {
                MoveTowardHorizontalPosition(
                    enemySimulationPosition.x,
                    objectiveBallSimulationPosition.x
                );
                return;
            }

            UpdatePatrol(enemySimulationPosition.x);
        }

        private bool IsObjectiveBallOnEnemySide(
            Vector3 enemySimulationPosition,
            Vector3 objectiveBallSimulationPosition)
        {
            var distanceFromCenterToEnemy = enemySimulationPosition.z - simulation.SimulationCenter.y;
            var enemySideDirection = Mathf.Sign(distanceFromCenterToEnemy);

            if (enemySideDirection == 0f)
            {
                return false;
            }

            var objectiveBallDistanceTowardEnemy =
                (objectiveBallSimulationPosition.z - simulation.SimulationCenter.y)
                * enemySideDirection;

            return objectiveBallDistanceTowardEnemy
                   >= -waveDefenseActivationDistanceBeforeCenter;
        }

        private void UpdateWaveDefense(
            float deltaTime,
            bool isObjectiveBallOnEnemySide,
            float enemyPositionX,
            float objectiveBallPositionX)
        {
            if (!isObjectiveBallOnEnemySide)
            {
                remainingWaveGenerationInterval = 0f;
                return;
            }

            remainingWaveGenerationInterval -= deltaTime;

            if (remainingWaveGenerationInterval > 0f
                || !IsHorizontallyAligned(
                    enemyPositionX,
                    objectiveBallPositionX,
                    waveHorizontalAlignmentTolerance
                ))
            {
                return;
            }

            waveGenerator.TryGenerateWave();
            remainingWaveGenerationInterval = UnityEngine.Random.Range(
                minimumWaveGenerationInterval,
                maximumWaveGenerationInterval
            );
        }

        private void AimAtAndAttackPlayer(float enemyPositionX, float playerPositionX)
        {
            MoveTowardHorizontalPosition(enemyPositionX, playerPositionX);

            if (IsHorizontallyAligned(
                enemyPositionX,
                playerPositionX,
                attackHorizontalAlignmentTolerance
            ))
            {
                lightningCannon.TryFireLightningCannon();
            }
        }

        private void MoveTowardHorizontalPosition(float currentPositionX, float targetPositionX)
        {
            var horizontalDistance = targetPositionX - currentPositionX;

            if (Mathf.Abs(horizontalDistance) <= attackHorizontalAlignmentTolerance)
            {
                characterMotor.SetHorizontalDirection(0f);
                return;
            }

            characterMotor.SetHorizontalDirection(Mathf.Sign(horizontalDistance));
        }

        private static bool IsHorizontallyAligned(
            float currentPositionX,
            float targetPositionX,
            float alignmentTolerance)
        {
            return Mathf.Abs(targetPositionX - currentPositionX) <= alignmentTolerance;
        }

        private void UpdatePatrol(float enemyPositionX)
        {
            if (Mathf.Abs(patrolTargetPositionX - enemyPositionX)
                <= attackHorizontalAlignmentTolerance)
            {
                ChooseNextPatrolTarget();
            }

            MoveTowardHorizontalPosition(
                enemyPositionX,
                patrolTargetPositionX
            );
        }

        private void ChooseNextPatrolTarget()
        {
            var halfSimulationAreaWidth = simulation.SimulationAreaSize.x * 0.5f;
            var boundaryInset = Mathf.Min(patrolBoundaryInset, halfSimulationAreaWidth);
            var minimumPatrolPositionX =
                simulation.SimulationCenter.x - halfSimulationAreaWidth + boundaryInset;
            var maximumPatrolPositionX =
                simulation.SimulationCenter.x + halfSimulationAreaWidth - boundaryInset;

            patrolTargetPositionX = UnityEngine.Random.Range(
                minimumPatrolPositionX,
                maximumPatrolPositionX
            );
        }

        private void OnDisable()
        {
            if (characterMotor != null)
            {
                characterMotor.SetHorizontalDirection(0f);
            }
        }

        private void OnValidate()
        {
            attackHorizontalAlignmentTolerance = Mathf.Max(0f, attackHorizontalAlignmentTolerance);
            waveHorizontalAlignmentTolerance = Mathf.Max(0f, waveHorizontalAlignmentTolerance);
            patrolBoundaryInset = Mathf.Max(0f, patrolBoundaryInset);
            waveDefenseActivationDistanceBeforeCenter = Mathf.Max(
                0f,
                waveDefenseActivationDistanceBeforeCenter
            );
            minimumWaveGenerationInterval = Mathf.Max(0.01f, minimumWaveGenerationInterval);
            maximumWaveGenerationInterval = Mathf.Max(
                minimumWaveGenerationInterval,
                maximumWaveGenerationInterval
            );
        }

        private void ValidateReferences()
        {
            if (simulation == null)
            {
                throw new InvalidOperationException("Simulation is not assigned.");
            }

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

            if (playerTransform == null)
            {
                throw new InvalidOperationException("Player Transform is not assigned.");
            }

            if (objectiveBall == null)
            {
                throw new InvalidOperationException("Objective Ball is not assigned.");
            }
        }
    }
}
