using Core;
using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// SPH-SWE粒子群の生成とシミュレーションを管理する。
    /// 現段階では初期配置とGizmos表示のみを行う。
    /// </summary>
    public sealed class SphSweSimulation : MonoBehaviour
    {
        [Header("Particle Layout")]

        [SerializeField, Min(1)]
        private int particleCountX = 16;

        [SerializeField, Min(1)]
        private int particleCountZ = 16;

        [SerializeField, Min(0.001f)]
        private float particleSpacing = 0.25f;

        [SerializeField]
        private Vector2 simulationCenter = Vector2.zero;

        [Header("Particle Properties")]

        [SerializeField, Min(0.0001f)]
        private float particleMass = 1f;

        [SerializeField, Min(0.001f)]
        private float effectiveRadius = 0.5f;

        [Header("Debug Drawing")]

        [SerializeField]
        private bool drawParticles = true;

        [SerializeField, Min(0.001f)]
        private float gizmoRadius = 0.08f;

        [SerializeField]
        private float gizmoHeight;

        [SerializeField]
        private Color fluidParticleColor = new Color(
            0.1f,
            0.5f,
            1f,
            1f
        );

        private SphSweParticle[] particles;
        
        public SphSweParticle[] Particles => particles;

        /// <summary>
        /// 現在生成されている粒子数。
        /// </summary>
        public int ParticleCount => particles?.Length ?? 0;

        private void Awake()
        {
            Initialize();
        }
        
        private void OnValidate()
        {
            particleCountX = Mathf.Max(1, particleCountX);
            particleCountZ = Mathf.Max(1, particleCountZ);
            particleSpacing = Mathf.Max(0.001f, particleSpacing);
            particleMass = Mathf.Max(0.0001f, particleMass);
            effectiveRadius = Mathf.Max(0.001f, effectiveRadius);
            gizmoRadius = Mathf.Max(0.001f, gizmoRadius);
        }

        private void Initialize()
        {
            GenerateParticles();
        }

        /// <summary>
        /// Inspectorの設定値を使用して流体粒子を格子状に生成する。
        /// </summary>
        [ContextMenu("Generate Particles")]
        public void GenerateParticles()
        {
            var totalParticleCount = particleCountX * particleCountZ;
            particles = new SphSweParticle[totalParticleCount];

            var particleIndex = 0;

            for (var x = 0; x < particleCountX; x++)
            {
                for (var z = 0; z < particleCountZ; z++)
                {
                    var position = CalculateInitialPosition(x, z);

                    particles[particleIndex] = new SphSweParticle(
                        position,
                        particleMass,
                        effectiveRadius,
                        SphSweParticleType.Fluid
                    );

                    particleIndex++;
                }
            }

            Debug.Log(
                $"Generated {ParticleCount} SPH-SWE particles.",
                this
            );
        }

        /// <summary>
        /// 格子番号から中央揃えされた初期位置を計算する。
        /// </summary>
        private Vector2 CalculateInitialPosition(int x, int z)
        {
            var width = (particleCountX - 1) * particleSpacing;
            var depth = (particleCountZ - 1) * particleSpacing;

            var positionX =
                simulationCenter.x
                - width * 0.5f
                + x * particleSpacing;

            var positionZ =
                simulationCenter.y
                - depth * 0.5f
                + z * particleSpacing;

            return new Vector2(positionX, positionZ);
        }

        private void OnDrawGizmos()
        {
            if (!drawParticles)
            {
                return;
            }

            Gizmos.color = fluidParticleColor;

            if (particles is { Length: > 0 })
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
            foreach (var particle in particles)
            {
                var worldPosition = TransformSimulationToWorldPosition(particle.Position);

                Gizmos.DrawSphere(worldPosition, gizmoRadius);
            }
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
    }
}