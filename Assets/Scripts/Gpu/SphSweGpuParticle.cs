using System.Runtime.InteropServices;
using Core;
using UnityEngine;

namespace Gpu
{
    /// <summary>
    /// CPUとGPUの間で転送するSPH-SWE粒子データ。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SphSweGpuParticle
    {
        /// <summary>
        /// 構造体一要素当たりのバイト数。
        /// Vector2が3個 (8*3)、floatが5個 (4*5)、intが1個 (4*1)で合計48バイトとなる。
        /// GraphicsBufferのstrideとして使用する。
        /// </summary>
        public const int Stride = 48;

        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Acceleration;

        public float Density;
        public float Mass;
        public float InitialMass;
        public float EffectiveRadius;
        public float FluidDepth;

        public int Type;

        public SphSweGpuParticle(SphSweParticle particle)
        {
            Position = particle.Position;
            Velocity = particle.Velocity;
            Acceleration = particle.Acceleration;

            Density = particle.Density;
            Mass = particle.Mass;
            InitialMass = particle.InitialMass;
            EffectiveRadius = particle.EffectiveRadius;
            FluidDepth = particle.FluidDepth;

            Type = (int)particle.Type;
        }
    }
}
