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

            var squaredRadius = effectiveRadius * effectiveRadius;
            
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
        
        /// <summary>
        /// 2次元Spikyカーネルの勾配を計算する。
        /// </summary>
        /// <param name="positionDifference">
        /// 対象粒子から近傍粒子を引いた位置差。
        /// </param>
        /// <param name="effectiveRadius">
        /// カーネルの有効半径。
        /// </param>
        public static Vector2 EvaluateSpikyGradient(
            Vector2 positionDifference,
            float effectiveRadius)
        {
            if (effectiveRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectiveRadius),
                    effectiveRadius,
                    "Effective radius must be greater than zero."
                );
            }

            var differenceX = positionDifference.x;
            var differenceZ = positionDifference.y;

            var squaredDistance = differenceX * differenceX + differenceZ * differenceZ;

            if (squaredDistance <= 0f)
            {
                return Vector2.zero;
            }

            var squaredRadius = effectiveRadius * effectiveRadius;

            if (squaredDistance >= squaredRadius)
            {
                return Vector2.zero;
            }

            var distance = Mathf.Sqrt(squaredDistance);
            var radiusDifference = effectiveRadius - distance;

            var radiusSquared = effectiveRadius * effectiveRadius;
            var radiusFourthPower = radiusSquared * radiusSquared;
            var radiusFifthPower = radiusFourthPower * effectiveRadius;

            var coefficient = -30f / (Mathf.PI * radiusFifthPower);

            var gradientScale =
                coefficient
                * radiusDifference
                * radiusDifference
                / distance;

            return positionDifference * gradientScale;
        }
        
        /// <summary>
        /// 2次元Viscosityカーネルのラプラシアンを計算する。
        /// </summary>
        public static float EvaluateViscosityLaplacian(float squaredDistance, float effectiveRadius)
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

            var squaredRadius = effectiveRadius * effectiveRadius;

            if (squaredDistance >= squaredRadius)
            {
                return 0f;
            }

            var distance = Mathf.Sqrt(squaredDistance);
            var radiusSquared = effectiveRadius * effectiveRadius;
            var radiusFourthPower = radiusSquared * radiusSquared;
            var radiusFifthPower = radiusFourthPower * effectiveRadius;
            var coefficient = 20f / (3f * Mathf.PI * radiusFifthPower);

            return coefficient * (effectiveRadius - distance);
        }
    }
}