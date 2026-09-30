using System;
using SphSwe.Simulation;
using UnityEngine;
using UnityEngine.Serialization;
using SphSwe.Validation;

namespace SphSwe.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweSimulation simulation;

        [SerializeField, Required]
        private BattleCharacterMotor characterMotor;

        [SerializeField, Required]
        private WaveGenerator waveGenerator;

        [SerializeField, Required]
        private LightningCannon lightningCannon;

        [SerializeField, Required]
        private Transform playerTransform;

        [SerializeField, Required]
        private ObjectiveBall objectiveBall;

        [Header("Horizontal Movement")]

        [SerializeField, Min(0f)]
        private float attackHorizontalAlignmentTolerance = 0.2f;

        [SerializeField, Min(0f)]
        private float waveHorizontalAlignmentTolerance = 0.45f;

        [FormerlySerializedAs("patrolBoundaryInset")]
        [SerializeField, Min(0f)]
        private float horizontalBoundaryInset = 0.25f;

        [Header("Lightning Attack")]

        [SerializeField, Min(0f)]
        private float minimumLightningAimDuration = 0.8f;

        [SerializeField, Min(0f)]
        private float maximumLightningAimDuration = 1.1f;

        [SerializeField, Range(0f, 1f)]
        private float lightningAttackAccuracy = 0.6f;

        [SerializeField, Min(0f)]
        private float minimumLightningMissOffset = 0.9f;

        [SerializeField, Min(0f)]
        private float maximumLightningMissOffset = 1.4f;

        [Header("Wave Generation")]

        [SerializeField, Min(0f)]
        private float waveDefenseActivationDistanceBeforeCenter = 0.75f;

        [SerializeField, Min(0.01f)]
        private float minimumWaveGenerationInterval = 0.08f;

        [SerializeField, Min(0.01f)]
        private float maximumWaveGenerationInterval = 0.14f;

        [SerializeField, Min(0.01f)]
        private float minimumOffensiveWaveGenerationInterval = 0.3f;

        [SerializeField, Min(0.01f)]
        private float maximumOffensiveWaveGenerationInterval = 0.5f;

        private float remainingWaveGenerationInterval;
        private float remainingLightningAimDuration;
        private float lockedLightningTargetPositionX;
        private bool isPreparingLightningAttack;

        private void Awake()
        {
            ValidateReferences();
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

            UpdateWaveGeneration(
                Time.deltaTime,
                isObjectiveBallOnEnemySide,
                enemySimulationPosition.x,
                objectiveBallSimulationPosition.x
            );

            if (lightningCannon.IsReady)
            {
                UpdateLightningAttack(
                    Time.deltaTime,
                    enemySimulationPosition.x,
                    playerSimulationPosition.x
                );
                return;
            }

            isPreparingLightningAttack = false;

            MoveTowardHorizontalPosition(
                enemySimulationPosition.x,
                objectiveBallSimulationPosition.x
            );
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

        private void UpdateWaveGeneration(
            float deltaTime,
            bool isObjectiveBallOnEnemySide,
            float enemyPositionX,
            float objectiveBallPositionX)
        {
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

            if (!waveGenerator.TryGenerateWave())
            {
                return;
            }

            remainingWaveGenerationInterval = isObjectiveBallOnEnemySide
                ? UnityEngine.Random.Range(
                    minimumWaveGenerationInterval,
                    maximumWaveGenerationInterval
                )
                : UnityEngine.Random.Range(
                    minimumOffensiveWaveGenerationInterval,
                    maximumOffensiveWaveGenerationInterval
                );
        }

        private void UpdateLightningAttack(
            float deltaTime,
            float enemyPositionX,
            float playerPositionX)
        {
            if (!isPreparingLightningAttack)
            {
                BeginLightningAttack(playerPositionX);
            }

            MoveTowardHorizontalPosition(
                enemyPositionX,
                lockedLightningTargetPositionX
            );

            remainingLightningAimDuration -= deltaTime;

            if (remainingLightningAimDuration > 0f ||
                !IsHorizontallyAligned(
                    enemyPositionX,
                    lockedLightningTargetPositionX,
                    attackHorizontalAlignmentTolerance
                ))
            {
                return;
            }

            if (lightningCannon.TryFireLightningCannon())
            {
                isPreparingLightningAttack = false;
            }
        }

        /// <summary>
        /// 雷砲の照準位置を現在のPlayer位置付近に固定する。
        /// 発射まで照準位置を更新しないため、Playerは予告時間中に回避できる。
        /// </summary>
        private void BeginLightningAttack(float playerPositionX)
        {
            var halfSimulationAreaWidth =
                simulation.SimulationAreaSize.x * 0.5f;

            var boundaryInset = Mathf.Min(
                horizontalBoundaryInset,
                halfSimulationAreaWidth
            );

            var minimumAttackPositionX =
                simulation.SimulationCenter.x
                - halfSimulationAreaWidth
                + boundaryInset;

            var maximumAttackPositionX =
                simulation.SimulationCenter.x
                + halfSimulationAreaWidth
                - boundaryInset;

            var aimOffset = CalculateLightningAimOffset();

            lockedLightningTargetPositionX = Mathf.Clamp(
                playerPositionX + aimOffset,
                minimumAttackPositionX,
                maximumAttackPositionX
            );

            remainingLightningAimDuration = UnityEngine.Random.Range(
                minimumLightningAimDuration,
                maximumLightningAimDuration
            );

            isPreparingLightningAttack = true;
        }

        /// <summary>
        /// 命中を狙う場合はPlayerの現在位置をそのまま照準位置とし、
        /// 外す場合は当たり判定の幅を十分に超える位置へ照準をずらす。
        /// 命中を狙った場合も照準位置は固定されるため、予告時間中に回避できる。
        /// </summary>
        private float CalculateLightningAimOffset()
        {
            if (UnityEngine.Random.value <= lightningAttackAccuracy)
            {
                return 0f;
            }

            var missDirection = UnityEngine.Random.value < 0.5f
                ? -1f
                : 1f;

            var missDistance = UnityEngine.Random.Range(
                minimumLightningMissOffset,
                maximumLightningMissOffset
            );

            return missDirection * missDistance;
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
            horizontalBoundaryInset = Mathf.Max(0f, horizontalBoundaryInset);
            minimumLightningAimDuration = Mathf.Max(0f, minimumLightningAimDuration);
            maximumLightningAimDuration = Mathf.Max(
                minimumLightningAimDuration,
                maximumLightningAimDuration
            );
            lightningAttackAccuracy = Mathf.Clamp01(lightningAttackAccuracy);
            minimumLightningMissOffset = Mathf.Max(0f, minimumLightningMissOffset);
            maximumLightningMissOffset = Mathf.Max(
                minimumLightningMissOffset,
                maximumLightningMissOffset
            );
            waveDefenseActivationDistanceBeforeCenter = Mathf.Max(
                0f,
                waveDefenseActivationDistanceBeforeCenter
            );
            minimumWaveGenerationInterval = Mathf.Max(0.01f, minimumWaveGenerationInterval);
            maximumWaveGenerationInterval = Mathf.Max(
                minimumWaveGenerationInterval,
                maximumWaveGenerationInterval
            );
            minimumOffensiveWaveGenerationInterval = Mathf.Max(
                0.01f,
                minimumOffensiveWaveGenerationInterval
            );
            maximumOffensiveWaveGenerationInterval = Mathf.Max(
                minimumOffensiveWaveGenerationInterval,
                maximumOffensiveWaveGenerationInterval
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
