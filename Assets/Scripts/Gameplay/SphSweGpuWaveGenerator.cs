using System;
using Gpu;
using UnityEngine;
using Validation;

namespace Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SphSweGpuWaveGenerator : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweGpuSimulation gpuSimulation;
        
        [SerializeField, Required]
        private SphSweStunStatus stunStatus;

        [Header("Wave Impulse")]

        [SerializeField, Min(0.001f)]
        private float waveImpulseRadius = 0.5f;

        [SerializeField, Min(0.001f)]
        private float waveImpulseStrength = 1f;

        /// <summary>
        /// このGameObjectの位置を中心として、
        /// transform.forward方向へ進む波を一度だけ発生させる。
        /// </summary>
        public bool TryGenerateWave()
        {
            if (stunStatus.IsStunned)
            {
                return false;
            }

            var sourceSimulation = gpuSimulation.SourceSimulation;
            var simulationTransform = sourceSimulation.transform;

            var localWaveGeneratorPosition = simulationTransform.InverseTransformPoint(transform.position);
            var localWaveGeneratorForward = simulationTransform.InverseTransformDirection(transform.forward);

            var waveImpulseCenterSimulationPosition = new Vector2(
                localWaveGeneratorPosition.x,
                localWaveGeneratorPosition.z
            );

            var simulationDirection = new Vector2(
                localWaveGeneratorForward.x,
                localWaveGeneratorForward.z
            );

            var waveGenerationRequested = gpuSimulation.TryApplyWaveImpulse(
                waveImpulseCenterSimulationPosition,
                simulationDirection,
                waveImpulseRadius,
                waveImpulseStrength
            );

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (waveGenerationRequested)
            {
                Debug.Log(
                    $"Wave generation requested. "
                    + $"Center: {waveImpulseCenterSimulationPosition}, "
                    + $"Direction: {simulationDirection.normalized}, "
                    + $"Radius: {waveImpulseRadius}, "
                    + $"Strength: {waveImpulseStrength}",
                    this
                );
            }
#endif

            return waveGenerationRequested;
        }
        
        private void Awake()
        {
            ValidateReferences();
        }
        
        private void ValidateReferences()
        {
            if (gpuSimulation == null)
            {
                throw new InvalidOperationException("GPU Simulation is not assigned.");
            }

            if (stunStatus == null)
            {
                throw new InvalidOperationException("Stun Status is not assigned.");
            }
        }
        
        private void OnValidate()
        {
            waveImpulseRadius = Mathf.Max(0.001f, waveImpulseRadius);
            waveImpulseStrength = Mathf.Max(0.001f, waveImpulseStrength);
        }
    }
}