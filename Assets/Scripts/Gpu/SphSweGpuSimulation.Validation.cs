using Cysharp.Text;
using UnityEngine;
using UnityEngine.Rendering;
using Core;
using System;

namespace Gpu
{
    public sealed partial class SphSweGpuSimulation
    {
        /// <summary>
        /// GPU計算結果を非同期で読み戻し、
        /// GPU計算開始時点のCPU版結果と比較するEditor専用の検証処理。
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void RequestSimulationCalculationValidation()
        {
            var sourceParticles = sourceSimulation.Particles;
            var expectedDensities = new float[sourceParticles.Length];
            var expectedFluidDepths = new float[sourceParticles.Length];
            var expectedAccelerations = new Vector2[sourceParticles.Length];
            var expectedPositions = new Vector2[sourceParticles.Length];
            var expectedVelocities = new Vector2[sourceParticles.Length];
            var simulationDeltaTime = sourceSimulation.CalculateTimeStepDiagnostics().SelectedTimeStep;

            for (var particleIndex = 0; particleIndex < sourceParticles.Length; particleIndex++)
            {
                var sourceParticle = sourceParticles[particleIndex];

                expectedDensities[particleIndex] = sourceParticle.Density;
                expectedFluidDepths[particleIndex] = sourceParticle.FluidDepth;
                expectedAccelerations[particleIndex] = sourceParticle.Acceleration;

                CalculateExpectedIntegratedState(
                    sourceParticle,
                    simulationDeltaTime,
                    out expectedPositions[particleIndex],
                    out expectedVelocities[particleIndex]
                );
            }

            AsyncGPUReadback.Request(
                gpuBuffers.ParticleBuffer,
                request => ValidateSimulationCalculation(
                    request,
                    expectedDensities,
                    expectedFluidDepths,
                    expectedAccelerations,
                    expectedPositions,
                    expectedVelocities
                )
            );
        }

        private void ValidateSimulationCalculation(
            AsyncGPUReadbackRequest request,
            float[] expectedDensities,
            float[] expectedFluidDepths,
            Vector2[] expectedAccelerations,
            Vector2[] expectedPositions,
            Vector2[] expectedVelocities)
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
                var gpuParticle = gpuParticles[particleIndex];

                var densityMatches = AreApproximatelyEqual(
                    gpuParticle.Density,
                    expectedDensities[particleIndex]
                );
                var fluidDepthMatches = AreApproximatelyEqual(
                    gpuParticle.FluidDepth,
                    expectedFluidDepths[particleIndex]
                );
                var accelerationMatches = AreApproximatelyEqual(
                    gpuParticle.Acceleration,
                    expectedAccelerations[particleIndex]
                );
                var positionMatches = AreApproximatelyEqual(
                    gpuParticle.Position,
                    expectedPositions[particleIndex]
                );
                var velocityMatches = AreApproximatelyEqual(
                    gpuParticle.Velocity,
                    expectedVelocities[particleIndex]
                );

                if (densityMatches
                    && fluidDepthMatches
                    && accelerationMatches
                    && positionMatches
                    && velocityMatches)
                {
                    continue;
                }

                Debug.LogError(
                    ZString.Format(
                        "GPU particle {0} did not match the CPU simulation result.",
                        particleIndex
                    ),
                    this
                );
                return;
            }

            Debug.Log(
                ZString.Format(
                    "GPU runtime density, acceleration, and integration validation passed. Particle count: {0}.",
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
        
        private static bool AreApproximatelyEqual(
            Vector2 actualValue,
            Vector2 expectedValue)
        {
            return AreApproximatelyEqual(
                       actualValue.x,
                       expectedValue.x
                   )
                   && AreApproximatelyEqual(
                       actualValue.y,
                       expectedValue.y
                   );
        }
        
        /// <summary>
        /// CPU基準実装と同じ半陰的オイラー法、速度制限、領域制限を使用して、
        /// GPU積分結果と比較する位置と速度の期待値を計算する。
        /// </summary>
        private void CalculateExpectedIntegratedState(
            SphSweParticle particle,
            float simulationDeltaTime,
            out Vector2 expectedPosition,
            out Vector2 expectedVelocity)
        {
            expectedPosition = particle.Position;
            expectedVelocity = particle.Velocity;

            if (particle.Type == SphSweParticleType.Boundary)
            {
                return;
            }

            expectedVelocity +=
                particle.Acceleration * simulationDeltaTime;

            var maximumAllowedVelocity = Mathf.Sqrt(
                Mathf.Max(
                    0f,
                    sourceSimulation.GravityAcceleration
                    * particle.FluidDepth
                )
            );
            var squaredVelocity =
                expectedVelocity.x * expectedVelocity.x
                + expectedVelocity.y * expectedVelocity.y;
            var squaredMaximumAllowedVelocity =
                maximumAllowedVelocity * maximumAllowedVelocity;

            if (squaredVelocity > squaredMaximumAllowedVelocity
                && squaredVelocity > 0f)
            {
                var velocityScale =
                    maximumAllowedVelocity / Mathf.Sqrt(squaredVelocity);

                expectedVelocity *= velocityScale;
            }

            expectedPosition +=
                expectedVelocity * simulationDeltaTime;

            var halfSimulationAreaSize =
                sourceSimulation.SimulationAreaSize * 0.5f;
            var simulationMinimumPosition =
                sourceSimulation.SimulationCenter - halfSimulationAreaSize;
            var simulationMaximumPosition =
                sourceSimulation.SimulationCenter + halfSimulationAreaSize;

            expectedPosition = new Vector2(
                Mathf.Clamp(
                    expectedPosition.x,
                    simulationMinimumPosition.x,
                    simulationMaximumPosition.x
                ),
                Mathf.Clamp(
                    expectedPosition.y,
                    simulationMinimumPosition.y,
                    simulationMaximumPosition.y
                )
            );
        }
        
        /// <summary>
        /// GPUで計算した最小時間刻みを非同期で読み戻し、
        /// CPU版のCFL計算結果と比較するEditor専用の検証処理。
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void RequestMinimumTimeStepValidation()
        {
            var expectedTimeStep =
                sourceSimulation.CalculateTimeStepDiagnostics().SelectedTimeStep;

            AsyncGPUReadback.Request(
                gpuBuffers.MinimumTimeStepBitsBuffer,
                request => ValidateMinimumTimeStep(
                    request,
                    expectedTimeStep
                )
            );
        }
        
        private void ValidateMinimumTimeStep(AsyncGPUReadbackRequest request, float expectedTimeStep)
        {
            if (request.hasError)
            {
                Debug.LogError(
                    "Failed to read GPU minimum time step.",
                    this
                );
                return;
            }

            var minimumTimeStepBits = request.GetData<uint>();

            if (minimumTimeStepBits.Length != 1)
            {
                Debug.LogError(
                    ZString.Format(
                        "GPU minimum time step buffer contained {0} elements, but 1 was expected.",
                        minimumTimeStepBits.Length
                    ),
                    this
                );
                return;
            }

            var gpuTimeStep = BitConverter.Int32BitsToSingle(
                unchecked((int)minimumTimeStepBits[0])
            );

            if (!AreApproximatelyEqual(gpuTimeStep, expectedTimeStep))
            {
                Debug.LogError(
                    ZString.Format(
                        "GPU minimum time step was {0}, but {1} was expected.",
                        gpuTimeStep,
                        expectedTimeStep
                    ),
                    this
                );
                return;
            }

            Debug.Log(
                ZString.Format(
                    "GPU minimum time step validation passed. Time step: {0}.",
                    gpuTimeStep
                ),
                this
            );
        }
    }
}
