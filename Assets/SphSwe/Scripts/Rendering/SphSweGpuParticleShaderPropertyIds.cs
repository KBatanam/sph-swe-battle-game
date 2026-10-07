using UnityEngine;

namespace SphSwe.Rendering
{
    internal static class SphSweGpuParticleShaderPropertyIds
    {
        public static readonly int Particles = Shader.PropertyToID("_Particles");
        public static readonly int SimulationLocalToWorld = Shader.PropertyToID("_SimulationLocalToWorld");
        public static readonly int ParticleRadius = Shader.PropertyToID("_ParticleRadius");
        public static readonly int GroundHeight = Shader.PropertyToID("_GroundHeight");
        public static readonly int FluidDepthHeightScale = Shader.PropertyToID("_FluidDepthHeightScale");
    }
}
