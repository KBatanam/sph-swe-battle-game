using Cysharp.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gpu
{
    public sealed partial class SphSweGpuSimulation
    {
        /// <summary>
        /// GPU密度計算結果を非同期で読み戻し、
        /// GPU計算開始時点のCPU版結果と比較する。
        /// Editor専用の検証処理。
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void RequestDensityCalculationValidation()
        {
            var sourceParticles = sourceSimulation.Particles;
            var expectedDensities = new float[sourceParticles.Length];
            var expectedFluidDepths = new float[sourceParticles.Length];
            
            for (var particleIndex = 0; particleIndex < sourceParticles.Length; particleIndex++)
            {
                expectedDensities[particleIndex] = sourceParticles[particleIndex].Density;
                expectedFluidDepths[particleIndex] = sourceParticles[particleIndex].FluidDepth;
            }
            
            AsyncGPUReadback.Request(
                gpuBuffers.ParticleBuffer,
                request => ValidateDensityCalculation(
                    request,
                    expectedDensities,
                    expectedFluidDepths
                )
            );
        }

        private void ValidateDensityCalculation(
            AsyncGPUReadbackRequest request,
            float[] expectedDensities,
            float[] expectedFluidDepths)
        {
            if (request.hasError)
            {
                Debug.LogError(
                    "Failed to read GPU particle data.",
                    this
                );
                return;
            }
            
            var gpuParticles = request.GetData<SphSweGpuParticle>();
         
            if (gpuParticles.Length != expectedDensities.Length)
            {
                Debug.LogError(
                    ZString.Format(
                        "GPU particle count was {0}, but {1} was expected.",
                        gpuParticles.Length,
                        expectedDensities.Length
                    ),
                    this
                );
                return;
            }
            
            for (var particleIndex = 0; particleIndex < gpuParticles.Length; particleIndex++)
            {
                var densityMatches = AreApproximatelyEqual(
                    gpuParticles[particleIndex].Density,
                    expectedDensities[particleIndex]
                );
                var fluidDepthMatches = AreApproximatelyEqual(
                    gpuParticles[particleIndex].FluidDepth,
                    expectedFluidDepths[particleIndex]
                );

                if (densityMatches && fluidDepthMatches)
                {
                    continue;
                }

                Debug.LogError(
                    ZString.Format(
                        "GPU particle {0} density/depth was {1}/{2}, but {3}/{4} was expected.",
                        particleIndex,
                        gpuParticles[particleIndex].Density,
                        gpuParticles[particleIndex].FluidDepth,
                        expectedDensities[particleIndex],
                        expectedFluidDepths[particleIndex]
                    ),
                    this
                );
                return;
            }

            Debug.Log(
                ZString.Format(
                    "GPU runtime density validation passed. Particle count: {0}.",
                    gpuParticles.Length
                ),
                this
            );
        }

        /// <summary>
        /// CPUとGPUで浮動小数点演算の順序が異なることを考慮し、
        /// 小さい値には絶対誤差、大きい値には相対誤差を使用して比較する。
        /// </summary>
        private static bool AreApproximatelyEqual(float actualValue, float expectedValue)
        {
            const float absoluteTolerance = 0.0001f;
            const float relativeTolerance = 0.000001f;

            var difference = Mathf.Abs(actualValue - expectedValue);
            var allowedDifference = Mathf.Max(
                absoluteTolerance,
                Mathf.Abs(expectedValue) * relativeTolerance
            );

            return difference <= allowedDifference;
        }
    }
}
