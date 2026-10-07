using System;
using System.Collections.Generic;
using SphSwe.Core;
using UnityEngine;

namespace SphSwe.Simulation
{
    /// <summary>
    /// 2次元シミュレーション平面を正方形セルへ分割し、
    /// 連続配列から指定位置周辺の粒子インデックスを検索する。
    /// </summary>
    public sealed class SphSweSpatialGrid
    {
        private float cellSize;
        private Vector2Int minimumCellCoordinate;
        private int cellCountX;
        private int cellCountZ;
        private SphSweParticle[] particles = Array.Empty<SphSweParticle>();
        private Vector2Int[] particleCellCoordinates = Array.Empty<Vector2Int>();
        private int[] particleCountsByCell = Array.Empty<int>();
        private int[] particleStartIndicesByCell = Array.Empty<int>();
        private int[] nextParticleWriteIndicesByCell = Array.Empty<int>();
        private int[] sortedParticleIndices = Array.Empty<int>();

        /// <summary>
        /// 現在の粒子位置から各セルの粒子数と開始位置を計算し、
        /// 粒子インデックスをセル単位で連続するように並べ直す。
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

            if (particles.Length == 0)
            {
                cellCountX = 0;
                cellCountZ = 0;
                return;
            }

            EnsureParticleArrayLengthsMatchParticleCount();
            CalculateParticleCellCoordinatesAndGridBounds();
            EnsureCellArrayLengthsMatchCellCount();
            CountParticlesByCell();
            CalculateParticleStartIndices();
            SortParticleIndicesByCell();
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

            if (particles.Length == 0)
            {
                return;
            }

            var centerCellCoordinate = CalculateCellCoordinate(position);
            var searchCellRange = Mathf.CeilToInt(effectiveRadius / cellSize);
            var squaredEffectiveRadius = effectiveRadius * effectiveRadius;

            for (var cellOffsetX = -searchCellRange; cellOffsetX <= searchCellRange; cellOffsetX++)
            {
                var cellCoordinateX = centerCellCoordinate.x + cellOffsetX;

                if (cellCoordinateX < minimumCellCoordinate.x
                    || cellCoordinateX >= minimumCellCoordinate.x + cellCountX)
                {
                    continue;
                }

                for (var cellOffsetZ = -searchCellRange; cellOffsetZ <= searchCellRange; cellOffsetZ++)
                {
                    var cellCoordinateZ = centerCellCoordinate.y + cellOffsetZ;

                    if (cellCoordinateZ < minimumCellCoordinate.y
                        || cellCoordinateZ >= minimumCellCoordinate.y + cellCountZ)
                    {
                        continue;
                    }

                    var cellIndex = CalculateCellIndex(cellCoordinateX, cellCoordinateZ);
                    var particleStartIndex = particleStartIndicesByCell[cellIndex];
                    var particleEndIndex = particleStartIndex + particleCountsByCell[cellIndex];

                    for (var sortedParticleIndex = particleStartIndex;
                         sortedParticleIndex < particleEndIndex;
                         sortedParticleIndex++)
                    {
                        var particleIndex = sortedParticleIndices[sortedParticleIndex];
                        var positionDifference = position - particles[particleIndex].Position;
                        var squaredDistance =
                            positionDifference.x * positionDifference.x
                            + positionDifference.y * positionDifference.y;

                        if (squaredDistance <= squaredEffectiveRadius)
                        {
                            neighborParticleIndices.Add(particleIndex);
                        }
                    }
                }
            }
        }

        private void EnsureParticleArrayLengthsMatchParticleCount()
        {
            if (particleCellCoordinates.Length != particles.Length)
            {
                particleCellCoordinates = new Vector2Int[particles.Length];
            }

            if (sortedParticleIndices.Length != particles.Length)
            {
                sortedParticleIndices = new int[particles.Length];
            }
        }

        private void CalculateParticleCellCoordinatesAndGridBounds()
        {
            var firstCellCoordinate = CalculateCellCoordinate(particles[0].Position);
            var minimumCellCoordinateX = firstCellCoordinate.x;
            var minimumCellCoordinateZ = firstCellCoordinate.y;
            var maximumCellCoordinateX = firstCellCoordinate.x;
            var maximumCellCoordinateZ = firstCellCoordinate.y;

            particleCellCoordinates[0] = firstCellCoordinate;

            for (var particleIndex = 1; particleIndex < particles.Length; particleIndex++)
            {
                var cellCoordinate = CalculateCellCoordinate(particles[particleIndex].Position);
                particleCellCoordinates[particleIndex] = cellCoordinate;

                minimumCellCoordinateX = Mathf.Min(minimumCellCoordinateX, cellCoordinate.x);
                minimumCellCoordinateZ = Mathf.Min(minimumCellCoordinateZ, cellCoordinate.y);
                maximumCellCoordinateX = Mathf.Max(maximumCellCoordinateX, cellCoordinate.x);
                maximumCellCoordinateZ = Mathf.Max(maximumCellCoordinateZ, cellCoordinate.y);
            }

            minimumCellCoordinate = new Vector2Int(
                minimumCellCoordinateX,
                minimumCellCoordinateZ
            );

            cellCountX = maximumCellCoordinateX - minimumCellCoordinateX + 1;
            cellCountZ = maximumCellCoordinateZ - minimumCellCoordinateZ + 1;
        }

        private void EnsureCellArrayLengthsMatchCellCount()
        {
            var totalCellCount = checked(cellCountX * cellCountZ);

            if (particleCountsByCell.Length == totalCellCount)
            {
                Array.Clear(particleCountsByCell, 0, totalCellCount);
                return;
            }

            particleCountsByCell = new int[totalCellCount];
            particleStartIndicesByCell = new int[totalCellCount];
            nextParticleWriteIndicesByCell = new int[totalCellCount];
        }

        private void CountParticlesByCell()
        {
            foreach (var cellCoordinate in particleCellCoordinates)
            {
                var cellIndex = CalculateCellIndex(cellCoordinate.x, cellCoordinate.y);
                particleCountsByCell[cellIndex]++;
            }
        }

        private void CalculateParticleStartIndices()
        {
            var nextParticleStartIndex = 0;

            for (var cellIndex = 0; cellIndex < particleCountsByCell.Length; cellIndex++)
            {
                particleStartIndicesByCell[cellIndex] = nextParticleStartIndex;
                nextParticleWriteIndicesByCell[cellIndex] = nextParticleStartIndex;
                nextParticleStartIndex += particleCountsByCell[cellIndex];
            }
        }

        private void SortParticleIndicesByCell()
        {
            for (var particleIndex = 0; particleIndex < particleCellCoordinates.Length; particleIndex++)
            {
                var cellCoordinate = particleCellCoordinates[particleIndex];
                var cellIndex = CalculateCellIndex(cellCoordinate.x, cellCoordinate.y);
                var particleWriteIndex = nextParticleWriteIndicesByCell[cellIndex];

                sortedParticleIndices[particleWriteIndex] = particleIndex;
                nextParticleWriteIndicesByCell[cellIndex]++;
            }
        }

        private Vector2Int CalculateCellCoordinate(Vector2 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.y / cellSize)
            );
        }

        private int CalculateCellIndex(int cellCoordinateX, int cellCoordinateZ)
        {
            var localCellCoordinateX = cellCoordinateX - minimumCellCoordinate.x;
            var localCellCoordinateZ = cellCoordinateZ - minimumCellCoordinate.y;

            return localCellCoordinateZ * cellCountX + localCellCoordinateX;
        }
    }
}
