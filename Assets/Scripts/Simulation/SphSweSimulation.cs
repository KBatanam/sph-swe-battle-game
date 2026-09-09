using System.Collections.Generic;
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

        private float particleSpacing;

        [SerializeField]
        private Vector2 simulationCenter = Vector2.zero;

        [Header("Particle Properties")]

        [SerializeField, Min(0.0001f)]
        private float particleMass = 2f;

        [SerializeField, Min(1)]
        private int kernelParticleCount = 20;

        private float effectiveRadius;
        private float particleRadius;
        
        [SerializeField, Min(0.001f)]
        private float referenceDensity = 998.29f;

        [SerializeField, Min(0f)]
        private float gravityAcceleration = 9.81f;
        
        [SerializeField, Min(0f)]
        private float viscosityCoefficient = 30f;
        
        [Header("Boundary Particles")]

        [SerializeField]
        private bool boundaryParticleGenerationEnabled = true;

        [SerializeField, Min(1)]
        private int boundaryParticleLayerCount = 2;

        [SerializeField, Range(0.1f, 1f)]
        private float boundaryParticleSpacingScale = 0.95f;

        [SerializeField]
        private Color boundaryParticleColor = new(
            0.2f,
            0.2f,
            0.2f,
            1f
        );
        
        [Header("Simulation Area")]

        [SerializeField]
        private Vector2 simulationAreaSize = new(5f, 5f);

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
        
        private SphSweParticle[] particles;
        
        public SphSweParticle[] Particles => particles ?? System.Array.Empty<SphSweParticle>();
        public int ParticleCount => particles?.Length ?? 0;
        public float ParticleSpacing => particleSpacing;
        public float EffectiveRadius => effectiveRadius;
        public float ParticleRadius => particleRadius;

        private void Awake()
        {
            Initialize();
        }
        
        private void OnValidate()
        {
            particleCountX = Mathf.Max(1, particleCountX);
            particleCountZ = Mathf.Max(1, particleCountZ);
            particleMass = Mathf.Max(0.0001f, particleMass);
            kernelParticleCount = Mathf.Max(1, kernelParticleCount);
            gizmoRadius = Mathf.Max(0.001f, gizmoRadius);
            referenceDensity = Mathf.Max(0.001f, referenceDensity);
            CalculateParticleDimensionsFromReferenceParameters();
            gravityAcceleration = Mathf.Max(0f, gravityAcceleration);
            particleAccelerationGizmoScale = Mathf.Max(0f, particleAccelerationGizmoScale);
            viscosityCoefficient = Mathf.Max(0f, viscosityCoefficient);
            simulationAreaSize.x = Mathf.Max(0.001f, simulationAreaSize.x);
            simulationAreaSize.y = Mathf.Max(0.001f, simulationAreaSize.y);
            courantNumber = Mathf.Clamp(courantNumber, 0.01f, 1f);
            maximumSimulationTimeStep = Mathf.Max(0.000001f, maximumSimulationTimeStep);
            maximumSimulationSubstepCount = Mathf.Max(1, maximumSimulationSubstepCount);
            maximumAccumulatedSimulationTime = Mathf.Max(maximumSimulationTimeStep, maximumAccumulatedSimulationTime);
            boundaryParticleLayerCount = Mathf.Max(1, boundaryParticleLayerCount);
            boundaryParticleSpacingScale = Mathf.Clamp(boundaryParticleSpacingScale, 0.1f, 1f);
        }

        private void Initialize()
        {
            CalculateParticleDimensionsFromReferenceParameters();
            GenerateParticles();
            CalculateDensitiesUsingSpatialGrid();
            CalculateAccelerations();
            LogFluidParticleDensityStatistics();
        }

        /// <summary>
        /// 粒子質量、目標近傍粒子数および参照密度から、
        /// 全粒子で共通して使用する固定有効半径、粒子半径および粒子間隔を計算する。
        /// 計算結果はシミュレーション中に変更しない。
        /// </summary>
        private void CalculateParticleDimensionsFromReferenceParameters()
        {
            effectiveRadius = Mathf.Sqrt(
                particleMass * kernelParticleCount
                / (Mathf.PI * referenceDensity)
            );

            particleRadius =
                0.5f * effectiveRadius
                * Mathf.Sqrt(Mathf.PI / kernelParticleCount);

            particleSpacing = particleRadius * 2f;
        }
        
        /// <summary>
        /// Inspectorの設定値を使用して流体粒子を格子状に生成する。
        /// </summary>
        [ContextMenu("Generate Particles")]
        public void GenerateParticles()
        {
            var fluidParticleCount = particleCountX * particleCountZ;
            var generatedParticles = new List<SphSweParticle>(fluidParticleCount);

            for (var x = 0; x < particleCountX; x++)
            {
                for (var z = 0; z < particleCountZ; z++)
                {
                    generatedParticles.Add(
                        new SphSweParticle(
                            CalculateInitialPosition(x, z),
                            particleMass,
                            effectiveRadius,
                            SphSweParticleType.Fluid
                        )
                    );
                }
            }

            AddBoundaryParticles(generatedParticles);
            particles = generatedParticles.ToArray();

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
            if (particles == null || particles.Length == 0)
            {
                Debug.LogWarning("Particles have not been generated.", this);
                return;
            }

            for (var i = 0; i < particles.Length; i++)
            {
                ref var particle = ref particles[i];
                var density = 0f;

                foreach (var neighbor in particles)
                {
                    var differenceX = particle.Position.x - neighbor.Position.x;
                    var differenceZ = particle.Position.y - neighbor.Position.y;
                    var squaredDistance = differenceX * differenceX + differenceZ * differenceZ;

                    var kernelValue = SphSweKernel.EvaluatePoly6(
                        squaredDistance,
                        particle.EffectiveRadius
                    );

                    density += neighbor.Mass * kernelValue;
                }

                particle.Density = density;
                particle.FluidDepth = Mathf.Max(0f, density / referenceDensity);
            }
        }
        
        /// <summary>
        /// 流体粒子の密度について、最小値、最大値、平均値を表示する。
        /// 境界条件を表現するための境界粒子は統計対象に含めない。
        /// </summary>
        [ContextMenu("Log Fluid Particle Density Statistics")]
        private void LogFluidParticleDensityStatistics()
        {
            if (particles == null || particles.Length == 0)
            {
                return;
            }

            var minimumDensity = float.PositiveInfinity;
            var maximumDensity = float.NegativeInfinity;
            var totalDensity = 0f;
            var fluidParticleCount = 0;

            foreach (var particle in particles)
            {
                if (particle.Type != SphSweParticleType.Fluid)
                {
                    continue;
                }

                minimumDensity = Mathf.Min(minimumDensity, particle.Density);
                maximumDensity = Mathf.Max(maximumDensity, particle.Density);

                totalDensity += particle.Density;
                fluidParticleCount++;
            }

            if (fluidParticleCount == 0)
            {
                Debug.LogWarning("Fluid particles were not found.", this);
                return;
            }

            var averageDensity = totalDensity / fluidParticleCount;

            var message = ZString.Format(
                "Fluid density — Min: {0:F5}, Max: {1:F5}, Average: {2:F5}",
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
