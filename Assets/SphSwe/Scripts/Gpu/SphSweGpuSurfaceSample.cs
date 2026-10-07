using System.Runtime.InteropServices;
using UnityEngine;

namespace SphSwe.Gpu
{
    /// <summary>
    /// 指定されたシミュレーション座標における、
    /// 補間済みの流体速度と流体深さ。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SphSweGpuSurfaceSample
    {
        /// <summary>
        /// Vector2が8バイト、floatとuintがそれぞれ4バイトで、
        /// 構造体一要素当たり合計16バイトとなる。
        /// </summary>
        public const int Stride = 16;

        public Vector2 FluidVelocity;
        public float FluidDepth;
        public uint IsValid;
    }
}
