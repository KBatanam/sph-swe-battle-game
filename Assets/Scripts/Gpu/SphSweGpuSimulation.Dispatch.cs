using System;

namespace Gpu
{
    public sealed partial class SphSweGpuSimulation
    {
        /// <summary>
        /// 現在の粒子位置からGPU空間グリッドを再構築し、
        /// 全粒子の密度相当量と流体深さを計算する。
        /// 各カーネルは前段階の結果を使用するため、記載順に実行する必要がある。
        /// セル開始位置とは、そのセルに所属する粒子インデックスを
        /// SortedParticleIndicesの何番目から格納するかを表す。
        /// </summary>
        private void ExecuteDensityCalculation()
        {
            // セルの粒子数をクリア
            DispatchOneDimension(clearCellParticleCountsKernelIndex, gpuBuffers.CellCount);
            // 各粒子をセルへ登録
            DispatchOneDimension(registerParticlesInCellsKernelIndex, gpuBuffers.ParticleCount);

            // 各Scanグループの粒子格納領域を0番目と仮定し、
            // 担当セルごとのローカルな粒子インデックス格納開始位置と粒子数合計を計算する。
            // 異なるグループは同じDispatch内で同期できないため、前のグループの粒子数はまだ反映しない。
            simulationComputeShader.Dispatch(
                scanCellParticleCountsByGroupKernelIndex,
                gpuBuffers.ScanGroupCount,
                1,
                1
            );

            // グループごとの粒子数合計をExclusive Scanし、
            // 各グループより前にある全グループの粒子数合計を、そのグループの開始位置として計算する。
            // 自分自身の粒子数は自分自身の開始位置には含めない。
            simulationComputeShader.Dispatch(
                scanCellParticleCountGroupSumsKernelIndex,
                1,
                1,
                1
            );

            // 各セルのローカルな開始位置へ所属グループの開始位置を加算し、
            // SortedParticleIndices全体における粒子インデックス格納開始位置へ変換する。
            DispatchOneDimension(addGroupStartIndicesKernelIndex, gpuBuffers.CellCount);
            // セルごとの書き込み位置を初期化
            DispatchOneDimension(initializeCellParticleWriteIndicesKernelIndex, gpuBuffers.CellCount);
            // 粒子インデックスをセル単位に整列
            DispatchOneDimension(sortParticleIndicesByCellKernelIndex, gpuBuffers.ParticleCount);
            // 近傍粒子から密度と流体深さを計算
            DispatchOneDimension(calculateDensitiesKernelIndex, gpuBuffers.ParticleCount);
        }

        /// <summary>
        /// 指定された要素数を処理できるスレッドグループ数を計算し、
        /// 一次元のCompute Shaderカーネルを実行する。
        /// </summary>
        private void DispatchOneDimension(int kernelIndex, int elementCount)
        {
            if (elementCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elementCount),
                    elementCount,
                    "Element count must be greater than zero."
                );
            }

            simulationComputeShader.GetKernelThreadGroupSizes(
                kernelIndex,
                out var threadCountX,
                out _,
                out _
            );

            var threadGroupCountX = (elementCount + (int)threadCountX - 1) / (int)threadCountX;

            simulationComputeShader.Dispatch(
                kernelIndex,
                threadGroupCountX,
                1,
                1
            );
        }
    }
}
