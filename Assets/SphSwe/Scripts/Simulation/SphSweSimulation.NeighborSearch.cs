using System;
using System.Collections.Generic;
using UnityEngine;

namespace SphSwe.Simulation
{
    public sealed partial class SphSweSimulation
    {
        private readonly SphSweSpatialGrid spatialGrid = new();

        private List<int>[] neighborParticleIndicesByParticle = Array.Empty<List<int>>();

        /// <summary>
        /// 現在の粒子位置に基づいて、空間グリッドのセルへの粒子登録を更新し、
        /// 全粒子について有効半径内の近傍粒子を検索する。
        /// 検索結果は密度計算と加速度計算で共通して使用する。
        /// </summary>
        private void RebuildNeighborParticleIndices()
        {
            using var profilingScope = NeighborSearchProfilerMarker.Auto();

            if (particles == null || particles.Length == 0)
            {
                return;
            }

            EnsureNeighborParticleIndexListsMatchParticleCount();

            using (SpatialGridRebuildProfilerMarker.Auto())
            {
                spatialGrid.Rebuild(particles, effectiveRadius);
            }

            using (NeighborCollectionProfilerMarker.Auto())
            {
                for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
                {
                    ref var particle = ref particles[particleIndex];

                    spatialGrid.CollectNeighborParticleIndices(
                        particle.Position,
                        particle.EffectiveRadius,
                        neighborParticleIndicesByParticle[particleIndex]
                    );
                }
            }
        }

        /// <summary>
        /// 全粒子がそれぞれ近傍探索結果を保持できるように、
        /// 現在の粒子数と同じ数の近傍インデックスリストを確保する。
        /// 粒子数が変わらない場合は、既存のリストを再利用する。
        /// </summary>
        private void EnsureNeighborParticleIndexListsMatchParticleCount()
        {
            if (neighborParticleIndicesByParticle.Length == particles.Length)
            {
                return;
            }

            neighborParticleIndicesByParticle = new List<int>[particles.Length];

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                neighborParticleIndicesByParticle[particleIndex] = new List<int>(16);
            }
        }
        
        /// <summary>
        /// 空間グリッドから構築した近傍リストを使用して、
        /// 各粒子の密度相当量と流体深さを計算する。
        /// </summary>
        private void CalculateDensitiesUsingSpatialGrid()
        {
            using var profilingScope = DensityCalculationProfilerMarker.Auto();

            if (particles == null || particles.Length == 0)
            {
                return;
            }

            RebuildNeighborParticleIndices();

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                ref var particle = ref particles[particleIndex];
                var density = 0f;

                var neighborParticleIndices = neighborParticleIndicesByParticle[particleIndex];

                foreach (var neighborParticleIndex in neighborParticleIndices)
                {
                    ref var neighbor = ref particles[neighborParticleIndex];

                    var differenceX = particle.Position.x - neighbor.Position.x;

                    var differenceZ = particle.Position.y - neighbor.Position.y;

                    var squaredDistance = differenceX * differenceX + differenceZ * differenceZ;

                    var kernelValue = SphSweKernel.EvaluatePoly6(
                        squaredDistance,
                        particle.EffectiveRadius
                    );

                    density += neighbor.Mass * kernelValue;
                }

                particle.Density = density;
                particle.FluidDepth = Mathf.Max(0f, density / referenceDensity);
            }
        }
    }
}
