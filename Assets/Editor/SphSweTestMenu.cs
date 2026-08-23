using System;
using System.Reflection;
using Core;
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
            VerifyNegativeSquaredDistanceThrowsForViscosity();
            VerifyInvalidEffectiveRadiusThrowsForViscosity();
            VerifyViscosityLaplacianAtCenter();
            VerifyViscosityLaplacianInsideEffectiveRadius();
            VerifyViscosityLaplacianAtBoundary();

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

        [MenuItem("Tools/SPH-SWE/Tests/Test Running Simulation Viscosity")]
        private static void TestRunningSimulationViscosity()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "The viscosity test must be run in Play mode."
                );
            }

            var simulation = UnityEngine.Object.FindFirstObjectByType<SphSweSimulation>();

            if (simulation == null)
            {
                throw new InvalidOperationException(
                    "SphSweSimulation was not found in the current scene."
                );
            }

            var particles = simulation.Particles;

            if (particles.Length < 2)
            {
                throw new InvalidOperationException(
                    "At least two particles are required for the viscosity test."
                );
            }

            foreach (ref var particle in particles.AsSpan())
            {
                particle.Velocity = Vector2.zero;
            }

            InvokeCalculateAccelerations(simulation);

            var baselineAccelerations = new Vector2[particles.Length];

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                baselineAccelerations[particleIndex] = particles[particleIndex].Acceleration;
            }

            var testParticleIndex = FindParticleClosestToCenter(particles);
            particles[testParticleIndex].Velocity = Vector2.right;

            try
            {
                InvokeCalculateAccelerations(simulation);

                var testParticleViscosityAcceleration =
                    particles[testParticleIndex].Acceleration
                    - baselineAccelerations[testParticleIndex];

                if (testParticleViscosityAcceleration.x >= -VectorComparisonTolerance)
                {
                    throw new InvalidOperationException(
                        "Viscosity did not decelerate the test particle."
                    );
                }

                var acceleratedNeighborFound = false;

                for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
                {
                    if (particleIndex == testParticleIndex)
                    {
                        continue;
                    }

                    var viscosityAcceleration =
                        particles[particleIndex].Acceleration
                        - baselineAccelerations[particleIndex];

                    VerifyFiniteVector(
                        viscosityAcceleration,
                        "A viscosity acceleration contains NaN or Infinity."
                    );

                    if (viscosityAcceleration.x > VectorComparisonTolerance)
                    {
                        acceleratedNeighborFound = true;
                    }
                }

                if (!acceleratedNeighborFound)
                {
                    throw new InvalidOperationException(
                        "Viscosity did not accelerate any neighboring particle."
                    );
                }

                Debug.Log(
                    $"Viscosity acceleration test passed. "
                    + $"Test particle acceleration change: {testParticleViscosityAcceleration}."
                );
            }
            finally
            {
                particles[testParticleIndex].Velocity = Vector2.zero;
                InvokeCalculateAccelerations(simulation);
            }
        }

        [MenuItem("Tools/SPH-SWE/Tests/Test Running Simulation Integration")]
        private static void TestRunningSimulationIntegration()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "The integration test must be run in Play mode."
                );
            }

            var simulation = UnityEngine.Object.FindFirstObjectByType<SphSweSimulation>();

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

            var originalParticles = (SphSweParticle[])particles.Clone();

            try
            {
                foreach (ref var particle in particles.AsSpan())
                {
                    particle.Type = SphSweParticleType.Boundary;
                }

                VerifySemiImplicitEulerIntegration(simulation, particles);
                VerifyIntegrationVelocityLimit(simulation, particles);
                VerifySimulationAreaClamping(simulation, particles);
                VerifyBoundaryParticleIsNotIntegrated(simulation, particles);

                Debug.Log("SPH-SWE integration tests passed.");
            }
            finally
            {
                Array.Copy(originalParticles, particles, particles.Length);
            }
        }

        [MenuItem("Tools/SPH-SWE/Tests/Test Running Simulation Time Step")]
        private static void TestRunningSimulationTimeStep()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "The time-step test must be run in Play mode."
                );
            }

            var simulation = UnityEngine.Object.FindFirstObjectByType<SphSweSimulation>();

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

            var originalParticles = (SphSweParticle[])particles.Clone();
            var simulationExecutionEnabled = GetPrivateBool(
                simulation,
                "simulationExecutionEnabled"
            );

            SetPrivateBool(simulation, "simulationExecutionEnabled", false);

            try
            {
                VerifyMaximumTimeStepUpperLimit(simulation, particles);
                VerifyHighVelocityReducesTimeStep(simulation, particles);
                VerifyHighFluidDepthReducesTimeStep(simulation, particles);
                VerifyBoundaryParticlesDoNotAffectTimeStep(simulation, particles);

                Debug.Log("SPH-SWE time-step tests passed.");
            }
            finally
            {
                Array.Copy(originalParticles, particles, particles.Length);
                SetPrivateBool(
                    simulation,
                    "simulationExecutionEnabled",
                    simulationExecutionEnabled
                );
            }
        }

        private static void VerifyMaximumTimeStepUpperLimit(
            SphSweSimulation simulation,
            SphSweParticle[] particles)
        {
            SetAllParticlesToBoundary(particles);

            ref var particle = ref particles[0];
            particle.Type = SphSweParticleType.Fluid;
            particle.Velocity = Vector2.zero;
            particle.FluidDepth = 0f;

            var actualTimeStep = InvokeCalculateMaximumStableTimeStep(simulation);
            var expectedTimeStep = GetPrivateFloat(simulation, "maximumSimulationTimeStep");

            VerifyFloatApproximatelyEqual(
                actualTimeStep,
                expectedTimeStep,
                "The maximum simulation time-step upper limit is incorrect."
            );
        }

        private static void VerifyHighVelocityReducesTimeStep(
            SphSweSimulation simulation,
            SphSweParticle[] particles)
        {
            SetAllParticlesToBoundary(particles);

            ref var particle = ref particles[0];
            particle.Type = SphSweParticleType.Fluid;
            particle.Velocity = new Vector2(1000f, 0f);
            particle.FluidDepth = 0f;

            var courantNumber = GetPrivateFloat(simulation, "courantNumber");
            var expectedTimeStep = courantNumber * particle.EffectiveRadius / 1000f;
            var actualTimeStep = InvokeCalculateMaximumStableTimeStep(simulation);

            VerifyFloatApproximatelyEqual(
                actualTimeStep,
                expectedTimeStep,
                "A high particle velocity did not reduce the CFL time step correctly."
            );
        }

        private static void VerifyHighFluidDepthReducesTimeStep(
            SphSweSimulation simulation,
            SphSweParticle[] particles)
        {
            SetAllParticlesToBoundary(particles);

            ref var particle = ref particles[0];
            particle.Type = SphSweParticleType.Fluid;
            particle.Velocity = Vector2.zero;
            particle.FluidDepth = 10000f;

            var courantNumber = GetPrivateFloat(simulation, "courantNumber");
            var gravityAcceleration = GetPrivateFloat(simulation, "gravityAcceleration");
            var shallowWaterWaveSpeed = Mathf.Sqrt(gravityAcceleration * particle.FluidDepth);
            var expectedTimeStep = courantNumber * particle.EffectiveRadius / shallowWaterWaveSpeed;
            var actualTimeStep = InvokeCalculateMaximumStableTimeStep(simulation);

            VerifyFloatApproximatelyEqual(
                actualTimeStep,
                expectedTimeStep,
                "A high fluid depth did not reduce the CFL time step correctly."
            );
        }

        private static void VerifyBoundaryParticlesDoNotAffectTimeStep(
            SphSweSimulation simulation,
            SphSweParticle[] particles)
        {
            SetAllParticlesToBoundary(particles);

            foreach (ref var particle in particles.AsSpan())
            {
                particle.Velocity = new Vector2(1000f, 1000f);
                particle.FluidDepth = 10000f;
            }

            var actualTimeStep = InvokeCalculateMaximumStableTimeStep(simulation);
            var expectedTimeStep = GetPrivateFloat(simulation, "maximumSimulationTimeStep");

            VerifyFloatApproximatelyEqual(
                actualTimeStep,
                expectedTimeStep,
                "Boundary particles incorrectly affected the CFL time step."
            );
        }

        private static void SetAllParticlesToBoundary(SphSweParticle[] particles)
        {
            foreach (ref var particle in particles.AsSpan())
            {
                particle.Type = SphSweParticleType.Boundary;
            }
        }

        private static void VerifySemiImplicitEulerIntegration(
            SphSweSimulation simulation,
            SphSweParticle[] particles)
        {
            const float deltaTime = 0.25f;

            ref var particle = ref particles[0];
            particle.Type = SphSweParticleType.Fluid;
            particle.Position = Vector2.zero;
            particle.Velocity = new Vector2(2f, 3f);
            particle.Acceleration = new Vector2(4f, -2f);
            particle.FluidDepth = 100f;

            InvokeIntegrateParticles(simulation, deltaTime);

            VerifyVectorApproximatelyEqual(
                particle.Velocity,
                new Vector2(3f, 2.5f),
                "The velocity was not updated correctly."
            );
            VerifyVectorApproximatelyEqual(
                particle.Position,
                new Vector2(0.75f, 0.625f),
                "The position was not updated using the new velocity."
            );

            particle.Type = SphSweParticleType.Boundary;
        }

        private static void VerifyIntegrationVelocityLimit(
            SphSweSimulation simulation,
            SphSweParticle[] particles)
        {
            ref var particle = ref particles[0];
            particle.Type = SphSweParticleType.Fluid;
            particle.Position = Vector2.zero;
            particle.Velocity = new Vector2(6f, 8f);
            particle.Acceleration = Vector2.zero;
            particle.FluidDepth = 1f;

            InvokeIntegrateParticles(simulation, 0.1f);

            var gravityAcceleration = GetPrivateFloat(simulation, "gravityAcceleration");
            var expectedMaximumVelocity = Mathf.Sqrt(gravityAcceleration);
            var actualVelocityMagnitude = Mathf.Sqrt(
                particle.Velocity.x * particle.Velocity.x
                + particle.Velocity.y * particle.Velocity.y
            );

            VerifyFloatApproximatelyEqual(
                actualVelocityMagnitude,
                expectedMaximumVelocity,
                "The particle velocity was not limited to the shallow-water wave speed."
            );

            particle.Type = SphSweParticleType.Boundary;
        }

        private static void VerifyBoundaryParticleIsNotIntegrated(
            SphSweSimulation simulation,
            SphSweParticle[] particles)
        {
            ref var particle = ref particles[0];
            particle.Type = SphSweParticleType.Boundary;
            particle.Position = new Vector2(1f, 2f);
            particle.Velocity = new Vector2(3f, 4f);
            particle.Acceleration = new Vector2(5f, 6f);
            particle.FluidDepth = 1f;

            var originalPosition = particle.Position;
            var originalVelocity = particle.Velocity;

            InvokeIntegrateParticles(simulation, 0.1f);

            VerifyVectorApproximatelyEqual(
                particle.Position,
                originalPosition,
                "A boundary particle position was changed by integration."
            );
            VerifyVectorApproximatelyEqual(
                particle.Velocity,
                originalVelocity,
                "A boundary particle velocity was changed by integration."
            );
        }

        private static void VerifySimulationAreaClamping(
            SphSweSimulation simulation,
            SphSweParticle[] particles)
        {
            var simulationCenter = GetPrivateVector2(simulation, "simulationCenter");
            var simulationAreaSize = GetPrivateVector2(simulation, "simulationAreaSize");
            var halfSimulationAreaSize = simulationAreaSize * 0.5f;
            var minimumPosition = simulationCenter - halfSimulationAreaSize;
            var maximumPosition = simulationCenter + halfSimulationAreaSize;

            VerifyClampedPosition(
                simulation,
                particles,
                minimumPosition - Vector2.one,
                minimumPosition
            );
            VerifyClampedPosition(
                simulation,
                particles,
                maximumPosition + Vector2.one,
                maximumPosition
            );
            VerifyClampedPosition(
                simulation,
                particles,
                simulationCenter,
                simulationCenter
            );
        }

        private static void VerifyClampedPosition(
            SphSweSimulation simulation,
            SphSweParticle[] particles,
            Vector2 inputPosition,
            Vector2 expectedPosition)
        {
            ref var particle = ref particles[0];
            particle.Type = SphSweParticleType.Fluid;
            particle.Position = inputPosition;
            particle.Velocity = Vector2.zero;
            particle.Acceleration = Vector2.zero;
            particle.FluidDepth = 0f;

            InvokeIntegrateParticles(simulation, 0.1f);

            VerifyVectorApproximatelyEqual(
                particle.Position,
                expectedPosition,
                "The particle position was not clamped to the simulation area."
            );

            particle.Type = SphSweParticleType.Boundary;
        }

        private static int FindParticleClosestToCenter(Core.SphSweParticle[] particles)
        {
            var center = Vector2.zero;

            foreach (var particle in particles)
            {
                center += particle.Position;
            }

            center /= particles.Length;

            var closestParticleIndex = 0;
            var minimumSquaredDistance = float.PositiveInfinity;

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                var positionDifference = particles[particleIndex].Position - center;
                var squaredDistance = positionDifference.x * positionDifference.x
                                      + positionDifference.y * positionDifference.y;

                if (squaredDistance >= minimumSquaredDistance)
                {
                    continue;
                }

                closestParticleIndex = particleIndex;
                minimumSquaredDistance = squaredDistance;
            }

            return closestParticleIndex;
        }

        private static void InvokeCalculateAccelerations(SphSweSimulation simulation)
        {
            var calculateAccelerationsMethod = typeof(SphSweSimulation).GetMethod(
                "CalculateAccelerations",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (calculateAccelerationsMethod == null)
            {
                throw new MissingMethodException(
                    nameof(SphSweSimulation),
                    "CalculateAccelerations"
                );
            }

            calculateAccelerationsMethod.Invoke(simulation, null);
        }

        private static void InvokeIntegrateParticles(
            SphSweSimulation simulation,
            float deltaTime)
        {
            var integrateParticlesMethod = typeof(SphSweSimulation).GetMethod(
                "IntegrateParticles",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (integrateParticlesMethod == null)
            {
                throw new MissingMethodException(
                    nameof(SphSweSimulation),
                    "IntegrateParticles"
                );
            }

            integrateParticlesMethod.Invoke(simulation, new object[] { deltaTime });
        }

        private static float InvokeCalculateMaximumStableTimeStep(
            SphSweSimulation simulation)
        {
            var method = typeof(SphSweSimulation).GetMethod(
                "CalculateMaximumStableTimeStep",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (method == null)
            {
                throw new MissingMethodException(
                    nameof(SphSweSimulation),
                    "CalculateMaximumStableTimeStep"
                );
            }

            return (float)method.Invoke(simulation, null);
        }

        private static float GetPrivateFloat(
            SphSweSimulation simulation,
            string fieldName)
        {
            var field = typeof(SphSweSimulation).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (field == null)
            {
                throw new MissingFieldException(nameof(SphSweSimulation), fieldName);
            }

            return (float)field.GetValue(simulation);
        }

        private static Vector2 GetPrivateVector2(
            SphSweSimulation simulation,
            string fieldName)
        {
            var field = typeof(SphSweSimulation).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (field == null)
            {
                throw new MissingFieldException(nameof(SphSweSimulation), fieldName);
            }

            return (Vector2)field.GetValue(simulation);
        }

        private static bool GetPrivateBool(
            SphSweSimulation simulation,
            string fieldName)
        {
            var field = typeof(SphSweSimulation).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (field == null)
            {
                throw new MissingFieldException(nameof(SphSweSimulation), fieldName);
            }

            return (bool)field.GetValue(simulation);
        }

        private static void SetPrivateBool(
            SphSweSimulation simulation,
            string fieldName,
            bool value)
        {
            var field = typeof(SphSweSimulation).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (field == null)
            {
                throw new MissingFieldException(nameof(SphSweSimulation), fieldName);
            }

            field.SetValue(simulation, value);
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

        private static void VerifyNegativeSquaredDistanceThrowsForViscosity()
        {
            try
            {
                SphSweKernel.EvaluateViscosityLaplacian(-1f, 1f);
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }

            throw new InvalidOperationException(
                "A negative squared distance did not throw an exception."
            );
        }

        private static void VerifyInvalidEffectiveRadiusThrowsForViscosity()
        {
            try
            {
                SphSweKernel.EvaluateViscosityLaplacian(0f, 0f);
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }

            throw new InvalidOperationException(
                "A nonpositive effective radius did not throw an exception for viscosity."
            );
        }

        private static void VerifyViscosityLaplacianAtCenter()
        {
            var actualValue = SphSweKernel.EvaluateViscosityLaplacian(0f, 1f);
            var expectedValue = 20f / (3f * Mathf.PI);

            VerifyFloatApproximatelyEqual(
                actualValue,
                expectedValue,
                "The viscosity Laplacian at the center is incorrect."
            );
        }

        private static void VerifyViscosityLaplacianInsideEffectiveRadius()
        {
            var actualValue = SphSweKernel.EvaluateViscosityLaplacian(0.25f, 1f);
            var expectedValue = 10f / (3f * Mathf.PI);

            VerifyFloatApproximatelyEqual(
                actualValue,
                expectedValue,
                "The viscosity Laplacian inside the effective radius is incorrect."
            );
        }

        private static void VerifyViscosityLaplacianAtBoundary()
        {
            var actualValue = SphSweKernel.EvaluateViscosityLaplacian(1f, 1f);

            VerifyFloatApproximatelyEqual(
                actualValue,
                0f,
                "The viscosity Laplacian at the boundary is not zero."
            );
        }

        private static void VerifyFloatApproximatelyEqual(
            float actualValue,
            float expectedValue,
            string failureMessage)
        {
            if (Mathf.Abs(actualValue - expectedValue) <= VectorComparisonTolerance)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{failureMessage} Expected {expectedValue}, but received {actualValue}."
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
