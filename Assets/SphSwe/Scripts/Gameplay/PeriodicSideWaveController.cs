using System;
using SphSwe.Gpu;
using SphSwe.Validation;
using UnityEngine;

namespace SphSwe.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PeriodicSideWaveController : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweGpuSimulation gpuSimulation;

        [SerializeField, Required]
        private ObjectiveBall objectiveBall;

        [Header("Timing")]

        [SerializeField, Min(0.01f)]
        private float waveGenerationInterval = 2f;

        [Header("Wave Impulse")]

        [SerializeField, Min(1)]
        private int affectedParticleLayerCount = 1;

        [SerializeField, Min(0.001f)]
        private float waveImpulseStrength = 1f;

        private float elapsedTime;
        private bool isNextCenterWaveGeneratedFromLeftSide = true;

        private void Awake()
        {
            ValidateReferences();
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;

            if (elapsedTime < waveGenerationInterval)
            {
                return;
            }

            if (TryGenerateSideWave())
            {
                elapsedTime -= waveGenerationInterval;
            }
        }

        private bool TryGenerateSideWave()
        {
            var simulation = gpuSimulation.SourceSimulation;
            var localBallPosition = simulation.transform.InverseTransformPoint(objectiveBall.transform.position);

            var simulationDirection = DetermineWaveDirection(localBallPosition.x, simulation.SimulationCenter.x);
            var affectedWidth = simulation.ParticleSpacing * affectedParticleLayerCount;

            return gpuSimulation.TryApplySideWaveImpulse(
                simulationDirection,
                affectedWidth,
                waveImpulseStrength
            );
        }

        private Vector2 DetermineWaveDirection(float ballPositionX, float simulationCenterX)
        {
            if (ballPositionX > simulationCenterX)
            {
                return Vector2.left;
            }

            if (ballPositionX < simulationCenterX)
            {
                return Vector2.right;
            }

            var simulationDirection =
                isNextCenterWaveGeneratedFromLeftSide
                    ? Vector2.right
                    : Vector2.left;

            isNextCenterWaveGeneratedFromLeftSide = !isNextCenterWaveGeneratedFromLeftSide;

            return simulationDirection;
        }

        private void ValidateReferences()
        {
            if (gpuSimulation == null)
            {
                throw new InvalidOperationException("GPU Simulation is not assigned.");
            }

            if (objectiveBall == null)
            {
                throw new InvalidOperationException("Objective Ball is not assigned.");
            }
        }

        private void OnValidate()
        {
            waveGenerationInterval = Mathf.Max(0.01f, waveGenerationInterval);
            affectedParticleLayerCount = Mathf.Max(1, affectedParticleLayerCount);
            waveImpulseStrength = Mathf.Max(0.001f, waveImpulseStrength);
        }
    }
}