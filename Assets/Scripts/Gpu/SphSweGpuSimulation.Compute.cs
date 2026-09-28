using UnityEngine;

namespace Gpu
{
    public sealed partial class SphSweGpuSimulation
    {
        private static readonly int ParticlesPropertyId = Shader.PropertyToID("_Particles");
        private static readonly int ParticleCellIndicesPropertyId = Shader.PropertyToID("_ParticleCellIndices");
        private static readonly int CellParticleCountsPropertyId = Shader.PropertyToID("_CellParticleCounts");
        private static readonly int CellParticleStartIndicesPropertyId = Shader.PropertyToID("_CellParticleStartIndices");
        private static readonly int CellParticleCountSumsByGroupPropertyId = Shader.PropertyToID("_CellParticleCountSumsByGroup");
        private static readonly int CellParticleCountStartIndicesByGroupPropertyId = Shader.PropertyToID("_CellParticleCountStartIndicesByGroup");
        private static readonly int CellParticleWriteIndicesPropertyId = Shader.PropertyToID("_CellParticleWriteIndices");
        private static readonly int SortedParticleIndicesPropertyId = Shader.PropertyToID("_SortedParticleIndices");

        private static readonly int ParticleCountPropertyId = Shader.PropertyToID("_ParticleCount");
        private static readonly int CellCountPropertyId = Shader.PropertyToID("_CellCount");
        private static readonly int ScanGroupCountPropertyId = Shader.PropertyToID("_ScanGroupCount");
        private static readonly int GridMinimumPositionPropertyId = Shader.PropertyToID("_GridMinimumPosition");
        private static readonly int GridCellSizePropertyId = Shader.PropertyToID("_GridCellSize");
        private static readonly int GridCellCountXPropertyId = Shader.PropertyToID("_GridCellCountX");
        private static readonly int GridCellCountZPropertyId = Shader.PropertyToID("_GridCellCountZ");
        private static readonly int EffectiveRadiusPropertyId = Shader.PropertyToID("_EffectiveRadius");
        private static readonly int ReferenceDensityPropertyId = Shader.PropertyToID("_ReferenceDensity");
        
        private static readonly int GravityAccelerationPropertyId = Shader.PropertyToID("_GravityAcceleration");
        private static readonly int ViscosityCoefficientPropertyId = Shader.PropertyToID("_ViscosityCoefficient");
        
        private static readonly int SimulationMinimumPositionPropertyId = Shader.PropertyToID("_SimulationMinimumPosition");
        private static readonly int SimulationMaximumPositionPropertyId = Shader.PropertyToID("_SimulationMaximumPosition");
        
        private static readonly int MinimumTimeStepBitsPropertyId = Shader.PropertyToID("_MinimumTimeStepBits");
        private static readonly int CourantNumberPropertyId = Shader.PropertyToID("_CourantNumber");
        private static readonly int MaximumSimulationTimeStepPropertyId = Shader.PropertyToID("_MaximumSimulationTimeStep");
        
        private static readonly int SimulationTimeStatePropertyId = Shader.PropertyToID("_SimulationTimeState");
        private static readonly int FrameDeltaTimePropertyId = Shader.PropertyToID("_FrameDeltaTime");
        private static readonly int MaximumAccumulatedSimulationTimePropertyId = Shader.PropertyToID("_MaximumAccumulatedSimulationTime");
        private static readonly int MaximumSimulationSubstepCountPropertyId = Shader.PropertyToID("_MaximumSimulationSubstepCount");
        
        private int clearCellParticleCountsKernelIndex;
        private int registerParticlesInCellsKernelIndex;
        private int scanCellParticleCountsByGroupKernelIndex;
        private int scanCellParticleCountGroupSumsKernelIndex;
        private int addGroupStartIndicesKernelIndex;
        private int initializeCellParticleWriteIndicesKernelIndex;
        private int sortParticleIndicesByCellKernelIndex;
        private int calculateDensitiesKernelIndex;
        private int calculateAccelerationsKernelIndex;
        private int integrateParticlesKernelIndex;
        private int clearMinimumTimeStepKernelIndex;
        private int calculateMinimumTimeStepKernelIndex;
        private int beginSimulationFrameKernelIndex;
        private int beginSimulationSubstepKernelIndex;
        private int finalizeSimulationTimeStepKernelIndex;
        private int completeSimulationSubstepKernelIndex;

        private void InitializeKernelIndices()
        {
            beginSimulationFrameKernelIndex = simulationComputeShader.FindKernel("BeginSimulationFrame");
            beginSimulationSubstepKernelIndex = simulationComputeShader.FindKernel("BeginSimulationSubstep");
            clearCellParticleCountsKernelIndex = simulationComputeShader.FindKernel("ClearCellParticleCounts");
            registerParticlesInCellsKernelIndex = simulationComputeShader.FindKernel("RegisterParticlesInCells");
            scanCellParticleCountsByGroupKernelIndex = simulationComputeShader.FindKernel("ScanCellParticleCountsByGroup");
            scanCellParticleCountGroupSumsKernelIndex = simulationComputeShader.FindKernel("ScanCellParticleCountGroupSums");
            addGroupStartIndicesKernelIndex = simulationComputeShader.FindKernel("AddGroupStartIndicesToCellParticleStartIndices");
            initializeCellParticleWriteIndicesKernelIndex = simulationComputeShader.FindKernel("InitializeCellParticleWriteIndices");
            sortParticleIndicesByCellKernelIndex = simulationComputeShader.FindKernel("SortParticleIndicesByCell");
            calculateDensitiesKernelIndex = simulationComputeShader.FindKernel("CalculateDensities");
            calculateAccelerationsKernelIndex = simulationComputeShader.FindKernel("CalculateAccelerations");
            integrateParticlesKernelIndex = simulationComputeShader.FindKernel("IntegrateParticles");
            clearMinimumTimeStepKernelIndex = simulationComputeShader.FindKernel("ClearMinimumTimeStep");
            calculateMinimumTimeStepKernelIndex = simulationComputeShader.FindKernel("CalculateMinimumTimeStep");
            finalizeSimulationTimeStepKernelIndex = simulationComputeShader.FindKernel("FinalizeSimulationTimeStep");
            completeSimulationSubstepKernelIndex = simulationComputeShader.FindKernel("CompleteSimulationSubstep");
        }

        /// <summary>
        /// 各GPUバッファを、そのバッファを使用するCompute Shaderカーネルへ接続する。
        /// SetBufferによる接続はCompute Shader全体ではなくカーネルごとに管理されるため、
        /// 同じバッファを使用するすべてのカーネルに対して個別に設定する。
        /// バッファは実行中に再生成しないため、この処理は初期化時に一度だけ行う。
        /// </summary>
        private void BindBuffersToKernels()
        {
            BindSimulationTimeStateBuffer(beginSimulationFrameKernelIndex);
            BindSimulationTimeStateBuffer(beginSimulationSubstepKernelIndex);
            BindSimulationTimeStateBuffer(finalizeSimulationTimeStepKernelIndex);
            BindSimulationTimeStateBuffer(completeSimulationSubstepKernelIndex);
            BindSimulationTimeStateBuffer(clearCellParticleCountsKernelIndex);
            BindSimulationTimeStateBuffer(registerParticlesInCellsKernelIndex);
            BindSimulationTimeStateBuffer(scanCellParticleCountsByGroupKernelIndex);
            BindSimulationTimeStateBuffer(scanCellParticleCountGroupSumsKernelIndex);
            BindSimulationTimeStateBuffer(addGroupStartIndicesKernelIndex);
            BindSimulationTimeStateBuffer(initializeCellParticleWriteIndicesKernelIndex);
            BindSimulationTimeStateBuffer(sortParticleIndicesByCellKernelIndex);
            BindSimulationTimeStateBuffer(calculateDensitiesKernelIndex);
            BindSimulationTimeStateBuffer(calculateAccelerationsKernelIndex);
            BindSimulationTimeStateBuffer(clearMinimumTimeStepKernelIndex);
            BindSimulationTimeStateBuffer(calculateMinimumTimeStepKernelIndex);
            BindSimulationTimeStateBuffer(integrateParticlesKernelIndex);
            
            simulationComputeShader.SetBuffer(
                clearCellParticleCountsKernelIndex,
                CellParticleCountsPropertyId,
                gpuBuffers.CellParticleCountBuffer
            );
            simulationComputeShader.SetBuffer(
                registerParticlesInCellsKernelIndex,
                ParticlesPropertyId,
                gpuBuffers.ParticleBuffer
            );
            simulationComputeShader.SetBuffer(
                registerParticlesInCellsKernelIndex,
                ParticleCellIndicesPropertyId,
                gpuBuffers.ParticleCellIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                registerParticlesInCellsKernelIndex,
                CellParticleCountsPropertyId,
                gpuBuffers.CellParticleCountBuffer
            );
            simulationComputeShader.SetBuffer(
                scanCellParticleCountsByGroupKernelIndex,
                CellParticleCountsPropertyId,
                gpuBuffers.CellParticleCountBuffer
            );
            simulationComputeShader.SetBuffer(
                scanCellParticleCountsByGroupKernelIndex,
                CellParticleStartIndicesPropertyId,
                gpuBuffers.CellParticleStartIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                scanCellParticleCountsByGroupKernelIndex,
                CellParticleCountSumsByGroupPropertyId,
                gpuBuffers.CellParticleCountSumByGroupBuffer
            );
            simulationComputeShader.SetBuffer(
                scanCellParticleCountGroupSumsKernelIndex,
                CellParticleCountSumsByGroupPropertyId,
                gpuBuffers.CellParticleCountSumByGroupBuffer
            );
            simulationComputeShader.SetBuffer(
                scanCellParticleCountGroupSumsKernelIndex,
                CellParticleCountStartIndicesByGroupPropertyId,
                gpuBuffers.CellParticleCountStartIndexByGroupBuffer
            );
            simulationComputeShader.SetBuffer(
                addGroupStartIndicesKernelIndex,
                CellParticleStartIndicesPropertyId,
                gpuBuffers.CellParticleStartIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                addGroupStartIndicesKernelIndex,
                CellParticleCountStartIndicesByGroupPropertyId,
                gpuBuffers.CellParticleCountStartIndexByGroupBuffer
            );
            simulationComputeShader.SetBuffer(
                initializeCellParticleWriteIndicesKernelIndex,
                CellParticleStartIndicesPropertyId,
                gpuBuffers.CellParticleStartIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                initializeCellParticleWriteIndicesKernelIndex,
                CellParticleWriteIndicesPropertyId,
                gpuBuffers.CellParticleWriteIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                sortParticleIndicesByCellKernelIndex,
                ParticleCellIndicesPropertyId,
                gpuBuffers.ParticleCellIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                sortParticleIndicesByCellKernelIndex,
                CellParticleWriteIndicesPropertyId,
                gpuBuffers.CellParticleWriteIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                sortParticleIndicesByCellKernelIndex,
                SortedParticleIndicesPropertyId,
                gpuBuffers.SortedParticleIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                calculateDensitiesKernelIndex,
                ParticlesPropertyId,
                gpuBuffers.ParticleBuffer
            );
            simulationComputeShader.SetBuffer(
                calculateDensitiesKernelIndex,
                ParticleCellIndicesPropertyId,
                gpuBuffers.ParticleCellIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                calculateDensitiesKernelIndex,
                CellParticleCountsPropertyId,
                gpuBuffers.CellParticleCountBuffer
            );
            simulationComputeShader.SetBuffer(
                calculateDensitiesKernelIndex,
                CellParticleStartIndicesPropertyId,
                gpuBuffers.CellParticleStartIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                calculateDensitiesKernelIndex,
                SortedParticleIndicesPropertyId,
                gpuBuffers.SortedParticleIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                calculateAccelerationsKernelIndex,
                ParticlesPropertyId,
                gpuBuffers.ParticleBuffer
            );
            simulationComputeShader.SetBuffer(
                calculateAccelerationsKernelIndex,
                ParticleCellIndicesPropertyId,
                gpuBuffers.ParticleCellIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                calculateAccelerationsKernelIndex,
                CellParticleCountsPropertyId,
                gpuBuffers.CellParticleCountBuffer
            );
            simulationComputeShader.SetBuffer(
                calculateAccelerationsKernelIndex,
                CellParticleStartIndicesPropertyId,
                gpuBuffers.CellParticleStartIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                calculateAccelerationsKernelIndex,
                SortedParticleIndicesPropertyId,
                gpuBuffers.SortedParticleIndexBuffer
            );
            simulationComputeShader.SetBuffer(
                integrateParticlesKernelIndex,
                ParticlesPropertyId,
                gpuBuffers.ParticleBuffer
            );
            simulationComputeShader.SetBuffer(
                calculateMinimumTimeStepKernelIndex,
                ParticlesPropertyId,
                gpuBuffers.ParticleBuffer
            );
            simulationComputeShader.SetBuffer(
                calculateMinimumTimeStepKernelIndex,
                MinimumTimeStepBitsPropertyId,
                gpuBuffers.MinimumTimeStepBitsBuffer
            );
            simulationComputeShader.SetBuffer(
                clearMinimumTimeStepKernelIndex,
                MinimumTimeStepBitsPropertyId,
                gpuBuffers.MinimumTimeStepBitsBuffer
            );
            simulationComputeShader.SetBuffer(
                finalizeSimulationTimeStepKernelIndex,
                MinimumTimeStepBitsPropertyId,
                gpuBuffers.MinimumTimeStepBitsBuffer
            );
        }
        
        /// <summary>
        /// シミュレーション中に共通して使用する値をCompute Shaderへ設定する。
        /// 現時点では実行中に変更しない値だけを初期化時に設定する。
        /// </summary>
        private void SetSimulationParameters()
        {
            simulationComputeShader.SetInt(MaximumSimulationSubstepCountPropertyId, maximumSimulationSubstepCount);
            simulationComputeShader.SetInt(ParticleCountPropertyId, gpuBuffers.ParticleCount);
            simulationComputeShader.SetInt(CellCountPropertyId, gpuBuffers.CellCount);
            simulationComputeShader.SetInt(ScanGroupCountPropertyId, gpuBuffers.ScanGroupCount);
            simulationComputeShader.SetVector(GridMinimumPositionPropertyId, gridMinimumPosition);
            simulationComputeShader.SetFloat(GridCellSizePropertyId, sourceSimulation.EffectiveRadius);
            simulationComputeShader.SetInt(GridCellCountXPropertyId, gridCellCountX);
            simulationComputeShader.SetInt(GridCellCountZPropertyId, gridCellCountZ);
            simulationComputeShader.SetFloat(EffectiveRadiusPropertyId, sourceSimulation.EffectiveRadius);
            simulationComputeShader.SetFloat(ReferenceDensityPropertyId, sourceSimulation.ReferenceDensity);
            simulationComputeShader.SetFloat(GravityAccelerationPropertyId, sourceSimulation.GravityAcceleration);
            simulationComputeShader.SetFloat(ViscosityCoefficientPropertyId, sourceSimulation.ViscosityCoefficient);
            
            var halfSimulationAreaSize = sourceSimulation.SimulationAreaSize * 0.5f;
            var simulationMinimumPosition = sourceSimulation.SimulationCenter - halfSimulationAreaSize;
            var simulationMaximumPosition = sourceSimulation.SimulationCenter + halfSimulationAreaSize;

            simulationComputeShader.SetVector(SimulationMinimumPositionPropertyId, simulationMinimumPosition);
            simulationComputeShader.SetVector(SimulationMaximumPositionPropertyId, simulationMaximumPosition);
            
            simulationComputeShader.SetFloat(CourantNumberPropertyId, sourceSimulation.CourantNumber);
            simulationComputeShader.SetFloat(MaximumSimulationTimeStepPropertyId, sourceSimulation.MaximumSimulationTimeStep);
        }
        
        private void BindSimulationTimeStateBuffer(int kernelIndex)
        {
            simulationComputeShader.SetBuffer(
                kernelIndex,
                SimulationTimeStatePropertyId,
                gpuBuffers.SimulationTimeStateBuffer
            );
        }
    }
}