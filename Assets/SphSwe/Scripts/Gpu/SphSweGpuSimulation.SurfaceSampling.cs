using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SphSwe.Gpu
{
    public sealed partial class SphSweGpuSimulation
    {
        private bool surfaceSampleReadbackPending;

        /// <summary>
        /// 指定したシミュレーション座標の流体速度と流体深さをGPUで計算し、
        /// 非同期Readback完了後にコールバックへ渡す。
        /// 前回のReadbackが完了していない場合は新しい要求を開始しない。
        /// </summary>
        public bool TryRequestFluidSurfaceSample(Vector2 simulationPosition, Action<SphSweGpuSurfaceSample> onSampleReceived)
        {
            if (onSampleReceived == null)
            {
                throw new ArgumentNullException(nameof(onSampleReceived));
            }

            if (gpuBuffers == null ||
                gpuBuffers.SurfaceSampleBuffer == null ||
                !gpuBuffers.SurfaceSampleBuffer.IsValid() ||
                surfaceSampleReadbackPending)
            {
                return false;
            }

            simulationComputeShader.SetVector(SurfaceSampleSimulationPositionPropertyId, simulationPosition);

            simulationComputeShader.Dispatch(
                sampleFluidSurfaceKernelIndex,
                1,
                1,
                1
            );

            surfaceSampleReadbackPending = true;

            AsyncGPUReadback.Request(
                gpuBuffers.SurfaceSampleBuffer,
                request => CompleteFluidSurfaceSampleReadback(
                    request,
                    onSampleReceived
                )
            );

            return true;
        }

        private void CompleteFluidSurfaceSampleReadback(
            AsyncGPUReadbackRequest request,
            Action<SphSweGpuSurfaceSample> onSampleReceived)
        {
            surfaceSampleReadbackPending = false;

            if (request.hasError)
            {
                Debug.LogError(
                    "Failed to read GPU fluid surface sample.",
                    this
                );
                return;
            }

            var surfaceSamples = request.GetData<SphSweGpuSurfaceSample>();

            if (surfaceSamples.Length != 1)
            {
                Debug.LogError(
                    "GPU surface sample buffer did not contain exactly one element.",
                    this
                );
                return;
            }

            onSampleReceived(surfaceSamples[0]);
        }
    }
}
