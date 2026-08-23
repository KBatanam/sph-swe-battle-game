using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Simulation
{
    public sealed partial class SphSweSimulation
    {
        /// <summary>
        /// 壁付近で不足する近傍粒子を補い、
        /// 密度、流体深さ勾配および粘性の計算に矩形境界の影響を反映するため、
        /// シミュレーション領域の外側へ固定された境界粒子を複数層生成する。
        /// </summary>
        private void AddBoundaryParticles(List<SphSweParticle> generatedParticles)
        {
            if (!boundaryParticleGenerationEnabled)
            {
                return;
            }

            var halfSimulationAreaSize = simulationAreaSize * 0.5f;
            var simulationMinimumPosition = simulationCenter - halfSimulationAreaSize;
            var simulationMaximumPosition = simulationCenter + halfSimulationAreaSize;
            var boundaryParticleSpacing = particleSpacing * boundaryParticleSpacingScale;

            for (var layerIndex = 0; layerIndex < boundaryParticleLayerCount; layerIndex++)
            {
                var boundaryOffset = particleSpacing * (layerIndex + 0.5f);
                var layerMinimumPosition = simulationMinimumPosition - Vector2.one * boundaryOffset;
                var layerMaximumPosition = simulationMaximumPosition + Vector2.one * boundaryOffset;

                AddRectangularBoundaryLayer(
                    generatedParticles,
                    layerMinimumPosition,
                    layerMaximumPosition,
                    boundaryParticleSpacing
                );
            }
        }

        private void AddRectangularBoundaryLayer(
            List<SphSweParticle> generatedParticles,
            Vector2 minimumPosition,
            Vector2 maximumPosition,
            float boundaryParticleSpacing)
        {
            var boundaryWidth = maximumPosition.x - minimumPosition.x;
            var boundaryDepth = maximumPosition.y - minimumPosition.y;
            
            var particleCountAlongX = Mathf.CeilToInt(boundaryWidth / boundaryParticleSpacing) + 1;
            var particleCountAlongZ = Mathf.CeilToInt(boundaryDepth / boundaryParticleSpacing) + 1;

            for (var xIndex = 0; xIndex < particleCountAlongX; xIndex++)
            {
                var interpolationRate = (float)xIndex / (particleCountAlongX - 1);

                var positionX = Mathf.Lerp(
                    minimumPosition.x,
                    maximumPosition.x,
                    interpolationRate
                );

                generatedParticles.Add(CreateBoundaryParticle(new Vector2(positionX, minimumPosition.y)));
                generatedParticles.Add(CreateBoundaryParticle(new Vector2(positionX, maximumPosition.y)));
            }

            // 四隅はX方向のループで生成済みなので除外する。
            for (var zIndex = 1; zIndex < particleCountAlongZ - 1; zIndex++)
            {
                var interpolationRate = (float)zIndex / (particleCountAlongZ - 1);

                var positionZ = Mathf.Lerp(minimumPosition.y, maximumPosition.y, interpolationRate);

                generatedParticles.Add(CreateBoundaryParticle(new Vector2(minimumPosition.x, positionZ)));
                generatedParticles.Add(CreateBoundaryParticle(new Vector2(maximumPosition.x, positionZ)));
            }
        }

        private SphSweParticle CreateBoundaryParticle(Vector2 position)
        {
            return new SphSweParticle(
                position,
                particleMass,
                effectiveRadius,
                SphSweParticleType.Boundary
            );
        }
    }
}