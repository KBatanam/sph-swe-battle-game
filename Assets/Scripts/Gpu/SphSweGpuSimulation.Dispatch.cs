using System;
using UnityEngine;

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
        /// 構築済みの空間グリッドと計算済みの密度を使用して、
        /// 流体深さ勾配と粘性による全粒子の加速度を計算する。
        /// </summary>
        private void ExecuteAccelerationCalculation()
        {
            DispatchOneDimension(calculateAccelerationsKernelIndex, gpuBuffers.ParticleCount);
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
        
        /// <summary>
        /// 指定した時間刻みを使用し、半陰的オイラー法で
        /// 流体粒子の速度と位置を1ステップ更新する。
        /// </summary>
        private void ExecuteParticleIntegration()
        {
            DispatchOneDimension(integrateParticlesKernelIndex, gpuBuffers.ParticleCount);
        }
        
        /// <summary>
        /// 各流体粒子のCFL条件を評価し、
        /// 全粒子に対して安定な最小時間刻みをGPUバッファへ格納する。
        /// </summary>
        private void ExecuteMinimumTimeStepCalculation()
        {
            simulationComputeShader.Dispatch(
                clearMinimumTimeStepKernelIndex,
                1,
                1,
                1
            );

            DispatchOneDimension(calculateMinimumTimeStepKernelIndex, gpuBuffers.ParticleCount);
        }
        
        /// <summary>
        /// CFL条件による時間刻みと未処理時間を比較し、
        /// 今回のサブステップで実際に進める時間を確定する。
        /// </summary>
        private void ExecuteFinalizeSimulationTimeStep()
        {
            simulationComputeShader.Dispatch(
                finalizeSimulationTimeStepKernelIndex,
                1,
                1,
                1
            );
        }
        
        /// <summary>
        /// 現在のGPU粒子状態から密度、加速度、CFL時間刻みを順番に計算し、
        /// 求めた時間刻みを使用して速度と位置を1サブステップ進める。
        /// 各処理は前段階の結果に依存するため、実行順を変更してはならない。
        /// </summary>
        private void ExecuteSimulationSubstep()
        {   
            ExecuteDensityCalculation();
            ExecuteAccelerationCalculation();
            ExecuteMinimumTimeStepCalculation();
            ExecuteFinalizeSimulationTimeStep();
            ExecuteParticleIntegration();
            ExecuteCompleteSimulationSubstep();
        }
        
        /// <summary>
        /// 描画フレームの経過時間をGPU側へ蓄積し、
        /// このフレームのサブステップ管理情報を初期化する。
        /// </summary>
        private void ExecuteBeginSimulationFrame(float frameDeltaTime)
        {
            simulationComputeShader.SetFloat(
                FrameDeltaTimePropertyId,
                frameDeltaTime
            );

            simulationComputeShader.SetFloat(
                MaximumAccumulatedSimulationTimePropertyId,
                maximumAccumulatedSimulationTime
            );

            simulationComputeShader.Dispatch(
                beginSimulationFrameKernelIndex,
                1,
                1,
                1
            );
        }
        
        /// <summary>
        /// GPU側に残っている未処理時間と実行済み回数から、
        /// 次のサブステップを実行するか判定する。
        /// </summary>
        private void ExecuteBeginSimulationSubstep()
        {
            simulationComputeShader.Dispatch(
                beginSimulationSubstepKernelIndex,
                1,
                1,
                1
            );
        }
        
        /// <summary>
        /// 粒子積分で進めた時間を未処理時間から減算し、
        /// 完了したサブステップ数を増やす。
        /// </summary>
        private void ExecuteCompleteSimulationSubstep()
        {
            simulationComputeShader.Dispatch(
                completeSimulationSubstepKernelIndex,
                1,
                1,
                1
            );
        }
        
        /// <summary>
        /// 描画フレームの経過時間をGPUへ蓄積し、
        /// 最大サブステップ数を上限としてシミュレーションを進める。
        /// 実際に必要なサブステップ数はGPU側の時間状態で判定する。
        /// </summary>
        private void ExecuteSimulationFrame(float frameDeltaTime)
        {
            ExecuteBeginSimulationFrame(frameDeltaTime);

            for (var substepIndex = 0; substepIndex < maximumSimulationSubstepCount; substepIndex++)
            {
                ExecuteBeginSimulationSubstep();
                ExecuteSimulationSubstep();
            }
        }
        
        /// <summary>
        /// 指定したシミュレーション座標を中心として、
        /// 範囲内の流体粒子へ指定方向の速度Impulseを一度だけ加える。
        /// </summary>
        public bool TryApplyWaveImpulse(
            Vector2 centerSimulationPosition,
            Vector2 simulationDirection,
            float radius,
            float strength)
        {
            if (gpuBuffers == null || gpuBuffers.ParticleBuffer == null || !gpuBuffers.ParticleBuffer.IsValid())
            {
                return false;
            }

            if (simulationDirection.sqrMagnitude <= 0f)
            {
                throw new ArgumentException(
                    "Wave impulse direction must not be zero.",
                    nameof(simulationDirection)
                );
            }

            if (radius <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(radius),
                    radius,
                    "Wave impulse radius must be greater than zero."
                );
            }

            if (strength <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(strength),
                    strength,
                    "Wave impulse strength must be greater than zero."
                );
            }

            var normalizedSimulationDirection = simulationDirection.normalized;

            simulationComputeShader.SetVector(
                WaveImpulseCenterSimulationPositionPropertyId,
                centerSimulationPosition
            );

            simulationComputeShader.SetVector(
                WaveImpulseSimulationDirectionPropertyId,
                normalizedSimulationDirection
            );

            simulationComputeShader.SetFloat(
                WaveImpulseRadiusPropertyId,
                radius
            );

            simulationComputeShader.SetFloat(
                WaveImpulseStrengthPropertyId,
                strength
            );

            DispatchOneDimension(
                applyWaveImpulseKernelIndex,
                gpuBuffers.ParticleCount
            );

            return true;
        }
    }
}
