using System;
using Core;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SphSweSimulation
    {
        /// <summary>
        /// 半陰的オイラー法で粒子の速度と位置を更新する。
        /// </summary>
        private void IntegrateParticles(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaTime),
                    deltaTime,
                    "時間刻みは0より大きい値である必要があります。"
                );
            }

            if (_particles == null || _particles.Length == 0)
            {
                return;
            }

            for (var particleIndex = 0; particleIndex < _particles.Length; particleIndex++)
            {
                ref var particle = ref _particles[particleIndex];

                if (particle.Type == SphSweParticleType.Boundary)
                {
                    continue;
                }

                particle.Velocity += particle.Acceleration * deltaTime;
                var maximumAllowedVelocity = Mathf.Sqrt(gravityAcceleration * particle.FluidDepth);

                LimitParticleVelocity(ref particle, maximumAllowedVelocity);

                particle.Position += particle.Velocity * deltaTime;
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
    }
}