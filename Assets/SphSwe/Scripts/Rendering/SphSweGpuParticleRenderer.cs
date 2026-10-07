using System;
using SphSwe.Gpu;
using UnityEngine;
using SphSwe.Validation;
using SphSwe.Simulation;

namespace SphSwe.Rendering
{
    [DisallowMultipleComponent]
    public sealed class SphSweGpuParticleRenderer : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweGpuSimulation gpuSimulation;

        [SerializeField, Required]
        private Material particleMaterialTemplate;

        [Header("Rendering")]

        [SerializeField, Min(0.01f)]
        private float particleRenderingRadiusScale = 0.45f;

        [SerializeField]
        private float groundHeight;

        [SerializeField, Min(0.01f)]
        private float fluidDepthHeightScale = 1f;

        [SerializeField, Min(0.01f)]
        private float renderingBoundsHeight = 10f;
        
        private Material particleMaterial;
        private const int VertexCountPerParticle = 6;

        private void Awake()
        {
            ValidateReferences();

            particleMaterial = new Material(particleMaterialTemplate)
            {
                name = $"{particleMaterialTemplate.name} (Runtime)"
            };
            
            particleMaterial.EnableKeyword(SphSweGpuParticleShaderKeywords.ParticleBufferAvailable);
        }

        private void OnDestroy()
        {
            if (particleMaterial == null)
            {
                return;
            }

            Destroy(particleMaterial);
            particleMaterial = null;
        }
        
        private void LateUpdate()
        {
            if (particleMaterial == null)
            {
                return;
            }

            if (!gpuSimulation.TryGetParticleBuffer(out var particleBuffer, out var particleCount))
            {
                return;
            }

            var sourceSimulation = gpuSimulation.SourceSimulation;
            var particleRadius = sourceSimulation.ParticleSpacing * particleRenderingRadiusScale;

            particleMaterial.SetBuffer(SphSweGpuParticleShaderPropertyIds.Particles, particleBuffer);
            particleMaterial.SetMatrix(
                SphSweGpuParticleShaderPropertyIds.SimulationLocalToWorld,
                sourceSimulation.transform.localToWorldMatrix
            );
            particleMaterial.SetFloat(SphSweGpuParticleShaderPropertyIds.ParticleRadius, particleRadius);
            particleMaterial.SetFloat(SphSweGpuParticleShaderPropertyIds.GroundHeight, groundHeight);
            particleMaterial.SetFloat(
                SphSweGpuParticleShaderPropertyIds.FluidDepthHeightScale,
                fluidDepthHeightScale
            );

            var renderParams = new RenderParams(particleMaterial)
            {
                worldBounds = CalculateRenderingBounds(sourceSimulation),
                layer = gameObject.layer,
                entityId = gameObject.GetEntityId()
            };

            Graphics.RenderPrimitives(
                renderParams,
                MeshTopology.Triangles,
                VertexCountPerParticle,
                particleCount
            );
        }
        
        private Bounds CalculateRenderingBounds(SphSweSimulation sourceSimulation)
        {
            var simulationCenter = sourceSimulation.SimulationCenter;
            var simulationAreaSize = sourceSimulation.SimulationAreaSize;
            var particleRadius = sourceSimulation.ParticleSpacing * particleRenderingRadiusScale;

            var localBoundsCenter = new Vector3(
                simulationCenter.x,
                groundHeight + renderingBoundsHeight * 0.5f,
                simulationCenter.y
            );

            var localBoundsSize = new Vector3(
                simulationAreaSize.x + particleRadius * 2f,
                renderingBoundsHeight,
                simulationAreaSize.y + particleRadius * 2f
            );

            return new Bounds(
                sourceSimulation.transform.TransformPoint(localBoundsCenter),
                Vector3.Scale(localBoundsSize, sourceSimulation.transform.lossyScale)
            );
        }

        private void ValidateReferences()
        {
            if (gpuSimulation == null)
            {
                throw new InvalidOperationException(
                    "GPU simulation is not assigned."
                );
            }

            if (particleMaterialTemplate == null)
            {
                throw new InvalidOperationException(
                    "Particle material template is not assigned."
                );
            }
        }
    }
}
