using System;
using Gpu;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class SphSweGpuSpatialGridTestMenu
    {
        private const string SimulationComputeShaderPath =
            "Assets/Shaders/Compute/SphSweSimulation.compute";

        private static readonly int ParticlesPropertyId = Shader.PropertyToID("_Particles");
        private static readonly int ParticleCellIndicesPropertyId = Shader.PropertyToID("_ParticleCellIndices");
        private static readonly int CellParticleCountsPropertyId = Shader.PropertyToID("_CellParticleCounts");
        private static readonly int ParticleCountPropertyId = Shader.PropertyToID("_ParticleCount");
        private static readonly int CellCountPropertyId = Shader.PropertyToID("_CellCount");
        private static readonly int GridMinimumPositionPropertyId = Shader.PropertyToID("_GridMinimumPosition");
        private static readonly int GridCellSizePropertyId = Shader.PropertyToID("_GridCellSize");
        private static readonly int GridCellCountXPropertyId = Shader.PropertyToID("_GridCellCountX");
        private static readonly int GridCellCountZPropertyId = Shader.PropertyToID("_GridCellCountZ");

        [MenuItem("Tools/SPH-SWE/Tests/Test GPU Spatial Grid Registration")]
        private static void TestGpuSpatialGridRegistration()
        {
            var computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                SimulationComputeShaderPath
            );

            if (computeShader == null)
            {
                throw new InvalidOperationException(
                    $"Compute Shader was not found at {SimulationComputeShaderPath}."
                );
            }

            const int gridCellCountX = 3;
            const int gridCellCountZ = 2;
            const int cellCount = gridCellCountX * gridCellCountZ;

            var particles = CreateTestParticles();
            var particleCellIndices = new uint[particles.Length];
            var cellParticleCounts = new uint[cellCount];

            Array.Fill(cellParticleCounts, 99u);

            using var particleBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                particles.Length,
                SphSweGpuParticle.Stride
            );
            using var particleCellIndexBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                particleCellIndices.Length,
                sizeof(uint)
            );
            using var cellParticleCountBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                cellParticleCounts.Length,
                sizeof(uint)
            );

            particleBuffer.SetData(particles);
            particleCellIndexBuffer.SetData(particleCellIndices);
            cellParticleCountBuffer.SetData(cellParticleCounts);

            var clearKernelIndex = computeShader.FindKernel("ClearCellParticleCounts");
            computeShader.SetBuffer(
                clearKernelIndex,
                CellParticleCountsPropertyId,
                cellParticleCountBuffer
            );
            computeShader.SetInt(CellCountPropertyId, cellCount);
            Dispatch(computeShader, clearKernelIndex, cellCount);

            var registerKernelIndex = computeShader.FindKernel("RegisterParticlesInCells");
            computeShader.SetBuffer(registerKernelIndex, ParticlesPropertyId, particleBuffer);
            computeShader.SetBuffer(
                registerKernelIndex,
                ParticleCellIndicesPropertyId,
                particleCellIndexBuffer
            );
            computeShader.SetBuffer(
                registerKernelIndex,
                CellParticleCountsPropertyId,
                cellParticleCountBuffer
            );
            computeShader.SetInt(ParticleCountPropertyId, particles.Length);
            computeShader.SetVector(GridMinimumPositionPropertyId, new Vector2(-1f, -1f));
            computeShader.SetFloat(GridCellSizePropertyId, 1f);
            computeShader.SetInt(GridCellCountXPropertyId, gridCellCountX);
            computeShader.SetInt(GridCellCountZPropertyId, gridCellCountZ);
            Dispatch(computeShader, registerKernelIndex, particles.Length);

            particleCellIndexBuffer.GetData(particleCellIndices);
            cellParticleCountBuffer.GetData(cellParticleCounts);

            VerifyValues(
                particleCellIndices,
                new[] { 0u, 1u, 1u, 5u, uint.MaxValue },
                "particle cell indices"
            );
            VerifyValues(
                cellParticleCounts,
                new[] { 1u, 2u, 0u, 0u, 0u, 1u },
                "cell particle counts"
            );

            Debug.Log(
                "SPH-SWE GPU spatial grid registration test passed. "
                + $"Particle count: {particles.Length}, cell count: {cellCount}."
            );
        }

        private static SphSweGpuParticle[] CreateTestParticles()
        {
            return new[]
            {
                new SphSweGpuParticle { Position = new Vector2(-0.75f, -0.75f) },
                new SphSweGpuParticle { Position = new Vector2(0.25f, -0.75f) },
                new SphSweGpuParticle { Position = new Vector2(0.75f, -0.25f) },
                new SphSweGpuParticle { Position = new Vector2(1.25f, 0.25f) },
                new SphSweGpuParticle { Position = new Vector2(2.1f, 0f) }
            };
        }

        private static void Dispatch(
            ComputeShader computeShader,
            int kernelIndex,
            int itemCount)
        {
            computeShader.GetKernelThreadGroupSizes(
                kernelIndex,
                out var threadCountX,
                out _,
                out _
            );

            var threadGroupCountX =
                (itemCount + (int)threadCountX - 1)
                / (int)threadCountX;

            computeShader.Dispatch(kernelIndex, threadGroupCountX, 1, 1);
        }

        private static void VerifyValues(
            uint[] actualValues,
            uint[] expectedValues,
            string valueDescription)
        {
            if (actualValues.Length != expectedValues.Length)
            {
                throw new InvalidOperationException(
                    $"The {valueDescription} length was {actualValues.Length}, "
                    + $"but {expectedValues.Length} was expected."
                );
            }

            for (var index = 0; index < actualValues.Length; index++)
            {
                if (actualValues[index] == expectedValues[index])
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"The {valueDescription} value at index {index} was "
                    + $"{actualValues[index]}, but {expectedValues[index]} was expected."
                );
            }
        }
    }
}
