using System;
using System.Diagnostics;
using System.Reflection;
using Core;
using Simulation;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class SphSwePerformanceBenchmarkMenu
    {
        private const int VirtualFrameCount = 10;
        private const float SimulationTimePerVirtualFrame = 0.02f;
        private const float LongStabilitySimulationDuration = 5f;
        private const float RemainingTimeTolerance = 0.000001f;

        private static readonly float[] MaximumSimulationTimeSteps =
        {
            0.002f,
            0.003f,
            0.004f,
            0.005f
        };

        private static readonly int[] BoundaryParticleLayerCounts =
        {
            2,
            3
        };

        private static readonly float[] LongStabilityMaximumSimulationTimeSteps =
        {
            0.002f,
            0.004f
        };

        [MenuItem("Tools/SPH-SWE/Benchmarks/Run Time Step and Boundary Layer Benchmark")]
        private static void RunTimeStepAndBoundaryLayerBenchmark()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "The performance benchmark must be run in Play mode."
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

            var originalSimulationExecutionEnabled = GetPrivateField<bool>(
                simulation,
                "simulationExecutionEnabled"
            );
            var originalMaximumSimulationTimeStep = GetPrivateField<float>(
                simulation,
                "maximumSimulationTimeStep"
            );
            var originalBoundaryParticleLayerCount = GetPrivateField<int>(
                simulation,
                "boundaryParticleLayerCount"
            );

            SetPrivateField(simulation, "simulationExecutionEnabled", false);

            try
            {
                UnityEngine.Debug.Log(
                    "SPH-SWE time-step and boundary-layer benchmark started.",
                    simulation
                );

                foreach (var boundaryParticleLayerCount in BoundaryParticleLayerCounts)
                {
                    foreach (var maximumSimulationTimeStep in MaximumSimulationTimeSteps)
                    {
                        RunBenchmarkCase(
                            simulation,
                            maximumSimulationTimeStep,
                            boundaryParticleLayerCount
                        );
                    }
                }
            }
            finally
            {
                SetPrivateField(
                    simulation,
                    "maximumSimulationTimeStep",
                    originalMaximumSimulationTimeStep
                );
                SetPrivateField(
                    simulation,
                    "boundaryParticleLayerCount",
                    originalBoundaryParticleLayerCount
                );
                simulation.GenerateParticles();
                InvokePrivateParameterlessMethod(
                    simulation,
                    "CalculateDensitiesUsingSpatialGrid"
                );
                InvokePrivateParameterlessMethod(simulation, "CalculateAccelerations");
                SetPrivateField(
                    simulation,
                    "simulationExecutionEnabled",
                    originalSimulationExecutionEnabled
                );
            }

            UnityEngine.Debug.Log(
                "SPH-SWE time-step and boundary-layer benchmark completed.",
                simulation
            );
        }

        [MenuItem("Tools/SPH-SWE/Benchmarks/Run Long Time Step Stability Benchmark")]
        private static void RunLongTimeStepStabilityBenchmark()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "The long stability benchmark must be run in Play mode."
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

            var originalSimulationExecutionEnabled = GetPrivateField<bool>(
                simulation,
                "simulationExecutionEnabled"
            );
            var originalMaximumSimulationTimeStep = GetPrivateField<float>(
                simulation,
                "maximumSimulationTimeStep"
            );

            SetPrivateField(simulation, "simulationExecutionEnabled", false);

            try
            {
                foreach (var maximumSimulationTimeStep in LongStabilityMaximumSimulationTimeSteps)
                {
                    RunLongStabilityBenchmarkCase(
                        simulation,
                        maximumSimulationTimeStep
                    );
                }
            }
            finally
            {
                SetPrivateField(
                    simulation,
                    "maximumSimulationTimeStep",
                    originalMaximumSimulationTimeStep
                );
                simulation.GenerateParticles();
                InvokePrivateParameterlessMethod(
                    simulation,
                    "CalculateDensitiesUsingSpatialGrid"
                );
                InvokePrivateParameterlessMethod(simulation, "CalculateAccelerations");
                SetPrivateField(
                    simulation,
                    "simulationExecutionEnabled",
                    originalSimulationExecutionEnabled
                );
            }

            UnityEngine.Debug.Log(
                "SPH-SWE long time-step stability benchmark completed.",
                simulation
            );
        }

        private static void RunLongStabilityBenchmarkCase(
            SphSweSimulation simulation,
            float maximumSimulationTimeStep)
        {
            SetPrivateField(
                simulation,
                "maximumSimulationTimeStep",
                maximumSimulationTimeStep
            );

            simulation.GenerateParticles();
            ApplyStrongInitialDisturbance(simulation);

            var stopwatch = Stopwatch.StartNew();
            var simulatedTime = 0f;
            var substepCount = 0;
            var cflLimitedSubstepCount = 0;
            var minimumObservedDensity = float.PositiveInfinity;
            var maximumObservedDensity = float.NegativeInfinity;
            var maximumObservedVelocity = 0f;
            var maximumInvalidParticleCount = 0;

            while (simulatedTime < LongStabilitySimulationDuration - RemainingTimeTolerance)
            {
                var availableSimulationTime = Mathf.Min(
                    maximumSimulationTimeStep,
                    LongStabilitySimulationDuration - simulatedTime
                );
                var stableTimeStep = InvokeCalculateMaximumStableTimeStep(simulation);

                if (stableTimeStep < maximumSimulationTimeStep - RemainingTimeTolerance)
                {
                    cflLimitedSubstepCount++;
                }

                var elapsedSimulationTime = InvokeSimulateAdaptiveStep(
                    simulation,
                    availableSimulationTime
                );

                simulatedTime += elapsedSimulationTime;
                substepCount++;

                var statistics = CalculateParticleStatistics(simulation);
                minimumObservedDensity = Mathf.Min(
                    minimumObservedDensity,
                    statistics.MinimumFluidDensity
                );
                maximumObservedDensity = Mathf.Max(
                    maximumObservedDensity,
                    statistics.MaximumFluidDensity
                );
                maximumObservedVelocity = Mathf.Max(
                    maximumObservedVelocity,
                    statistics.MaximumFluidVelocity
                );
                maximumInvalidParticleCount = Mathf.Max(
                    maximumInvalidParticleCount,
                    statistics.InvalidParticleCount
                );

                if (maximumInvalidParticleCount > 0)
                {
                    break;
                }
            }

            stopwatch.Stop();

            UnityEngine.Debug.Log(
                $"SPH-SWE long stability benchmark: "
                + $"maximum time step={maximumSimulationTimeStep:F3}, "
                + $"simulated time={simulatedTime:F3}, "
                + $"substeps={substepCount}, "
                + $"CFL-limited substeps={cflLimitedSubstepCount}, "
                + $"CPU time={stopwatch.Elapsed.TotalMilliseconds:F2} ms, "
                + $"observed density=[{minimumObservedDensity:F2}, "
                + $"{maximumObservedDensity:F2}], "
                + $"maximum velocity={maximumObservedVelocity:F3}, "
                + $"invalid particles={maximumInvalidParticleCount}.",
                simulation
            );
        }

        private static void ApplyStrongInitialDisturbance(SphSweSimulation simulation)
        {
            var particles = simulation.Particles;
            var simulationCenter = GetPrivateField<Vector2>(simulation, "simulationCenter");

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                ref var particle = ref particles[particleIndex];

                if (particle.Type != SphSweParticleType.Fluid)
                {
                    continue;
                }

                if (particle.Position.x < simulationCenter.x)
                {
                    particle.Mass *= 1.5f;
                    particle.Velocity = Vector2.right * 1.5f;
                    continue;
                }

                particle.Mass *= 0.5f;
                particle.Velocity = Vector2.left * 1.5f;
            }
        }

        private static void RunBenchmarkCase(
            SphSweSimulation simulation,
            float maximumSimulationTimeStep,
            int boundaryParticleLayerCount)
        {
            SetPrivateField(
                simulation,
                "maximumSimulationTimeStep",
                maximumSimulationTimeStep
            );
            SetPrivateField(
                simulation,
                "boundaryParticleLayerCount",
                boundaryParticleLayerCount
            );

            simulation.GenerateParticles();
            var wallAdjacentFluidDensity =
                MoveFluidParticleNextToWallAndCalculateDensity(simulation);

            var stopwatch = Stopwatch.StartNew();
            var totalSubstepCount = 0;
            var incompleteVirtualFrameCount = 0;

            for (var virtualFrameIndex = 0;
                 virtualFrameIndex < VirtualFrameCount;
                 virtualFrameIndex++)
            {
                var remainingSimulationTime = SimulationTimePerVirtualFrame;
                var completedSubstepCount = 0;
                var maximumSubstepCount = GetPrivateField<int>(
                    simulation,
                    "maximumSimulationSubstepCount"
                );

                while (remainingSimulationTime > RemainingTimeTolerance
                       && completedSubstepCount < maximumSubstepCount)
                {
                    var elapsedSimulationTime = InvokeSimulateAdaptiveStep(
                        simulation,
                        remainingSimulationTime
                    );

                    remainingSimulationTime = Mathf.Max(
                        0f,
                        remainingSimulationTime - elapsedSimulationTime
                    );
                    completedSubstepCount++;
                    totalSubstepCount++;
                }

                if (remainingSimulationTime > RemainingTimeTolerance)
                {
                    incompleteVirtualFrameCount++;
                }
            }

            stopwatch.Stop();

            var statistics = CalculateParticleStatistics(simulation);
            var averageFrameMilliseconds =
                stopwatch.Elapsed.TotalMilliseconds / VirtualFrameCount;

            UnityEngine.Debug.Log(
                $"SPH-SWE benchmark: layers={boundaryParticleLayerCount}, "
                + $"maximum time step={maximumSimulationTimeStep:F3}, "
                + $"particles={statistics.TotalParticleCount}, "
                + $"boundary particles={statistics.BoundaryParticleCount}, "
                + $"substeps={totalSubstepCount}, "
                + $"average CPU time={averageFrameMilliseconds:F2} ms, "
                + $"density=[{statistics.MinimumFluidDensity:F2}, "
                + $"{statistics.MaximumFluidDensity:F2}], "
                + $"wall density={wallAdjacentFluidDensity:F2}, "
                + $"maximum velocity={statistics.MaximumFluidVelocity:F3}, "
                + $"invalid particles={statistics.InvalidParticleCount}, "
                + $"incomplete frames={incompleteVirtualFrameCount}.",
                simulation
            );
        }

        private static float MoveFluidParticleNextToWallAndCalculateDensity(
            SphSweSimulation simulation)
        {
            var particles = simulation.Particles;
            var simulationCenter = GetPrivateField<Vector2>(simulation, "simulationCenter");
            var simulationAreaSize = GetPrivateField<Vector2>(simulation, "simulationAreaSize");
            var minimumSimulationPosition = simulationCenter - simulationAreaSize * 0.5f;

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                ref var particle = ref particles[particleIndex];

                if (particle.Type != SphSweParticleType.Fluid)
                {
                    continue;
                }

                particle.Position = new Vector2(
                    minimumSimulationPosition.x + simulation.ParticleSpacing * 0.5f,
                    simulationCenter.y
                );
                particle.Velocity = Vector2.zero;
                particle.Acceleration = Vector2.zero;

                InvokePrivateParameterlessMethod(
                    simulation,
                    "CalculateDensitiesUsingSpatialGrid"
                );

                return particle.Density;
            }

            throw new InvalidOperationException(
                "The simulation does not contain any fluid particles."
            );
        }

        private static ParticleStatistics CalculateParticleStatistics(
            SphSweSimulation simulation)
        {
            var particles = simulation.Particles;
            var minimumFluidDensity = float.PositiveInfinity;
            var maximumFluidDensity = float.NegativeInfinity;
            var maximumFluidVelocity = 0f;
            var boundaryParticleCount = 0;
            var invalidParticleCount = 0;

            foreach (var particle in particles)
            {
                if (!IsFinite(particle.Position.x)
                    || !IsFinite(particle.Position.y)
                    || !IsFinite(particle.Velocity.x)
                    || !IsFinite(particle.Velocity.y)
                    || !IsFinite(particle.Density)
                    || !IsFinite(particle.FluidDepth))
                {
                    invalidParticleCount++;
                }

                if (particle.Type == SphSweParticleType.Boundary)
                {
                    boundaryParticleCount++;
                    continue;
                }

                minimumFluidDensity = Mathf.Min(minimumFluidDensity, particle.Density);
                maximumFluidDensity = Mathf.Max(maximumFluidDensity, particle.Density);

                var squaredVelocity =
                    particle.Velocity.x * particle.Velocity.x
                    + particle.Velocity.y * particle.Velocity.y;

                maximumFluidVelocity = Mathf.Max(
                    maximumFluidVelocity,
                    Mathf.Sqrt(squaredVelocity)
                );
            }

            return new ParticleStatistics(
                particles.Length,
                boundaryParticleCount,
                invalidParticleCount,
                minimumFluidDensity,
                maximumFluidDensity,
                maximumFluidVelocity
            );
        }

        private static float InvokeSimulateAdaptiveStep(
            SphSweSimulation simulation,
            float availableSimulationTime)
        {
            var method = typeof(SphSweSimulation).GetMethod(
                "SimulateAdaptiveStep",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (method == null)
            {
                throw new MissingMethodException(
                    nameof(SphSweSimulation),
                    "SimulateAdaptiveStep"
                );
            }

            return (float)method.Invoke(simulation, new object[] { availableSimulationTime });
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

        private static void InvokePrivateParameterlessMethod(
            SphSweSimulation simulation,
            string methodName)
        {
            var method = typeof(SphSweSimulation).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (method == null)
            {
                throw new MissingMethodException(nameof(SphSweSimulation), methodName);
            }

            method.Invoke(simulation, null);
        }

        private static T GetPrivateField<T>(
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

            return (T)field.GetValue(simulation);
        }

        private static void SetPrivateField<T>(
            SphSweSimulation simulation,
            string fieldName,
            T value)
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

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private readonly struct ParticleStatistics
        {
            public readonly int TotalParticleCount;
            public readonly int BoundaryParticleCount;
            public readonly int InvalidParticleCount;
            public readonly float MinimumFluidDensity;
            public readonly float MaximumFluidDensity;
            public readonly float MaximumFluidVelocity;

            public ParticleStatistics(
                int totalParticleCount,
                int boundaryParticleCount,
                int invalidParticleCount,
                float minimumFluidDensity,
                float maximumFluidDensity,
                float maximumFluidVelocity)
            {
                TotalParticleCount = totalParticleCount;
                BoundaryParticleCount = boundaryParticleCount;
                InvalidParticleCount = invalidParticleCount;
                MinimumFluidDensity = minimumFluidDensity;
                MaximumFluidDensity = maximumFluidDensity;
                MaximumFluidVelocity = maximumFluidVelocity;
            }
        }
    }
}
