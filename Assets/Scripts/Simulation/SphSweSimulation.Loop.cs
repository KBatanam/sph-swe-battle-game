using Core;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SphSweSimulation
    {
        [Header("Simulation Loop")]

        [SerializeField]
        private bool simulationExecutionEnabled = true;

        [SerializeField, Range(0.01f, 1f)]
        private float courantNumber = 0.25f;

        [SerializeField, Min(0.000001f)]
        private float maximumSimulationTimeStep = 0.002f;

        [SerializeField, Min(1)]
        private int maximumSimulationSubstepCount = 20;

        [SerializeField, Min(0.000001f)]
        private float maximumAccumulatedSimulationTime = 0.04f;

        private float accumulatedSimulationTime;

        private void FixedUpdate()
        {
            if (!simulationExecutionEnabled)
            {
                return;
            }

            accumulatedSimulationTime = Mathf.Min(
                accumulatedSimulationTime + Time.fixedDeltaTime,
                maximumAccumulatedSimulationTime
            );

            var completedSubstepCount = 0;

            while (accumulatedSimulationTime > 0f && completedSubstepCount < maximumSimulationSubstepCount)
            {
                var elapsedSimulationTime = SimulateAdaptiveStep(accumulatedSimulationTime);

                accumulatedSimulationTime = Mathf.Max(
                    0f,
                    accumulatedSimulationTime - elapsedSimulationTime
                );

                completedSubstepCount++;
            }
        }

        /// <summary>
        /// 現在の粒子状態から密度と加速度を再計算し、
        /// CFL条件を満たす時間だけシミュレーションを進める。
        /// </summary>
        /// <returns>実際に進めたシミュレーション時間。</returns>
        private float SimulateAdaptiveStep(float availableSimulationTime)
        {
            CalculateDensities();
            CalculateAccelerations();

            var maximumStableTimeStep =
                CalculateMaximumStableTimeStep();

            var simulationDeltaTime = Mathf.Min(
                availableSimulationTime,
                maximumStableTimeStep
            );

            IntegrateParticles(simulationDeltaTime);

            return simulationDeltaTime;
        }

        /// <summary>
        /// 全流体粒子についてCFL条件を評価し、
        /// 数値的に安定して進められる最大時間刻みを計算する。
        /// </summary>
        private float CalculateMaximumStableTimeStep()
        {
            var maximumStableTimeStep = maximumSimulationTimeStep;

            if (particles == null || particles.Length == 0)
            {
                return maximumStableTimeStep;
            }

            foreach (var particle in particles)
            {
                if (particle.Type == SphSweParticleType.Boundary)
                {
                    continue;
                }

                var squaredVelocity = 
                    particle.Velocity.x * particle.Velocity.x + particle.Velocity.y * particle.Velocity.y;

                var velocityMagnitude = Mathf.Sqrt(squaredVelocity);

                var shallowWaterWaveSpeed = Mathf.Sqrt(gravityAcceleration * particle.FluidDepth);

                var maximumSignalSpeed = velocityMagnitude + shallowWaterWaveSpeed;

                if (maximumSignalSpeed <= 0f)
                {
                    continue;
                }

                var particleMaximumTimeStep = courantNumber * particle.EffectiveRadius / maximumSignalSpeed;

                maximumStableTimeStep = Mathf.Min(maximumStableTimeStep, particleMaximumTimeStep);
            }

            return maximumStableTimeStep;
        }
    }
}
