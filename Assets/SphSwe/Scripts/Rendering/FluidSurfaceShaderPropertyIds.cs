using UnityEngine;

namespace SphSwe.Rendering
{
    internal static class FluidSurfaceShaderPropertyIds
    {
        public static readonly int SurfaceVertices = Shader.PropertyToID("_SurfaceVertices");
        public static readonly int GroundHeight = Shader.PropertyToID("_GroundHeight");
        public static readonly int FluidDepthHeightScale = Shader.PropertyToID("_FluidDepthHeightScale");
    }
}