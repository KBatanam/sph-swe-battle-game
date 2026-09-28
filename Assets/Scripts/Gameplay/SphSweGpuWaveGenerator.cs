using System;
using Gpu;
using UnityEngine;
using Validation;
using Cysharp.Text;

namespace Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SphSweGpuWaveGenerator : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweGpuSimulation gpuSimulation;

        [Header("Wave Impulse")]

        [SerializeField, Min(0.001f)]
        private float waveImpulseRadius = 0.5f;

        [SerializeField, Min(0.001f)]
        private float waveImpulseStrength = 1f;

        /// <summary>
        /// このGameObjectに最も近いシミュレーション領域内の位置から、
        /// transform.forward方向へ進む波を一度だけ発生させる。
        /// </summary>
        public bool TryGenerateWave()
        {
            if (gpuSimulation == null)
            {
                throw new InvalidOperationException(
                    "GPU Simulation is not assigned."
                );
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
        
        private void OnValidate()
        {
            waveImpulseRadius = Mathf.Max(0.001f, waveImpulseRadius);
            waveImpulseStrength = Mathf.Max(0.001f, waveImpulseStrength);
        }
    }
}