using System;
using Simulation;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class SphSweTestMenu
    {
        private const float VectorComparisonTolerance = 0.00001f;

        [MenuItem("Tools/SPH-SWE/Tests/Run Kernel Tests")]
        private static void RunKernelTests()
        {
            VerifyInvalidEffectiveRadiusThrowsException();
            VerifyZeroDistanceReturnsZeroGradient();
            VerifyBoundaryDistanceReturnsZeroGradient();
            VerifyGradientInsideEffectiveRadius();
            VerifyReversedPositionDifferenceReversesGradient();

            Debug.Log("All SPH-SWE kernel tests passed.");
        }

        [MenuItem("Tools/SPH-SWE/Tests/Validate Running Simulation Accelerations")]
        private static void ValidateRunningSimulationAccelerations()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "The acceleration validation must be run in Play mode."
                );
            }

            var simulation =
                UnityEngine.Object.FindFirstObjectByType<SphSweSimulation>();

            if (simulation == null)
            {
                throw new InvalidOperationException(
                    "SphSweSimulation was not found in the current scene."
                );
            }

            var particles = simulation.Particles;

            if (particles.Length == 0)
            {
                throw new InvalidOperationException(
                    "The simulation does not contain any particles."
                );
            }

            var totalAcceleration = Vector2.zero;
            var acceleratedParticleCount = 0;

            foreach (var particle in particles)
            {
                VerifyFiniteVector(
                    particle.Acceleration,
                    "A particle acceleration contains NaN or Infinity."
                );

                totalAcceleration += particle.Acceleration;

                var squaredAccelerationMagnitude =
                    particle.Acceleration.x * particle.Acceleration.x
                    + particle.Acceleration.y * particle.Acceleration.y;

                if (squaredAccelerationMagnitude
                    > VectorComparisonTolerance * VectorComparisonTolerance)
                {
                    acceleratedParticleCount++;
                }
            }

            if (acceleratedParticleCount == 0)
            {
                throw new InvalidOperationException(
                    "No particle received a fluid depth gradient acceleration."
                );
            }

            VerifyVectorApproximatelyEqual(
                totalAcceleration,
                Vector2.zero,
                "The total acceleration of the symmetric particle grid is not zero."
            );

            var oppositeCornerAccelerationSum =
                particles[0].Acceleration
                + particles[particles.Length - 1].Acceleration;

            VerifyVectorApproximatelyEqual(
                oppositeCornerAccelerationSum,
                Vector2.zero,
                "Opposite corner accelerations are not symmetric."
            );

            Debug.Log(
                $"Fluid depth gradient acceleration test passed. "
                + $"Accelerated particles: {acceleratedParticleCount}/{particles.Length}."
            );
        }

        private static void VerifyInvalidEffectiveRadiusThrowsException()
        {
            try
            {
                SphSweKernel.EvaluateSpikyGradient(
                    Vector2.right,
                    0f
                );
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }

            throw new InvalidOperationException(
                "A nonpositive effective radius did not throw an exception."
            );
        }

        private static void VerifyZeroDistanceReturnsZeroGradient()
        {
            var gradient = SphSweKernel.EvaluateSpikyGradient(
                Vector2.zero,
                1f
            );

            VerifyVectorApproximatelyEqual(
                gradient,
                Vector2.zero,
                "A zero distance did not return a zero gradient."
            );
        }

        private static void VerifyBoundaryDistanceReturnsZeroGradient()
        {
            var gradient = SphSweKernel.EvaluateSpikyGradient(
                Vector2.right,
                1f
            );

            VerifyVectorApproximatelyEqual(
                gradient,
                Vector2.zero,
                "A boundary distance did not return a zero gradient."
            );
        }

        private static void VerifyGradientInsideEffectiveRadius()
        {
            var gradient = SphSweKernel.EvaluateSpikyGradient(
                new Vector2(0.5f, 0f),
                1f
            );

            var expectedGradient = new Vector2(
                -7.5f / Mathf.PI,
                0f
            );

            VerifyVectorApproximatelyEqual(
                gradient,
                expectedGradient,
                "The Spiky gradient inside the effective radius is incorrect."
            );
        }

        private static void VerifyReversedPositionDifferenceReversesGradient()
        {
            var positiveDirectionGradient =
                SphSweKernel.EvaluateSpikyGradient(
                    new Vector2(0.5f, 0f),
                    1f
                );

            var negativeDirectionGradient =
                SphSweKernel.EvaluateSpikyGradient(
                    new Vector2(-0.5f, 0f),
                    1f
                );

            VerifyVectorApproximatelyEqual(
                positiveDirectionGradient + negativeDirectionGradient,
                Vector2.zero,
                "Reversing the position difference did not reverse the gradient."
            );
        }

        private static void VerifyFiniteVector(
            Vector2 value,
            string failureMessage)
        {
            if (float.IsNaN(value.x)
                || float.IsNaN(value.y)
                || float.IsInfinity(value.x)
                || float.IsInfinity(value.y))
            {
                throw new InvalidOperationException(failureMessage);
            }
        }

        private static void VerifyVectorApproximatelyEqual(
            Vector2 actualValue,
            Vector2 expectedValue,
            string failureMessage)
        {
            var differenceX = actualValue.x - expectedValue.x;
            var differenceY = actualValue.y - expectedValue.y;

            if (Mathf.Abs(differenceX) <= VectorComparisonTolerance
                && Mathf.Abs(differenceY) <= VectorComparisonTolerance)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{failureMessage} "
                + $"Expected ({expectedValue.x}, {expectedValue.y}), "
                + $"but received ({actualValue.x}, {actualValue.y})."
            );
        }
    }
}
