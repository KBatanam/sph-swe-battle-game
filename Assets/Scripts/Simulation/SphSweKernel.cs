using System;
using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// SPH-SWEで使用するカーネル関数を提供する。
    /// </summary>
    public static class SphSweKernel
    {
        /// <summary>
        /// 2次元Poly6カーネルの値を計算する。
        /// </summary>
        /// <param name="squaredDistance">
        /// 2粒子間の距離の二乗。
        /// </param>
        /// <param name="effectiveRadius">
        /// カーネルの有効半径。
        /// </param>
        public static float EvaluatePoly6(float squaredDistance, float effectiveRadius)
        {
            if (squaredDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(squaredDistance),
                    squaredDistance,
                    "Squared distance must be greater than or equal to zero."
                );
            }

            if (effectiveRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectiveRadius),
                    effectiveRadius,
                    "Effective radius must be greater than zero."
                );
            }

            var squaredRadius =
                effectiveRadius * effectiveRadius;
            
            if (squaredDistance >= squaredRadius)
            {
                return 0f;
            }

            var radiusFourthPower = squaredRadius * squaredRadius;
            var radiusEighthPower = radiusFourthPower * radiusFourthPower;
            var coefficient = 4f / (Mathf.PI * radiusEighthPower);
            var radiusDifference = squaredRadius - squaredDistance;

            return coefficient
                   * radiusDifference
                   * radiusDifference
                   * radiusDifference;
        }
    }
}