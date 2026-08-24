using Core;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SphSweSimulation
    {
        /// <summary>
        /// 全粒子に働く加速度を計算する。
        /// </summary>
        [ContextMenu("Calculate Accelerations")]
        private void CalculateAccelerations()
        {
            CalculateFluidDepthGradientAccelerations();
            AddViscosityAccelerations();
        }

        /// <summary>
        /// 流体深さの勾配から、各流体粒子の加速度を計算する。
        /// 計算対象は有効半径内の近傍粒子に限定する。
        /// </summary>
        private void CalculateFluidDepthGradientAccelerations()
        {
            if (particles == null || particles.Length == 0)
            {
                Debug.LogWarning("Particles have not been generated.", this);
                return;
            }

            var fluidDepthGradientAccelerationScale = -gravityAcceleration / referenceDensity;

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                ref var particle = ref particles[particleIndex];

                if (particle.Type == SphSweParticleType.Boundary)
                {
                    particle.Acceleration = Vector2.zero;
                    continue;
                }

                var fluidDepthGradientAcceleration = Vector2.zero;
                var neighborParticleIndices = neighborParticleIndicesByParticle[particleIndex];

                foreach (var neighborParticleIndex in neighborParticleIndices)
                {
                    if (particleIndex == neighborParticleIndex)
                    {
                        continue;
                    }

                    ref var neighbor = ref particles[neighborParticleIndex];

                    var positionDifference = particle.Position - neighbor.Position;

                    var spikyGradient = SphSweKernel.EvaluateSpikyGradient(
                        positionDifference,
                        particle.EffectiveRadius
                    );

                    var combinedSpikyGradient = spikyGradient * 2f;

                    fluidDepthGradientAcceleration +=
                        fluidDepthGradientAccelerationScale
                        * neighbor.Mass
                        * combinedSpikyGradient;
                }

                particle.Acceleration = fluidDepthGradientAcceleration;
            }
        }

        /// <summary>
        /// 有効半径内の近傍粒子との速度差から粘性加速度を計算し、
        /// 現在の加速度へ加算する。
        /// </summary>
        private void AddViscosityAccelerations()
        {
            if (particles == null || particles.Length == 0)
            {
                Debug.LogWarning("Particles have not been generated.", this);
                return;
            }

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                ref var particle = ref particles[particleIndex];

                if (particle.Type == SphSweParticleType.Boundary)
                {
                    continue;
                }

                if (particle.Density <= 0f)
                {
                    continue;
                }

                var viscosityAcceleration = Vector2.zero;
                var particleViscosityScale = viscosityCoefficient / particle.Density;
                var neighborParticleIndices = neighborParticleIndicesByParticle[particleIndex];

                foreach (var neighborParticleIndex in neighborParticleIndices)
                {
                    if (particleIndex == neighborParticleIndex)
                    {
                        continue;
                    }

                    ref var neighbor = ref particles[neighborParticleIndex];

                    if (neighbor.Density <= 0f)
                    {
                        continue;
                    }

                    var positionDifference = particle.Position - neighbor.Position;
                    var differenceX = positionDifference.x;
                    var differenceZ = positionDifference.y;
                    var squaredDistance = differenceX * differenceX + differenceZ * differenceZ;

                    var viscosityLaplacian =
                        SphSweKernel.EvaluateViscosityLaplacian(
                            squaredDistance,
                            particle.EffectiveRadius
                        );

                    var combinedViscosityLaplacian = viscosityLaplacian * 2f;
                    var velocityDifference = neighbor.Velocity - particle.Velocity;

                    viscosityAcceleration +=
                        particleViscosityScale * neighbor.Mass * velocityDifference
                        / neighbor.Density * combinedViscosityLaplacian;
                }

                particle.Acceleration += viscosityAcceleration;
            }
        }
    }
}
