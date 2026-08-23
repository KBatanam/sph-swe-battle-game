using Core;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SphSweSimulation
    {
        private void OnDrawGizmos()
        {
            if (!particleGizmoDrawingEnabled)
            {
                return;
            }

            Gizmos.color = fluidParticleColor;

            if (Particles != null && Particles.Length > 0)
            {
                DrawGeneratedParticles();
                return;
            }

            DrawParticlePreview();
        }

        /// <summary>
        /// 実際に生成されている粒子を描画する。
        /// </summary>
        private void DrawGeneratedParticles()
        {
            if (Particles == null || Particles.Length == 0)
            {
                return;
            }

            var particleDensityRangeExists = TryCalculateParticleDensityRange(
                out var minimumDensity,
                out var maximumDensity
            );

            foreach (var particle in Particles)
            {
                var worldPosition = TransformSimulationToWorldPosition(particle.Position);

                if (densityBasedParticleColoringEnabled
                    && particleDensityRangeExists)
                {
                    Gizmos.color = CalculateParticleGizmoColor(
                        particle.Density,
                        minimumDensity,
                        maximumDensity
                    );
                }
                else
                {
                    Gizmos.color = fluidParticleColor;
                }
                
                Gizmos.DrawSphere(worldPosition, gizmoRadius);

                DrawParticleAccelerationGizmo(particle, worldPosition);
            }
        }

        /// <summary>
        /// 現在生成されている粒子から密度の最小値と最大値を計算する。
        /// </summary>
        private bool TryCalculateParticleDensityRange(out float minimumDensity, out float maximumDensity)
        {
            minimumDensity = 0f;
            maximumDensity = 0f;

            if (Particles == null || Particles.Length == 0)
            {
                return false;
            }

            minimumDensity = float.PositiveInfinity;
            maximumDensity = float.NegativeInfinity;

            foreach (var particle in Particles)
            {
                minimumDensity = Mathf.Min(
                    minimumDensity,
                    particle.Density
                );

                maximumDensity = Mathf.Max(
                    maximumDensity,
                    particle.Density
                );
            }

            return true;
        }

        /// <summary>
        /// 粒子の密度をGizmos描画用の色へ変換する。
        /// </summary>
        private Color CalculateParticleGizmoColor(float particleDensity, float minimumDensity, float maximumDensity)
        {
            var normalizedDensity = Mathf.InverseLerp(
                minimumDensity,
                maximumDensity,
                particleDensity
            );

            return Color.Lerp(
                lowDensityParticleColor,
                highDensityParticleColor,
                normalizedDensity
            );
        }

        /// <summary>
        /// Editモードでは設定値から初期配置をプレビューする。
        /// </summary>
        private void DrawParticlePreview()
        {
            for (var x = 0; x < particleCountX; x++)
            {
                for (var z = 0; z < particleCountZ; z++)
                {
                    var position = CalculateInitialPosition(x, z);
                    var worldPosition = TransformSimulationToWorldPosition(position);

                    Gizmos.DrawSphere(worldPosition, gizmoRadius);
                }
            }
        }

        /// <summary>
        /// 2次元のシミュレーション座標をUnity座標へ変換する。
        /// </summary>
        private Vector3 TransformSimulationToWorldPosition(Vector2 position)
        {
            return transform.TransformPoint(
                new Vector3(
                    position.x,
                    gizmoHeight,
                    position.y
                )
            );
        }
        
        /// <summary>
        /// 粒子の加速度をGizmosの線として描画する。
        /// </summary>
        private void DrawParticleAccelerationGizmo(SphSweParticle particle, Vector3 particleWorldPosition)
        {
            if (!particleAccelerationGizmoDrawingEnabled)
            {
                return;
            }

            var accelerationX = particle.Acceleration.x;
            var accelerationZ = particle.Acceleration.y;

            var squaredAccelerationMagnitude = accelerationX * accelerationX + accelerationZ * accelerationZ;

            if (squaredAccelerationMagnitude <= 0f)
            {
                return;
            }

            var accelerationEndSimulationPosition =
                particle.Position + particle.Acceleration * particleAccelerationGizmoScale;

            var accelerationEndWorldPosition = 
                TransformSimulationToWorldPosition(accelerationEndSimulationPosition);

            Gizmos.color = particleAccelerationGizmoColor;

            Gizmos.DrawLine(particleWorldPosition, accelerationEndWorldPosition);
        }
    }
}
