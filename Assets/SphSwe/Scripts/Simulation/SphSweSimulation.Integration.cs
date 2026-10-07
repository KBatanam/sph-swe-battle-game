using System;
using SphSwe.Core;
using UnityEngine;

namespace SphSwe.Simulation
{
    public sealed partial class SphSweSimulation
    {
        /// <summary>
        /// 半陰的オイラー法で粒子の速度と位置を更新する。
        /// </summary>
        private void IntegrateParticles(float deltaTime)
        {
            using var profilingScope = IntegrationProfilerMarker.Auto();

            if (deltaTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaTime),
                    deltaTime,
                    "時間刻みは0より大きい値である必要があります。"
                );
            }

            if (particles == null || particles.Length == 0)
            {
                return;
            }

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                ref var particle = ref particles[particleIndex];

                if (particle.Type == SphSweParticleType.Boundary)
                {
                    continue;
                }

                particle.Velocity += particle.Acceleration * deltaTime;
                var maximumAllowedVelocity = Mathf.Sqrt(gravityAcceleration * particle.FluidDepth);

                LimitParticleVelocity(ref particle, maximumAllowedVelocity);

                particle.Position += particle.Velocity * deltaTime;
                ClampParticlePositionToSimulationArea(ref particle);
            }
        }

        /// <summary>
        /// 粒子速度の大きさを浅水波の伝播速度以下に制限する。
        /// </summary>
        /// <remarks>
        /// SPH-SWEでは、流体深さをh、重力加速度をgとしたとき、
        /// 浅水波の代表的な伝播速度は√(gh)で表される。
        /// 粒子速度がこの値を大きく超えると、1ステップ当たりの移動距離が増加し、
        /// 近傍関係や密度、加速度が急激に変化して数値計算が不安定になりやすい。
        /// そのため、参照実装と同様に√(gh)を最大許容速度として速度を制限する。
        /// </remarks>
        private static void LimitParticleVelocity(ref SphSweParticle particle, float maximumAllowedVelocity)
        {
            var squaredVelocity = 
                particle.Velocity.x * particle.Velocity.x + particle.Velocity.y * particle.Velocity.y;

            var squaredMaximumAllowedVelocity = maximumAllowedVelocity * maximumAllowedVelocity;

            if (squaredVelocity <= squaredMaximumAllowedVelocity || squaredVelocity <= 0f)
            {
                return;
            }

            var velocityScale = maximumAllowedVelocity / Mathf.Sqrt(squaredVelocity);

            particle.Velocity *= velocityScale;
        }
        
        /// <summary>
        /// 粒子が数値誤差や強い加速度によって計算領域外へ流出しないように、
        /// シミュレーション平面上の位置を計算領域内へ制限する。
        /// この処理は壁との物理的な衝突を再現するものではなく、
        /// 境界粒子による相互作用を実装するまでの安全制限として使用する。
        /// </summary>
        private void ClampParticlePositionToSimulationArea(ref SphSweParticle particle)
        {
            var halfSimulationAreaSize = simulationAreaSize * 0.5f;
            var minimumPosition = simulationCenter - halfSimulationAreaSize;
            var maximumPosition = simulationCenter + halfSimulationAreaSize;

            particle.Position = new Vector2(
                Mathf.Clamp(
                    particle.Position.x,
                    minimumPosition.x,
                    maximumPosition.x
                ),
                Mathf.Clamp(
                    particle.Position.y,
                    minimumPosition.y,
                    maximumPosition.y
                )
            );
        }
    }
}
