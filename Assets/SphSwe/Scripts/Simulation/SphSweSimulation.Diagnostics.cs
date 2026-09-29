using SphSwe.Core;
using UnityEngine;

namespace SphSwe.Simulation
{
    public sealed partial class SphSweSimulation
    {
        /// <summary>
        /// 現在の粒子状態とシミュレーション設定から、
        /// CFL時間刻みおよびサブステップ数の診断情報を計算する。
        /// シミュレーションの状態は変更しない。
        /// </summary>
        public SphSweTimeStepDiagnostics CalculateTimeStepDiagnostics()
        {
            var fluidParticleCount = 0;
            var limitingParticleIndex = -1;
            var cflTimeStep = float.PositiveInfinity;
            var limitingParticleVelocity = 0f;
            var limitingParticleFluidDepth = 0f;
            var limitingParticleWaveSpeed = 0f;
            var limitingParticleSignalSpeed = 0f;

            if (particles != null)
            {
                for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
                {
                    ref var particle = ref particles[particleIndex];

                    if (particle.Type == SphSweParticleType.Boundary)
                    {
                        continue;
                    }

                    fluidParticleCount++;

                    var squaredVelocity =
                        particle.Velocity.x * particle.Velocity.x
                        + particle.Velocity.y * particle.Velocity.y;

                    var velocityMagnitude = Mathf.Sqrt(squaredVelocity);
                    var waveSpeed = Mathf.Sqrt(gravityAcceleration * particle.FluidDepth);
                    var signalSpeed = velocityMagnitude + waveSpeed;

                    if (signalSpeed <= 0f)
                    {
                        continue;
                    }

                    var particleCflTimeStep =
                        courantNumber * particle.EffectiveRadius / signalSpeed;

                    if (particleCflTimeStep >= cflTimeStep)
                    {
                        continue;
                    }

                    cflTimeStep = particleCflTimeStep;
                    limitingParticleIndex = particleIndex;
                    limitingParticleVelocity = velocityMagnitude;
                    limitingParticleFluidDepth = particle.FluidDepth;
                    limitingParticleWaveSpeed = waveSpeed;
                    limitingParticleSignalSpeed = signalSpeed;
                }
            }

            var selectedTimeStep = Mathf.Min(maximumSimulationTimeStep, cflTimeStep);
            var estimatedRequiredSubstepCount = selectedTimeStep > 0f
                ? Mathf.CeilToInt(lastRequestedSimulationTime / selectedTimeStep)
                : 0;

            return new SphSweTimeStepDiagnostics(
                fluidParticleCount,
                limitingParticleIndex,
                lastCompletedSimulationSubstepCount,
                estimatedRequiredSubstepCount,
                maximumSimulationSubstepCount,
                lastRequestedSimulationTime,
                maximumSimulationTimeStep,
                cflTimeStep,
                selectedTimeStep,
                limitingParticleVelocity,
                limitingParticleFluidDepth,
                limitingParticleWaveSpeed,
                limitingParticleSignalSpeed,
                lastSimulatedTime,
                accumulatedSimulationTime
            );
        }
    }
}
