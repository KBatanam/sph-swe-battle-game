using System;
using Simulation;
using UnityEngine;
using Validation;

namespace Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BattleCharacterMotor : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweSimulation simulation;

        [SerializeField, Required]
        private StunStatus stunStatus;
        
        [Header("Movement")]

        [SerializeField, Min(0f)]
        private float horizontalMovementSpeed = 2f;

        [SerializeField, Min(0f)]
        private float horizontalMovementBoundaryInset = 0.25f;
        
        private float horizontalMovementDirection;

        /// <summary>
        /// 左右の移動方向を設定する。
        /// -1で左、0で停止、1で右へ移動する。
        /// Player入力、Enemy AIおよび将来のネットワーク入力で共通して使用する。
        /// </summary>
        public void SetHorizontalDirection(float movementDirection)
        {
            horizontalMovementDirection = Mathf.Clamp(
                movementDirection,
                -1f,
                1f
            );
        }

        private void Awake()
        {
            ValidateReferences();
        }

        private void Update()
        {
            if (stunStatus.IsStunned)
            {
                return;
            }

            ApplyHorizontalMovement(Time.deltaTime);
        }

        private void ApplyHorizontalMovement(float deltaTime)
        {
            var simulationTransform = simulation.transform;
            var localPosition = simulationTransform.InverseTransformPoint(transform.position);
            localPosition.x += horizontalMovementDirection * horizontalMovementSpeed * deltaTime;

            var halfSimulationAreaWidth = simulation.SimulationAreaSize.x * 0.5f;
            var boundaryInset = Mathf.Min(
                horizontalMovementBoundaryInset,
                halfSimulationAreaWidth
            );

            var minimumPositionX = simulation.SimulationCenter.x - halfSimulationAreaWidth + boundaryInset;
            var maximumPositionX = simulation.SimulationCenter.x + halfSimulationAreaWidth - boundaryInset;

            localPosition.x = Mathf.Clamp(localPosition.x, minimumPositionX, maximumPositionX);

            transform.position = simulationTransform.TransformPoint(localPosition);
        }

        private void OnValidate()
        {
            horizontalMovementSpeed = Mathf.Max(0f, horizontalMovementSpeed);
            horizontalMovementBoundaryInset = Mathf.Max(0f, horizontalMovementBoundaryInset);
        }
        
        private void ValidateReferences()
        {
            if (simulation == null)
            {
                throw new InvalidOperationException("Simulation is not assigned.");
            }

            if (stunStatus == null)
            {
                throw new InvalidOperationException("Stun Status is not assigned.");
            }
        }
    }
}