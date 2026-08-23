using System;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace Core
{
    /// <summary>
    /// SPH-SWEシミュレーションで使用する1粒子分のデータ。
    /// Position、Velocity、AccelerationはUnityのX-Z平面に対応する。
    /// </summary>
    [Serializable]
    public struct SphSweParticle
    {
        /// <summary>
        /// シミュレーション平面上の位置。
        /// xはUnityのX、yはUnityのZに対応する。
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// シミュレーション平面上の速度。
        /// </summary>
        public Vector2 Velocity;

        /// <summary>
        /// シミュレーション平面上の加速度。
        /// </summary>
        public Vector2 Acceleration;
        
        public float Density;
        
        public float Mass;

        /// <summary>
        /// シミュレーション開始時の粒子質量。
        /// 波の生成などでMassを変更した後、元へ戻すために使用する。
        /// </summary>
        public float InitialMass;

        /// <summary>
        /// 近傍粒子を検索する有効半径。
        /// </summary>
        public float EffectiveRadius;
        
        /// <summary>
        /// 密度相当量から計算された流体深さ。
        /// </summary>
        public float FluidDepth;
        
        public SphSweParticleType Type;

        public SphSweParticle(
            Vector2 position,
            float mass,
            float effectiveRadius,
            SphSweParticleType type)
        {
            Position = position;
            Velocity = Vector2.zero;
            Acceleration = Vector2.zero;
            Density = 0f;
            Mass = mass;
            InitialMass = mass;
            EffectiveRadius = effectiveRadius;
            FluidDepth = 0f;
            Type = type;
        }
    }
}