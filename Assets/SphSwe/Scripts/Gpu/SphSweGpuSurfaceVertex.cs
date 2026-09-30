using System.Runtime.InteropServices;
using UnityEngine;

namespace SphSwe.Gpu
{
    /// <summary>
    /// 描画用水面メッシュの頂点一つ分について、
    /// 粒子から補間した水深と水面法線を保持する。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SphSweGpuSurfaceVertex
    {
        /// <summary>
        /// floatが1個で4バイト、
        /// Vector3が1個で12バイト、合計16バイトとなる。
        /// </summary>
        public const int Stride = 16;

        public float FluidDepth;
        public Vector3 Normal;
    }
}