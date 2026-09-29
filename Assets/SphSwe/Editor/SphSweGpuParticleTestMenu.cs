using System;
using System.Runtime.InteropServices;
using SphSwe.Gpu;
using UnityEditor;
using UnityEngine;

namespace SphSwe.Editor
{
    public static class SphSweGpuParticleTestMenu
    {
        private const string ParticleCopyComputeShaderPath =
            "Assets/SphSwe/Shaders/Compute/SphSweParticleCopy.compute";

        private static readonly int InputParticlesPropertyId =
            Shader.PropertyToID("_InputParticles");
        private static readonly int OutputParticlesPropertyId =
            Shader.PropertyToID("_OutputParticles");
        private static readonly int ParticleCountPropertyId =
            Shader.PropertyToID("_ParticleCount");

        [MenuItem("Tools/SPH-SWE/Tests/Test GPU Particle Layout")]
        private static void TestGpuParticleLayout()
        {
            var marshalSize = Marshal.SizeOf<SphSweGpuParticle>();

            VerifySize(marshalSize, "Marshal.SizeOf");

            VerifyFieldOffset(nameof(SphSweGpuParticle.Position), 0);
            VerifyFieldOffset(nameof(SphSweGpuParticle.Velocity), 8);
            VerifyFieldOffset(nameof(SphSweGpuParticle.Acceleration), 16);
            VerifyFieldOffset(nameof(SphSweGpuParticle.Density), 24);
            VerifyFieldOffset(nameof(SphSweGpuParticle.Mass), 28);
            VerifyFieldOffset(nameof(SphSweGpuParticle.InitialMass), 32);
            VerifyFieldOffset(nameof(SphSweGpuParticle.EffectiveRadius), 36);
            VerifyFieldOffset(nameof(SphSweGpuParticle.FluidDepth), 40);
            VerifyFieldOffset(nameof(SphSweGpuParticle.Type), 44);

            Debug.Log(
                $"SPH-SWE GPU particle layout test passed. "
                + $"Size: {marshalSize} bytes."
            );
        }

        [MenuItem("Tools/SPH-SWE/Tests/Test GPU Particle Copy")]
        private static void TestGpuParticleCopy()
        {
            var computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                ParticleCopyComputeShaderPath
            );

            if (computeShader == null)
            {
                throw new InvalidOperationException(
                    $"Compute Shader was not found at "
                    + $"{ParticleCopyComputeShaderPath}."
                );
            }

            var inputParticles = CreateTestParticles();
            var outputParticles = new SphSweGpuParticle[inputParticles.Length];

            using var inputBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                inputParticles.Length,
                SphSweGpuParticle.Stride
            );
            using var outputBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                outputParticles.Length,
                SphSweGpuParticle.Stride
            );

            inputBuffer.SetData(inputParticles);

            var kernelIndex = computeShader.FindKernel("CopyParticles");
            computeShader.SetBuffer(
                kernelIndex,
                InputParticlesPropertyId,
                inputBuffer
            );
            computeShader.SetBuffer(
                kernelIndex,
                OutputParticlesPropertyId,
                outputBuffer
            );
            computeShader.SetInt(
                ParticleCountPropertyId,
                inputParticles.Length
            );

            computeShader.GetKernelThreadGroupSizes(
                kernelIndex,
                out var threadCountX,
                out _,
                out _
            );

            var threadGroupCountX =
                (inputParticles.Length + (int)threadCountX - 1)
                / (int)threadCountX;

            computeShader.Dispatch(kernelIndex, threadGroupCountX, 1, 1);
            outputBuffer.GetData(outputParticles);

            for (var particleIndex = 0;
                 particleIndex < inputParticles.Length;
                 particleIndex++)
            {
                VerifyParticle(
                    inputParticles[particleIndex],
                    outputParticles[particleIndex],
                    particleIndex
                );
            }

            Debug.Log(
                $"SPH-SWE GPU particle copy test passed. "
                + $"Particle count: {inputParticles.Length}."
            );
        }

        private static SphSweGpuParticle[] CreateTestParticles()
        {
            return new[]
            {
                new SphSweGpuParticle
                {
                    Position = new Vector2(1.25f, -2.5f),
                    Velocity = new Vector2(3.75f, -4.125f),
                    Acceleration = new Vector2(5.5f, -6.25f),
                    Density = 998.29f,
                    Mass = 2f,
                    InitialMass = 2.25f,
                    EffectiveRadius = 0.112935f,
                    FluidDepth = 1.125f,
                    Type = 0
                },
                new SphSweGpuParticle
                {
                    Position = new Vector2(-7.5f, 8.25f),
                    Velocity = new Vector2(-9.5f, 10.75f),
                    Acceleration = new Vector2(-11.25f, 12.5f),
                    Density = 1234.5f,
                    Mass = 3.25f,
                    InitialMass = 3.5f,
                    EffectiveRadius = 0.25f,
                    FluidDepth = 1.75f,
                    Type = 1
                }
            };
        }

        private static void VerifyParticle(
            SphSweGpuParticle expected,
            SphSweGpuParticle actual,
            int particleIndex)
        {
            VerifyField(expected.Position, actual.Position, particleIndex, "Position");
            VerifyField(expected.Velocity, actual.Velocity, particleIndex, "Velocity");
            VerifyField(expected.Acceleration, actual.Acceleration, particleIndex, "Acceleration");
            VerifyField(expected.Density, actual.Density, particleIndex, "Density");
            VerifyField(expected.Mass, actual.Mass, particleIndex, "Mass");
            VerifyField(expected.InitialMass, actual.InitialMass, particleIndex, "InitialMass");
            VerifyField(expected.EffectiveRadius, actual.EffectiveRadius, particleIndex, "EffectiveRadius");
            VerifyField(expected.FluidDepth, actual.FluidDepth, particleIndex, "FluidDepth");
            VerifyField(expected.Type, actual.Type, particleIndex, "Type");
        }

        private static void VerifyField<T>(
            T expected,
            T actual,
            int particleIndex,
            string fieldName)
            where T : IEquatable<T>
        {
            if (expected.Equals(actual))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Particle {particleIndex} field {fieldName} was {actual}, "
                + $"but {expected} was expected."
            );
        }

        private static void VerifySize(
            int actualSize,
            string sizeCalculationMethodName)
        {
            if (actualSize == SphSweGpuParticle.Stride)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{sizeCalculationMethodName} returned {actualSize} bytes, "
                + $"but {SphSweGpuParticle.Stride} bytes were expected."
            );
        }

        private static void VerifyFieldOffset(
            string fieldName,
            int expectedOffset)
        {
            var actualOffset = Marshal.OffsetOf<SphSweGpuParticle>(
                fieldName
            ).ToInt32();

            if (actualOffset == expectedOffset)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{fieldName} starts at byte {actualOffset}, "
                + $"but byte {expectedOffset} was expected."
            );
        }
    }
}
