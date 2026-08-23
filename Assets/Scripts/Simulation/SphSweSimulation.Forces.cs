using Core;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SphSweSimulation
    {
        /// <summary>
        /// 流体深さの勾配から全流体粒子の加速度を計算する。
        /// </summary>
        [ContextMenu("Calculate Fluid Depth Gradient Accelerations")]
        private void CalculateFluidDepthGradientAccelerations()
        {
            if (_particles == null || _particles.Length == 0)
            {
                Debug.LogWarning("Particles have not been generated.", this);
                return;
            }

            var fluidDepthGradientAccelerationScale =
                -gravityAcceleration / referenceDensity;

            for (var particleIndex = 0;
                 particleIndex < _particles.Length;
                 particleIndex++)
            {
                ref var particle = ref _particles[particleIndex];

                if (particle.Type == SphSweParticleType.Boundary)
                {
                    particle.Acceleration = Vector2.zero;
                    continue;
                }

                var fluidDepthGradientAcceleration = Vector2.zero;

                for (var neighborIndex = 0;
                     neighborIndex < _particles.Length;
                     neighborIndex++)
                {
                    if (particleIndex == neighborIndex)
                    {
                        continue;
                    }

                    var neighbor = _particles[neighborIndex];

                    var positionDifference = particle.Position - neighbor.Position;

                    var spikyGradient =
                        SphSweKernel.EvaluateSpikyGradient(
                            positionDifference,
                            particle.EffectiveRadius
                        );

                    var combinedSpikyGradient = spikyGradient * 2f;

                    fluidDepthGradientAcceleration +=
                        fluidDepthGradientAccelerationScale
                        * neighbor.Mass
                        * combinedSpikyGradient;
                }

                particle.Acceleration =
                    fluidDepthGradientAcceleration;
            }
        }
    }
}
