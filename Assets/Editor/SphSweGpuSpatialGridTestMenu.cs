using System;
using Gpu;
using Simulation;
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
        private static readonly int CellParticleStartIndicesPropertyId =
            Shader.PropertyToID("_CellParticleStartIndices");
        private static readonly int CellParticleCountSumsByGroupPropertyId =
            Shader.PropertyToID("_CellParticleCountSumsByGroup");
        private static readonly int CellParticleCountStartIndicesByGroupPropertyId =
            Shader.PropertyToID("_CellParticleCountStartIndicesByGroup");
        private static readonly int CellParticleWriteIndicesPropertyId =
            Shader.PropertyToID("_CellParticleWriteIndices");
        private static readonly int SortedParticleIndicesPropertyId =
            Shader.PropertyToID("_SortedParticleIndices");
        private static readonly int ParticleCountPropertyId = Shader.PropertyToID("_ParticleCount");
        private static readonly int CellCountPropertyId = Shader.PropertyToID("_CellCount");
        private static readonly int ScanGroupCountPropertyId = Shader.PropertyToID("_ScanGroupCount");
        private static readonly int GridMinimumPositionPropertyId = Shader.PropertyToID("_GridMinimumPosition");
        private static readonly int GridCellSizePropertyId = Shader.PropertyToID("_GridCellSize");
        private static readonly int GridCellCountXPropertyId = Shader.PropertyToID("_GridCellCountX");
        private static readonly int GridCellCountZPropertyId = Shader.PropertyToID("_GridCellCountZ");
        private static readonly int EffectiveRadiusPropertyId = Shader.PropertyToID("_EffectiveRadius");
        private static readonly int ReferenceDensityPropertyId = Shader.PropertyToID("_ReferenceDensity");

        [MenuItem("Tools/SPH-SWE/Tests/Test GPU Complete Spatial Grid Build")]
        private static void TestGpuCompleteSpatialGridBuild()
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
            const int scanGroupCount = 1;
            const float effectiveRadius = 1f;
            const float referenceDensity = 2f;

            var particles = CreateTestParticles();
            var expectedDensities = CalculateExpectedDensities(particles, 4, effectiveRadius);
            var particleCellIndices = new uint[particles.Length];
            var cellParticleCounts = new uint[cellCount];
            var cellParticleStartIndices = new uint[cellCount];
            var cellParticleCountSumsByGroup = new uint[scanGroupCount];
            var cellParticleCountStartIndicesByGroup = new uint[scanGroupCount];
            var cellParticleWriteIndices = new uint[cellCount];
            var sortedParticleIndices = new uint[particles.Length];

            Array.Fill(sortedParticleIndices, uint.MaxValue);

            using var particleBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                particles.Length,
                SphSweGpuParticle.Stride
            );
            using var particleCellIndexBuffer = CreateUIntBuffer(particleCellIndices);
            using var cellParticleCountBuffer = CreateUIntBuffer(cellParticleCounts);
            using var cellParticleStartIndexBuffer = CreateUIntBuffer(cellParticleStartIndices);
            using var cellParticleCountSumByGroupBuffer =
                CreateUIntBuffer(cellParticleCountSumsByGroup);
            using var cellParticleCountStartIndexByGroupBuffer =
                CreateUIntBuffer(cellParticleCountStartIndicesByGroup);
            using var cellParticleWriteIndexBuffer =
                CreateUIntBuffer(cellParticleWriteIndices);
            using var sortedParticleIndexBuffer = CreateUIntBuffer(sortedParticleIndices);

            particleBuffer.SetData(particles);

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

            var partialScanKernelIndex = computeShader.FindKernel(
                "ScanCellParticleCountsByGroup"
            );
            computeShader.SetBuffer(
                partialScanKernelIndex,
                CellParticleCountsPropertyId,
                cellParticleCountBuffer
            );
            computeShader.SetBuffer(
                partialScanKernelIndex,
                CellParticleStartIndicesPropertyId,
                cellParticleStartIndexBuffer
            );
            computeShader.SetBuffer(
                partialScanKernelIndex,
                CellParticleCountSumsByGroupPropertyId,
                cellParticleCountSumByGroupBuffer
            );
            computeShader.SetInt(CellCountPropertyId, cellCount);
            computeShader.Dispatch(partialScanKernelIndex, scanGroupCount, 1, 1);

            var groupSumScanKernelIndex = computeShader.FindKernel(
                "ScanCellParticleCountGroupSums"
            );
            computeShader.SetBuffer(
                groupSumScanKernelIndex,
                CellParticleCountSumsByGroupPropertyId,
                cellParticleCountSumByGroupBuffer
            );
            computeShader.SetBuffer(
                groupSumScanKernelIndex,
                CellParticleCountStartIndicesByGroupPropertyId,
                cellParticleCountStartIndexByGroupBuffer
            );
            computeShader.SetInt(ScanGroupCountPropertyId, scanGroupCount);
            computeShader.Dispatch(groupSumScanKernelIndex, 1, 1, 1);

            var addGroupStartIndicesKernelIndex = computeShader.FindKernel(
                "AddGroupStartIndicesToCellParticleStartIndices"
            );
            computeShader.SetBuffer(
                addGroupStartIndicesKernelIndex,
                CellParticleStartIndicesPropertyId,
                cellParticleStartIndexBuffer
            );
            computeShader.SetBuffer(
                addGroupStartIndicesKernelIndex,
                CellParticleCountStartIndicesByGroupPropertyId,
                cellParticleCountStartIndexByGroupBuffer
            );
            computeShader.SetInt(CellCountPropertyId, cellCount);
            Dispatch(computeShader, addGroupStartIndicesKernelIndex, cellCount);

            var initializeWriteIndicesKernelIndex = computeShader.FindKernel(
                "InitializeCellParticleWriteIndices"
            );
            computeShader.SetBuffer(
                initializeWriteIndicesKernelIndex,
                CellParticleStartIndicesPropertyId,
                cellParticleStartIndexBuffer
            );
            computeShader.SetBuffer(
                initializeWriteIndicesKernelIndex,
                CellParticleWriteIndicesPropertyId,
                cellParticleWriteIndexBuffer
            );
            computeShader.SetInt(CellCountPropertyId, cellCount);
            Dispatch(computeShader, initializeWriteIndicesKernelIndex, cellCount);

            var sortKernelIndex = computeShader.FindKernel("SortParticleIndicesByCell");
            computeShader.SetBuffer(
                sortKernelIndex,
                ParticleCellIndicesPropertyId,
                particleCellIndexBuffer
            );
            computeShader.SetBuffer(
                sortKernelIndex,
                CellParticleWriteIndicesPropertyId,
                cellParticleWriteIndexBuffer
            );
            computeShader.SetBuffer(
                sortKernelIndex,
                SortedParticleIndicesPropertyId,
                sortedParticleIndexBuffer
            );
            computeShader.SetInt(ParticleCountPropertyId, particles.Length);
            Dispatch(computeShader, sortKernelIndex, particles.Length);

            var densityKernelIndex = computeShader.FindKernel("CalculateDensities");
            computeShader.SetBuffer(densityKernelIndex, ParticlesPropertyId, particleBuffer);
            computeShader.SetBuffer(densityKernelIndex, ParticleCellIndicesPropertyId, particleCellIndexBuffer);
            computeShader.SetBuffer(densityKernelIndex, CellParticleCountsPropertyId, cellParticleCountBuffer);
            computeShader.SetBuffer(densityKernelIndex, CellParticleStartIndicesPropertyId, cellParticleStartIndexBuffer);
            computeShader.SetBuffer(densityKernelIndex, SortedParticleIndicesPropertyId, sortedParticleIndexBuffer);
            computeShader.SetInt(ParticleCountPropertyId, particles.Length);
            computeShader.SetInt(GridCellCountXPropertyId, gridCellCountX);
            computeShader.SetInt(GridCellCountZPropertyId, gridCellCountZ);
            computeShader.SetFloat(EffectiveRadiusPropertyId, effectiveRadius);
            computeShader.SetFloat(ReferenceDensityPropertyId, referenceDensity);
            Dispatch(computeShader, densityKernelIndex, particles.Length);

            particleBuffer.GetData(particles);
            particleCellIndexBuffer.GetData(particleCellIndices);
            cellParticleCountBuffer.GetData(cellParticleCounts);
            cellParticleStartIndexBuffer.GetData(cellParticleStartIndices);
            cellParticleWriteIndexBuffer.GetData(cellParticleWriteIndices);
            sortedParticleIndexBuffer.GetData(sortedParticleIndices);

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
            VerifyValues(
                cellParticleStartIndices,
                new[] { 0u, 1u, 3u, 3u, 3u, 3u },
                "cell particle start indices"
            );
            VerifyValues(
                cellParticleWriteIndices,
                new[] { 1u, 3u, 3u, 3u, 3u, 4u },
                "cell particle write indices"
            );

            VerifyCellParticleIndices(
                sortedParticleIndices,
                cellParticleStartIndices,
                cellParticleCounts,
                0,
                new[] { 0u }
            );
            VerifyCellParticleIndices(
                sortedParticleIndices,
                cellParticleStartIndices,
                cellParticleCounts,
                1,
                new[] { 1u, 2u }
            );
            VerifyCellParticleIndices(
                sortedParticleIndices,
                cellParticleStartIndices,
                cellParticleCounts,
                5,
                new[] { 3u }
            );
            VerifyParticleDensities(particles, expectedDensities, referenceDensity);

            Debug.Log(
                "SPH-SWE GPU complete spatial grid build and density calculation test passed. "
                + $"Particle count: {particles.Length}, cell count: {cellCount}."
            );
        }

        [MenuItem("Tools/SPH-SWE/Tests/Test GPU Cell Particle Count Complete Scan")]
        private static void TestGpuCellParticleCountCompleteScan()
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

            const int scanElementCount = 256;
            const int cellCount = 600;
            var scanGroupCount =
                (cellCount + scanElementCount - 1) / scanElementCount;

            var cellParticleCounts = new uint[cellCount];
            var expectedCellParticleStartIndices = new uint[cellCount];
            var expectedCellParticleCountSumsByGroup = new uint[scanGroupCount];
            var expectedCellParticleCountStartIndicesByGroup = new uint[scanGroupCount];

            uint totalParticleCount = 0;

            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                var cellParticleCount = (uint)((cellIndex * 7 + 3) % 5);
                var scanGroupIndex = cellIndex / scanElementCount;

                cellParticleCounts[cellIndex] = cellParticleCount;
                expectedCellParticleStartIndices[cellIndex] = totalParticleCount;
                expectedCellParticleCountSumsByGroup[scanGroupIndex] +=
                    cellParticleCount;
                totalParticleCount += cellParticleCount;
            }

            uint precedingGroupParticleCount = 0;

            for (var scanGroupIndex = 0;
                 scanGroupIndex < scanGroupCount;
                 scanGroupIndex++)
            {
                expectedCellParticleCountStartIndicesByGroup[scanGroupIndex] =
                    precedingGroupParticleCount;
                precedingGroupParticleCount +=
                    expectedCellParticleCountSumsByGroup[scanGroupIndex];
            }

            var cellParticleStartIndices = new uint[cellCount];
            var cellParticleCountSumsByGroup = new uint[scanGroupCount];
            var cellParticleCountStartIndicesByGroup = new uint[scanGroupCount];

            using var cellParticleCountBuffer = CreateUIntBuffer(cellParticleCounts);
            using var cellParticleStartIndexBuffer = CreateUIntBuffer(cellParticleStartIndices);
            using var cellParticleCountSumByGroupBuffer =
                CreateUIntBuffer(cellParticleCountSumsByGroup);
            using var cellParticleCountStartIndexByGroupBuffer =
                CreateUIntBuffer(cellParticleCountStartIndicesByGroup);

            var partialScanKernelIndex = computeShader.FindKernel(
                "ScanCellParticleCountsByGroup"
            );
            computeShader.SetBuffer(
                partialScanKernelIndex,
                CellParticleCountsPropertyId,
                cellParticleCountBuffer
            );
            computeShader.SetBuffer(
                partialScanKernelIndex,
                CellParticleStartIndicesPropertyId,
                cellParticleStartIndexBuffer
            );
            computeShader.SetBuffer(
                partialScanKernelIndex,
                CellParticleCountSumsByGroupPropertyId,
                cellParticleCountSumByGroupBuffer
            );
            computeShader.SetInt(CellCountPropertyId, cellCount);
            computeShader.Dispatch(
                partialScanKernelIndex,
                scanGroupCount,
                1,
                1
            );

            var groupSumScanKernelIndex = computeShader.FindKernel(
                "ScanCellParticleCountGroupSums"
            );
            computeShader.SetBuffer(
                groupSumScanKernelIndex,
                CellParticleCountSumsByGroupPropertyId,
                cellParticleCountSumByGroupBuffer
            );
            computeShader.SetBuffer(
                groupSumScanKernelIndex,
                CellParticleCountStartIndicesByGroupPropertyId,
                cellParticleCountStartIndexByGroupBuffer
            );
            computeShader.SetInt(ScanGroupCountPropertyId, scanGroupCount);
            computeShader.Dispatch(groupSumScanKernelIndex, 1, 1, 1);

            var addGroupStartIndicesKernelIndex = computeShader.FindKernel(
                "AddGroupStartIndicesToCellParticleStartIndices"
            );
            computeShader.SetBuffer(
                addGroupStartIndicesKernelIndex,
                CellParticleStartIndicesPropertyId,
                cellParticleStartIndexBuffer
            );
            computeShader.SetBuffer(
                addGroupStartIndicesKernelIndex,
                CellParticleCountStartIndicesByGroupPropertyId,
                cellParticleCountStartIndexByGroupBuffer
            );
            computeShader.SetInt(CellCountPropertyId, cellCount);
            Dispatch(computeShader, addGroupStartIndicesKernelIndex, cellCount);

            cellParticleStartIndexBuffer.GetData(cellParticleStartIndices);
            cellParticleCountSumByGroupBuffer.GetData(cellParticleCountSumsByGroup);
            cellParticleCountStartIndexByGroupBuffer.GetData(
                cellParticleCountStartIndicesByGroup
            );

            VerifyValues(
                cellParticleStartIndices,
                expectedCellParticleStartIndices,
                "complete cell particle start indices"
            );
            VerifyValues(
                cellParticleCountSumsByGroup,
                expectedCellParticleCountSumsByGroup,
                "cell particle count sums by group"
            );
            VerifyValues(
                cellParticleCountStartIndicesByGroup,
                expectedCellParticleCountStartIndicesByGroup,
                "cell particle count start indices by group"
            );

            Debug.Log(
                "SPH-SWE GPU cell particle count complete Scan test passed. "
                + $"Cell count: {cellCount}, Scan group count: {scanGroupCount}, "
                + $"particle count: {totalParticleCount}."
            );
        }

        [MenuItem("Tools/SPH-SWE/Tests/Test GPU Cell Particle Count Group Sum Scan")]
        private static void TestGpuCellParticleCountGroupSumScan()
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

            var cellParticleCountSumsByGroup = new[] { 400u, 350u, 120u };
            var cellParticleCountStartIndicesByGroup =
                new uint[cellParticleCountSumsByGroup.Length];

            Array.Fill(cellParticleCountStartIndicesByGroup, 99u);

            using var cellParticleCountSumByGroupBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                cellParticleCountSumsByGroup.Length,
                sizeof(uint)
            );
            using var cellParticleCountStartIndexByGroupBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                cellParticleCountStartIndicesByGroup.Length,
                sizeof(uint)
            );

            cellParticleCountSumByGroupBuffer.SetData(cellParticleCountSumsByGroup);
            cellParticleCountStartIndexByGroupBuffer.SetData(
                cellParticleCountStartIndicesByGroup
            );

            var kernelIndex = computeShader.FindKernel(
                "ScanCellParticleCountGroupSums"
            );
            computeShader.SetBuffer(
                kernelIndex,
                CellParticleCountSumsByGroupPropertyId,
                cellParticleCountSumByGroupBuffer
            );
            computeShader.SetBuffer(
                kernelIndex,
                CellParticleCountStartIndicesByGroupPropertyId,
                cellParticleCountStartIndexByGroupBuffer
            );
            computeShader.SetInt(
                ScanGroupCountPropertyId,
                cellParticleCountSumsByGroup.Length
            );

            computeShader.Dispatch(kernelIndex, 1, 1, 1);

            cellParticleCountStartIndexByGroupBuffer.GetData(
                cellParticleCountStartIndicesByGroup
            );

            VerifyValues(
                cellParticleCountStartIndicesByGroup,
                new[] { 0u, 400u, 750u },
                "cell particle count start indices by group"
            );

            Debug.Log(
                "SPH-SWE GPU cell particle count group sum Scan test passed. "
                + $"Scan group count: {cellParticleCountSumsByGroup.Length}."
            );
        }

        [MenuItem("Tools/SPH-SWE/Tests/Test GPU Cell Particle Count Partial Scan")]
        private static void TestGpuCellParticleCountPartialScan()
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

            var cellParticleCounts = new[] { 1u, 2u, 0u, 1u, 3u, 0u, 1u, 2u };
            var cellParticleStartIndices = new uint[cellParticleCounts.Length];
            var cellParticleCountSumsByGroup = new uint[1];

            Array.Fill(cellParticleStartIndices, 99u);
            Array.Fill(cellParticleCountSumsByGroup, 99u);

            using var cellParticleCountBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                cellParticleCounts.Length,
                sizeof(uint)
            );
            using var cellParticleStartIndexBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                cellParticleStartIndices.Length,
                sizeof(uint)
            );
            using var cellParticleCountSumByGroupBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                cellParticleCountSumsByGroup.Length,
                sizeof(uint)
            );

            cellParticleCountBuffer.SetData(cellParticleCounts);
            cellParticleStartIndexBuffer.SetData(cellParticleStartIndices);
            cellParticleCountSumByGroupBuffer.SetData(cellParticleCountSumsByGroup);

            var kernelIndex = computeShader.FindKernel("ScanCellParticleCountsByGroup");
            computeShader.SetBuffer(
                kernelIndex,
                CellParticleCountsPropertyId,
                cellParticleCountBuffer
            );
            computeShader.SetBuffer(
                kernelIndex,
                CellParticleStartIndicesPropertyId,
                cellParticleStartIndexBuffer
            );
            computeShader.SetBuffer(
                kernelIndex,
                CellParticleCountSumsByGroupPropertyId,
                cellParticleCountSumByGroupBuffer
            );
            computeShader.SetInt(CellCountPropertyId, cellParticleCounts.Length);

            Dispatch(computeShader, kernelIndex, cellParticleCounts.Length);

            cellParticleStartIndexBuffer.GetData(cellParticleStartIndices);
            cellParticleCountSumByGroupBuffer.GetData(cellParticleCountSumsByGroup);

            VerifyValues(
                cellParticleStartIndices,
                new[] { 0u, 1u, 3u, 3u, 4u, 7u, 7u, 8u },
                "cell particle start indices"
            );
            VerifyValues(
                cellParticleCountSumsByGroup,
                new[] { 10u },
                "cell particle count sums by group"
            );

            Debug.Log(
                "SPH-SWE GPU cell particle count partial Scan test passed. "
                + $"Cell count: {cellParticleCounts.Length}."
            );
        }

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
                new SphSweGpuParticle { Position = new Vector2(-0.75f, -0.75f), Mass = 1f },
                new SphSweGpuParticle { Position = new Vector2(0.25f, -0.75f), Mass = 1f },
                new SphSweGpuParticle { Position = new Vector2(0.75f, -0.25f), Mass = 1f },
                new SphSweGpuParticle { Position = new Vector2(1.25f, 0.25f), Mass = 1f },
                new SphSweGpuParticle { Position = new Vector2(2.1f, 0f), Mass = 1f }
            };
        }

        private static float[] CalculateExpectedDensities(SphSweGpuParticle[] particles, int validParticleCount, float effectiveRadius)
        {
            var expectedDensities = new float[particles.Length];

            for (var particleIndex = 0; particleIndex < validParticleCount; particleIndex++)
            {
                for (var neighborParticleIndex = 0; neighborParticleIndex < validParticleCount; neighborParticleIndex++)
                {
                    var positionDifference = particles[particleIndex].Position - particles[neighborParticleIndex].Position;
                    var squaredDistance = positionDifference.x * positionDifference.x + positionDifference.y * positionDifference.y;
                    expectedDensities[particleIndex] += particles[neighborParticleIndex].Mass
                                                        * SphSweKernel.EvaluatePoly6(squaredDistance, effectiveRadius);
                }
            }

            return expectedDensities;
        }

        private static void VerifyParticleDensities(SphSweGpuParticle[] particles, float[] expectedDensities, float referenceDensity)
        {
            const float tolerance = 0.0001f;

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                var expectedDensity = expectedDensities[particleIndex];
                var expectedFluidDepth = Mathf.Max(0f, expectedDensity / referenceDensity);

                if (Mathf.Abs(particles[particleIndex].Density - expectedDensity) > tolerance
                    || Mathf.Abs(particles[particleIndex].FluidDepth - expectedFluidDepth) > tolerance)
                {
                    throw new InvalidOperationException(
                        $"Particle {particleIndex} density/depth was "
                        + $"{particles[particleIndex].Density}/{particles[particleIndex].FluidDepth}, "
                        + $"but {expectedDensity}/{expectedFluidDepth} was expected."
                    );
                }
            }
        }

        private static GraphicsBuffer CreateUIntBuffer(uint[] values)
        {
            var buffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                values.Length,
                sizeof(uint)
            );
            buffer.SetData(values);
            return buffer;
        }

        private static void VerifyCellParticleIndices(
            uint[] sortedParticleIndices,
            uint[] cellParticleStartIndices,
            uint[] cellParticleCounts,
            int cellIndex,
            uint[] expectedParticleIndices)
        {
            var particleCount = (int)cellParticleCounts[cellIndex];

            if (particleCount != expectedParticleIndices.Length)
            {
                throw new InvalidOperationException(
                    $"Cell {cellIndex} contained {particleCount} particles, "
                    + $"but {expectedParticleIndices.Length} were expected."
                );
            }

            var actualParticleIndices = new uint[particleCount];
            Array.Copy(
                sortedParticleIndices,
                (int)cellParticleStartIndices[cellIndex],
                actualParticleIndices,
                0,
                particleCount
            );

            Array.Sort(actualParticleIndices);
            Array.Sort(expectedParticleIndices);

            VerifyValues(
                actualParticleIndices,
                expectedParticleIndices,
                $"sorted particle indices for cell {cellIndex}"
            );
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
