using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// 2次元シミュレーション平面を正方形セルへ分割し、
    /// 指定位置周辺に存在する粒子インデックスを検索する。
    /// </summary>
    public sealed class SphSweSpatialGrid
    {
        private readonly Dictionary<Vector2Int, List<int>> particleIndicesByCell = new();
        private readonly Stack<List<int>> reusableParticleIndexLists = new();
        private float cellSize;
        private SphSweParticle[] particles = Array.Empty<SphSweParticle>();

        /// <summary>
        /// 現在の粒子位置から空間グリッドを再構築する。
        /// </summary>
        public void Rebuild(SphSweParticle[] newParticles, float newCellSize)
        {
            if (newParticles == null)
            {
                throw new ArgumentNullException(nameof(newParticles));
            }

            if (newCellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newCellSize),
                    newCellSize,
                    "セルサイズは0より大きい値である必要があります。"
                );
            }

            particles = newParticles;
            cellSize = newCellSize;

            ReleaseCurrentCellLists();

            for (var particleIndex = 0; particleIndex < particles.Length; particleIndex++)
            {
                var cellCoordinate = CalculateCellCoordinate(particles[particleIndex].Position);

                if (!particleIndicesByCell.TryGetValue(cellCoordinate, out var particleIndices))
                {
                    particleIndices = AcquireParticleIndexList();

                    particleIndicesByCell.Add(
                        cellCoordinate,
                        particleIndices
                    );
                }

                particleIndices.Add(particleIndex);
            }
        }

        /// <summary>
        /// 指定位置から有効半径内にある粒子インデックスを収集する。
        /// 探索位置と同じ位置にある粒子自身も近傍粒子に含まれる。
        /// </summary>
        public void CollectNeighborParticleIndices(
            Vector2 position,
            float effectiveRadius,
            List<int> neighborParticleIndices)
        {
            if (effectiveRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectiveRadius),
                    effectiveRadius,
                    "有効半径は0より大きい値である必要があります。"
                );
            }

            if (neighborParticleIndices == null)
            {
                throw new ArgumentNullException(nameof(neighborParticleIndices));
            }

            if (cellSize <= 0f)
            {
                throw new InvalidOperationException("空間グリッドが構築されていません。");
            }

            neighborParticleIndices.Clear();

            var centerCellCoordinate = CalculateCellCoordinate(position);

            var searchCellRange = Mathf.CeilToInt(effectiveRadius / cellSize);

            var squaredEffectiveRadius = effectiveRadius * effectiveRadius;

            for (var cellOffsetX = -searchCellRange; cellOffsetX <= searchCellRange; cellOffsetX++)
            {
                for (var cellOffsetZ = -searchCellRange; cellOffsetZ <= searchCellRange; cellOffsetZ++)
                {
                    var cellCoordinate = new Vector2Int(
                        centerCellCoordinate.x + cellOffsetX,
                        centerCellCoordinate.y + cellOffsetZ
                    );

                    if (!particleIndicesByCell.TryGetValue(cellCoordinate, out var particleIndices))
                    {
                        continue;
                    }

                    foreach (var particleIndex in particleIndices)
                    {
                        var positionDifference = position - particles[particleIndex].Position;

                        var squaredDistance = positionDifference.x * positionDifference.x
                                              + positionDifference.y * positionDifference.y;

                        if (squaredDistance <= squaredEffectiveRadius)
                        {
                            neighborParticleIndices.Add(particleIndex);
                        }
                    }
                }
            }
        }

        private Vector2Int CalculateCellCoordinate(Vector2 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.y / cellSize)
            );
        }

        private void ReleaseCurrentCellLists()
        {
            foreach (var particleIndices in particleIndicesByCell.Values)
            {
                particleIndices.Clear();
                reusableParticleIndexLists.Push(particleIndices);
            }

            particleIndicesByCell.Clear();
        }

        private List<int> AcquireParticleIndexList()
        {
            if (reusableParticleIndexLists.Count > 0)
            {
                return reusableParticleIndexLists.Pop();
            }

            return new List<int>(16);
        }
    }
}
