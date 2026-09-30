using System;
using UnityEngine;

namespace SphSwe.Gpu
{
    public sealed class SphSweGpuBuffers : IDisposable
    {
        private const int ScanElementCount = 256;
        private bool disposed;
        
        public GraphicsBuffer ParticleBuffer { get; }
        public GraphicsBuffer ParticleCellIndexBuffer { get; }
        public GraphicsBuffer CellParticleCountBuffer { get; }
        public GraphicsBuffer CellParticleStartIndexBuffer { get; }
        public GraphicsBuffer CellParticleCountSumByGroupBuffer { get; }
        public GraphicsBuffer CellParticleCountStartIndexByGroupBuffer { get; }
        public GraphicsBuffer CellParticleWriteIndexBuffer { get; }
        public GraphicsBuffer SortedParticleIndexBuffer { get; }
        public GraphicsBuffer MinimumTimeStepBitsBuffer { get; }
        public GraphicsBuffer SimulationTimeStateBuffer { get; }
        public GraphicsBuffer SurfaceSampleBuffer { get; }
        public GraphicsBuffer SurfaceVertexBuffer { get; }
        
        public int ParticleCount { get; }
        public int CellCount { get; }
        public int ScanGroupCount { get; }
        public int SurfaceVertexCount { get; }

        public SphSweGpuBuffers(int particleCount, int cellCount, int surfaceVertexCount)
        {
            if (particleCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(particleCount),
                    particleCount,
                    "Particle count must be greater than zero."
                );
            }

            if (cellCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cellCount),
                    cellCount,
                    "Cell count must be greater than zero."
                );
            }
            
            if (surfaceVertexCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(surfaceVertexCount),
                    surfaceVertexCount,
                    "Surface vertex count must be greater than zero."
                );
            }
            
            ParticleCount = particleCount;
            CellCount = cellCount;
            ScanGroupCount = (cellCount + ScanElementCount - 1) / ScanElementCount;
            SurfaceVertexCount = surfaceVertexCount;
            
            ParticleBuffer = CreateBuffer(particleCount, SphSweGpuParticle.Stride);
            ParticleCellIndexBuffer = CreateUIntBuffer(particleCount);
            CellParticleCountBuffer = CreateUIntBuffer(cellCount);
            CellParticleStartIndexBuffer = CreateUIntBuffer(cellCount);
            CellParticleCountSumByGroupBuffer = CreateUIntBuffer(ScanGroupCount);
            CellParticleCountStartIndexByGroupBuffer = CreateUIntBuffer(ScanGroupCount);
            CellParticleWriteIndexBuffer = CreateUIntBuffer(cellCount);
            SortedParticleIndexBuffer = CreateUIntBuffer(particleCount);
            MinimumTimeStepBitsBuffer = CreateUIntBuffer(1);
            SimulationTimeStateBuffer = CreateBuffer(1, SphSweGpuSimulationTimeState.Stride);
            SurfaceSampleBuffer = CreateBuffer(1, SphSweGpuSurfaceSample.Stride);
            SurfaceVertexBuffer = CreateBuffer(surfaceVertexCount, SphSweGpuSurfaceVertex.Stride);
        }
        
        private static GraphicsBuffer CreateUIntBuffer(int elementCount)
        {
            return CreateBuffer(elementCount, sizeof(uint));
        }

        private static GraphicsBuffer CreateBuffer(int elementCount, int stride)
        {
            return new GraphicsBuffer(GraphicsBuffer.Target.Structured, elementCount, stride);
        }
        
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            ParticleBuffer.Dispose();
            ParticleCellIndexBuffer.Dispose();
            CellParticleCountBuffer.Dispose();
            CellParticleStartIndexBuffer.Dispose();
            CellParticleCountSumByGroupBuffer.Dispose();
            CellParticleCountStartIndexByGroupBuffer.Dispose();
            CellParticleWriteIndexBuffer.Dispose();
            SortedParticleIndexBuffer.Dispose();
            MinimumTimeStepBitsBuffer.Dispose();
            SimulationTimeStateBuffer.Dispose();
            SurfaceSampleBuffer.Dispose();
            SurfaceVertexBuffer.Dispose();

            disposed = true;
        }
    }
}
