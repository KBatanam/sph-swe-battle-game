using System;
using SphSwe.Core;
using SphSwe.Simulation;
using UnityEngine;
using SphSwe.Validation;

namespace SphSwe.Gpu
{
    [DisallowMultipleComponent]
    public sealed partial class SphSweGpuSimulation : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private ComputeShader simulationComputeShader;

        [SerializeField, Required]
        private SphSweSimulation sourceSimulation;

        private SphSweGpuBuffers gpuBuffers;
        private bool simulationTimeStateValidationRequested;
        private bool continuousSimulationParticleValidationRequested;

        private Vector2 gridMinimumPosition;
        private int gridCellCountX;
        private int gridCellCountZ;
        
        [Header("Surface Mesh")]

        [SerializeField, Min(2)]
        private int surfaceVertexCountX = 128;

        [SerializeField, Min(2)]
        private int surfaceVertexCountZ = 128;
        
        [Header("Simulation Timing")]
        
        [SerializeField]
        private bool simulationExecutionEnabled = true;

        [SerializeField, Min(0.001f)]
        private float maximumAccumulatedSimulationTime = 0.1f;
        
        [SerializeField, Min(1)]
        private int maximumSimulationSubstepCount = 20;

        public SphSweSimulation SourceSimulation => sourceSimulation;
        public int SurfaceVertexCountX => surfaceVertexCountX;
        public int SurfaceVertexCountZ => surfaceVertexCountZ;
        
        /// <summary>
        /// 描画処理で使用するGPU粒子バッファと粒子数を取得する。
        /// 返されたバッファの所有権はSphSweGpuSimulationが保持するため、
        /// 呼び出し側でDisposeしてはならない。
        /// </summary>
        public bool TryGetParticleBuffer(out GraphicsBuffer particleBuffer, out int particleCount)
        {
            if (gpuBuffers == null || gpuBuffers.ParticleBuffer == null || !gpuBuffers.ParticleBuffer.IsValid())
            {
                particleBuffer = null;
                particleCount = 0;
                return false;
            }

            particleBuffer = gpuBuffers.ParticleBuffer;
            particleCount = gpuBuffers.ParticleCount;
            return true;
        }
        
        /// <summary>
        /// 描画処理で使用するGPU水面頂点バッファと、
        /// X、Z方向の頂点数を取得する。
        /// 返されたバッファの所有権はSphSweGpuSimulationが保持するため、
        /// 呼び出し側でDisposeしてはならない。
        /// </summary>
        public bool TryGetSurfaceVertexBuffer(
            out GraphicsBuffer surfaceVertexBuffer,
            out int vertexCountX,
            out int vertexCountZ)
        {
            if (gpuBuffers == null || gpuBuffers.SurfaceVertexBuffer == null || !gpuBuffers.SurfaceVertexBuffer.IsValid())
            {
                surfaceVertexBuffer = null;
                vertexCountX = 0;
                vertexCountZ = 0;
                return false;
            }

            surfaceVertexBuffer = gpuBuffers.SurfaceVertexBuffer;
            vertexCountX = surfaceVertexCountX;
            vertexCountZ = surfaceVertexCountZ;
            return true;
        }
        
        private void Start()
        {
            ValidateReferences();
            InitializeKernelIndices();
            InitializeGpuResources();
        }
        
        private void Update()
        {
            if (!simulationExecutionEnabled || gpuBuffers == null)
            {
                return;
            }

            ExecuteSimulationFrame(Time.deltaTime);
            RequestSimulationTimeStateValidation();
            RequestContinuousSimulationParticleValidation();
        }

        private void OnDestroy()
        {
            ReleaseGpuResources();
        }

        private void ValidateReferences()
        {
            if (simulationComputeShader == null)
            {
                throw new InvalidOperationException("Simulation Compute Shader is not assigned.");
            }

            if (sourceSimulation == null)
            {
                throw new InvalidOperationException("Source Simulation is not assigned.");
            }
        }

        private void InitializeGpuResources()
        {
            var particles = sourceSimulation.Particles;
            
            if (particles == null || particles.Length == 0)
            {
                throw new InvalidOperationException("Source Simulation has no particles.");
            }
            
            CalculateGridBounds(particles, sourceSimulation.EffectiveRadius);

            var cellCount = checked(gridCellCountX * gridCellCountZ);
            var surfaceVertexCount = checked(surfaceVertexCountX * surfaceVertexCountZ);

            gpuBuffers = new SphSweGpuBuffers(
                particles.Length,
                cellCount,
                surfaceVertexCount
            );

            var gpuParticles = new SphSweGpuParticle[particles.Length];

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                gpuParticles[particleIndex] = new SphSweGpuParticle(particles[particleIndex]);
            }
            
            gpuBuffers.ParticleBuffer.SetData(gpuParticles);
            var initialSimulationTimeState = new[] { new SphSweGpuSimulationTimeState() };
            gpuBuffers.SimulationTimeStateBuffer.SetData(initialSimulationTimeState);
            
            BindBuffersToKernels();
            SetSimulationParameters();
        }

        private void CalculateGridBounds(SphSweParticle[] particles, float cellSize)
        {
            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cellSize),
                    cellSize,
                    "Cell size must be greater than zero."
                );
            }
            
            var firstPosition = particles[0].Position;
            var minimumCellCoordinateX = Mathf.FloorToInt(firstPosition.x / cellSize);
            var minimumCellCoordinateZ = Mathf.FloorToInt(firstPosition.y / cellSize);
            var maximumCellCoordinateX = minimumCellCoordinateX;
            var maximumCellCoordinateZ = minimumCellCoordinateZ;

            for (var particleIndex = 1; particleIndex < particles.Length; particleIndex++)
            {
                var position = particles[particleIndex].Position;
                var cellCoordinateX = Mathf.FloorToInt(position.x / cellSize);
                var cellCoordinateZ = Mathf.FloorToInt(position.y / cellSize);

                minimumCellCoordinateX = Mathf.Min(minimumCellCoordinateX, cellCoordinateX);
                minimumCellCoordinateZ = Mathf.Min(minimumCellCoordinateZ, cellCoordinateZ);
                maximumCellCoordinateX = Mathf.Max(maximumCellCoordinateX, cellCoordinateX);
                maximumCellCoordinateZ = Mathf.Max(maximumCellCoordinateZ, cellCoordinateZ);
            }
            
            gridMinimumPosition = new Vector2(minimumCellCoordinateX * cellSize, minimumCellCoordinateZ * cellSize);
            
            gridCellCountX = maximumCellCoordinateX - minimumCellCoordinateX + 1;
            gridCellCountZ = maximumCellCoordinateZ - minimumCellCoordinateZ + 1;
        }

        private void ReleaseGpuResources()
        {
            gpuBuffers?.Dispose();
            gpuBuffers = null;
        }
    }
}
