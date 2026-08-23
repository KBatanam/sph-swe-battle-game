using Core;
using Cysharp.Text;
using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// SPH-SWE粒子群の生成とシミュレーションを管理する。
    /// 現段階では初期配置とGizmos表示のみを行う。
    /// </summary>
    public sealed partial class SphSweSimulation : MonoBehaviour
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
        
        [SerializeField, Min(0.001f)]
        private float referenceDensity = 998.29f;

        [SerializeField, Min(0f)]
        private float gravityAcceleration = 9.81f;
        
        [SerializeField, Min(0f)]
        private float viscosityCoefficient = 30f;

        [Header("Debug Drawing")]

        [UnityEngine.Serialization.FormerlySerializedAs("drawParticles")]
        [SerializeField]
        private bool particleGizmoDrawingEnabled = true;

        [SerializeField, Min(0.001f)]
        private float gizmoRadius = 0.08f;

        [SerializeField]
        private float gizmoHeight;

        [SerializeField]
        private Color fluidParticleColor = new (
            0.1f,
            0.5f,
            1f,
            1f
        );
        
        [SerializeField]
        private bool densityBasedParticleColoringEnabled = true;

        [SerializeField]
        private Color lowDensityParticleColor = new (
            0.1f,
            0.4f,
            1f,
            1f
        );

        [SerializeField]
        private Color highDensityParticleColor = new (
            1f,
            0.15f,
            0.05f,
            1f
        );
        
        [SerializeField]
        private bool particleAccelerationGizmoDrawingEnabled = true;

        [SerializeField, Min(0f)]
        private float particleAccelerationGizmoScale = 0.25f;

        [SerializeField]
        private Color particleAccelerationGizmoColor = new (
            1f,
            0.9f,
            0.1f,
            1f
        );
        
        private SphSweParticle[] _particles;
        
        public SphSweParticle[] Particles => _particles ?? System.Array.Empty<SphSweParticle>();
        public int ParticleCount => _particles?.Length ?? 0;

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
            referenceDensity = Mathf.Max(0.001f, referenceDensity);
            gravityAcceleration = Mathf.Max(0f, gravityAcceleration);
            particleAccelerationGizmoScale = Mathf.Max(0f, particleAccelerationGizmoScale);
            viscosityCoefficient = Mathf.Max(0f, viscosityCoefficient);
        }

        private void Initialize()
        {
            GenerateParticles();
            CalculateDensities();
            CalculateAccelerations();
        }

        /// <summary>
        /// Inspectorの設定値を使用して流体粒子を格子状に生成する。
        /// </summary>
        [ContextMenu("Generate Particles")]
        public void GenerateParticles()
        {
            var totalParticleCount = particleCountX * particleCountZ;
            _particles = new SphSweParticle[totalParticleCount];

            var particleIndex = 0;

            for (var x = 0; x < particleCountX; x++)
            {
                for (var z = 0; z < particleCountZ; z++)
                {
                    var position = CalculateInitialPosition(x, z);

                    _particles[particleIndex] = new SphSweParticle(
                        position,
                        particleMass,
                        effectiveRadius,
                        SphSweParticleType.Fluid
                    );

                    particleIndex++;
                }
            }

            var message = ZString.Format(
                "Generated {0} SPH-SWE particles.",
                ParticleCount
            );

            Debug.Log(message, this);
        }
        
        /// <summary>
        /// 全粒子探索とPoly6カーネルを使用して、
        /// 各粒子の密度相当量を計算する。
        /// </summary>
        [ContextMenu("Calculate Densities")]
        private void CalculateDensities()
        {
            if (_particles == null || _particles.Length == 0)
            {
                Debug.LogWarning("Particles have not been generated.", this);
                return;
            }

            for (var i = 0; i < _particles.Length; i++)
            {
                ref var particle = ref _particles[i];
                var density = 0f;

                foreach (var neighbor in _particles)
                {
                    var differenceX =
                        particle.Position.x - neighbor.Position.x;

                    var differenceZ =
                        particle.Position.y - neighbor.Position.y;

                    var squaredDistance =
                        differenceX * differenceX
                        + differenceZ * differenceZ;

                    var kernelValue = SphSweKernel.EvaluatePoly6(
                        squaredDistance,
                        particle.EffectiveRadius
                    );

                    density += neighbor.Mass * kernelValue;
                    particle.FluidDepth = Mathf.Max(0f, density / referenceDensity);
                }

                particle.Density = density;
            }
            
            LogDensityStatistics();
        }
        
        /// <summary>
        /// 現在の粒子密度について、最小値、最大値、平均値を表示する。
        /// </summary>
        private void LogDensityStatistics()
        {
            if (_particles == null || _particles.Length == 0)
            {
                return;
            }

            var minimumDensity = float.PositiveInfinity;
            var maximumDensity = float.NegativeInfinity;
            var totalDensity = 0f;

            foreach (var particle in _particles)
            {
                minimumDensity = Mathf.Min(
                    minimumDensity,
                    particle.Density
                );

                maximumDensity = Mathf.Max(
                    maximumDensity,
                    particle.Density
                );

                totalDensity += particle.Density;
            }

            var averageDensity =
                totalDensity / _particles.Length;

            var message = ZString.Format(
                "Density — Min: {0:F5}, Max: {1:F5}, Average: {2:F5}",
                minimumDensity,
                maximumDensity,
                averageDensity
            );

            Debug.Log(message, this);
        }

        /// <summary>
        /// 格子番号から中央揃えされた初期位置を計算する。
        /// </summary>
        private Vector2 CalculateInitialPosition(int x, int z)
        {
            var width = (particleCountX - 1) * particleSpacing;
            var depth = (particleCountZ - 1) * particleSpacing;

            var positionX = simulationCenter.x - width * 0.5f + x * particleSpacing;
            var positionZ = simulationCenter.y - depth * 0.5f + z * particleSpacing;

            return new Vector2(positionX, positionZ);
        }
    }
}
