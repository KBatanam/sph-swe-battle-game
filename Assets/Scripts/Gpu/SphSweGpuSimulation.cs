using System;
using Core;
using Simulation;
using UnityEngine;
using Validation;

namespace Gpu
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

        private Vector2 gridMinimumPosition;
        private int gridCellCountX;
        private int gridCellCountZ;

        private void Start()
        {
            ValidateReferences();
            InitializeGpuResources();
            InitializeGpuResources();
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
            gpuBuffers = new SphSweGpuBuffers(particles.Length, cellCount);

            var gpuParticles = new SphSweGpuParticle[particles.Length];

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                gpuParticles[particleIndex] = new SphSweGpuParticle(particles[particleIndex]);
            }
            
            gpuBuffers.ParticleBuffer.SetData(gpuParticles);
            BindBuffersToKernels();
            SetSimulationParameters();
            ExecuteDensityCalculation();
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
