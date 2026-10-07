#ifndef SPH_SWE_GPU_SURFACE_VERTEX_INCLUDED
#define SPH_SWE_GPU_SURFACE_VERTEX_INCLUDED

/// <summary>
/// 描画用水面メッシュの頂点一つ分について、
/// 粒子から補間した水深と水面法線を保持する。
///
/// C#側のSphSweGpuSurfaceVertexと同じ順序および
/// 合計16バイトになるように定義する。
/// </summary>
struct SphSweGpuSurfaceVertex
{
    float FluidDepth;
    float3 Normal;
};

#endif