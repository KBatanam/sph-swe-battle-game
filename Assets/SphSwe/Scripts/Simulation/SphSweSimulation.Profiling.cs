using Unity.Profiling;

namespace SphSwe.Simulation
{
    public sealed partial class SphSweSimulation
    {
        private static readonly ProfilerMarker SimulationUpdateProfilerMarker = new("SPH-SWE.Update");
        private static readonly ProfilerMarker AdaptiveStepProfilerMarker = new("SPH-SWE.SimulateAdaptiveStep");
        private static readonly ProfilerMarker DensityCalculationProfilerMarker = new("SPH-SWE.CalculateDensities");
        private static readonly ProfilerMarker NeighborSearchProfilerMarker = new("SPH-SWE.RebuildNeighborParticleIndices");
        private static readonly ProfilerMarker SpatialGridRebuildProfilerMarker = new("SPH-SWE.RebuildSpatialGrid");
        private static readonly ProfilerMarker NeighborCollectionProfilerMarker = new("SPH-SWE.CollectNeighbors");
        private static readonly ProfilerMarker AccelerationCalculationProfilerMarker = new("SPH-SWE.CalculateAccelerations");
        private static readonly ProfilerMarker FluidDepthGradientProfilerMarker = new("SPH-SWE.FluidDepthGradient");
        private static readonly ProfilerMarker ViscosityProfilerMarker = new("SPH-SWE.Viscosity");
        private static readonly ProfilerMarker IntegrationProfilerMarker = new("SPH-SWE.IntegrateParticles");
        private static readonly ProfilerMarker GizmoDrawingProfilerMarker = new("SPH-SWE.DrawGizmos");
    }
}
