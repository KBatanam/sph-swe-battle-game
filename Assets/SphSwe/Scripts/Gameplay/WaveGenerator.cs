using System;
using SphSwe.Gpu;
using UnityEngine;
using SphSwe.Validation;

namespace SphSwe.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WaveGenerator : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweGpuSimulation gpuSimulation;
        
        [SerializeField, Required]
        private StunStatus stunStatus;

        [Header("Wave Impulse")]

        [SerializeField, Min(0.001f)]
        private float waveImpulseRadius = 0.5f;

        [SerializeField, Min(0.001f)]
        private float waveImpulseStrength = 1f;

        /// <summary>
        /// 波の発生に成功したときに、GPUシミュレーション座標系の
        /// 中心、方向、半径、強度を通知する。
        /// ネットワーク対戦では、このイベントをサーバーへ転送して
        /// 権威側の流体へ同じインパルスを適用するために使用する。
        /// </summary>
        public event Action<Vector2, Vector2, float, float> WaveGenerated;

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

            if (waveGenerationRequested)
            {
                WaveGenerated?.Invoke(
                    waveImpulseCenterSimulationPosition,
                    simulationDirection,
                    waveImpulseRadius,
                    waveImpulseStrength
                );
            }

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
