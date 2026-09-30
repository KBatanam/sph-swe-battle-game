using System;
using SphSwe.Gpu;
using SphSwe.Simulation;
using SphSwe.Validation;
using UnityEngine;

namespace SphSwe.Rendering
{
    [DisallowMultipleComponent]
    public sealed class FluidSurfaceRenderer : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweGpuSimulation gpuSimulation;

        [SerializeField, Required]
        private Material surfaceMaterialTemplate;

        [Header("Rendering")]

        [SerializeField]
        private float groundHeight;

        [SerializeField, Min(0.01f)]
        private float fluidDepthHeightScale = 1f;

        [SerializeField, Min(0.01f)]
        private float renderingBoundsHeight = 10f;

        private Material surfaceMaterial;
        private Mesh surfaceMesh;
        private int currentVertexCountX;
        private int currentVertexCountZ;

        private void Awake()
        {
            ValidateReferences();

            surfaceMaterial = new Material(surfaceMaterialTemplate)
            {
                name = $"{surfaceMaterialTemplate.name} (Runtime)"
            };

            surfaceMaterial.EnableKeyword(FluidSurfaceShaderKeywords.SurfaceVertexBufferAvailable);
        }

        private void LateUpdate()
        {
            if (surfaceMaterial == null)
            {
                return;
            }

            if (!gpuSimulation.TryGetSurfaceVertexBuffer(
                    out var surfaceVertexBuffer,
                    out var vertexCountX,
                    out var vertexCountZ))
            {
                return;
            }

            EnsureSurfaceMesh(vertexCountX, vertexCountZ);

            surfaceMaterial.SetBuffer(FluidSurfaceShaderPropertyIds.SurfaceVertices, surfaceVertexBuffer);
            surfaceMaterial.SetFloat(FluidSurfaceShaderPropertyIds.GroundHeight, groundHeight);
            surfaceMaterial.SetFloat(FluidSurfaceShaderPropertyIds.FluidDepthHeightScale, fluidDepthHeightScale);

            var sourceSimulation = gpuSimulation.SourceSimulation;
            var renderParams = new RenderParams(surfaceMaterial)
            {
                worldBounds = CalculateRenderingBounds(sourceSimulation),
                layer = gameObject.layer,
                entityId = gameObject.GetEntityId()
            };

            Graphics.RenderMesh(
                renderParams,
                surfaceMesh,
                0,
                sourceSimulation.transform.localToWorldMatrix
            );
        }

        private void EnsureSurfaceMesh(int vertexCountX, int vertexCountZ)
        {
            if (surfaceMesh != null && currentVertexCountX == vertexCountX && currentVertexCountZ == vertexCountZ)
            {
                return;
            }

            DestroySurfaceMesh();

            var sourceSimulation = gpuSimulation.SourceSimulation;

            surfaceMesh = FluidSurfaceMeshFactory.Create(
                sourceSimulation.SimulationCenter,
                sourceSimulation.SimulationAreaSize,
                vertexCountX,
                vertexCountZ
            );

            currentVertexCountX = vertexCountX;
            currentVertexCountZ = vertexCountZ;
        }

        private Bounds CalculateRenderingBounds(
            SphSweSimulation sourceSimulation)
        {
            var simulationCenter = sourceSimulation.SimulationCenter;
            var simulationAreaSize = sourceSimulation.SimulationAreaSize;

            var localBoundsCenter = new Vector3(
                simulationCenter.x,
                groundHeight + renderingBoundsHeight * 0.5f,
                simulationCenter.y
            );

            var localBoundsSize = new Vector3(
                simulationAreaSize.x,
                renderingBoundsHeight,
                simulationAreaSize.y
            );

            return new Bounds(
                sourceSimulation.transform.TransformPoint(localBoundsCenter),
                Vector3.Scale(localBoundsSize, sourceSimulation.transform.lossyScale)
            );
        }

        private void OnDestroy()
        {
            DestroySurfaceMesh();

            if (surfaceMaterial == null)
            {
                return;
            }

            Destroy(surfaceMaterial);
            surfaceMaterial = null;
        }

        private void DestroySurfaceMesh()
        {
            if (surfaceMesh == null)
            {
                return;
            }

            Destroy(surfaceMesh);
            surfaceMesh = null;
            currentVertexCountX = 0;
            currentVertexCountZ = 0;
        }

        private void ValidateReferences()
        {
            if (gpuSimulation == null)
            {
                throw new InvalidOperationException("GPU simulation is not assigned.");
            }

            if (surfaceMaterialTemplate == null)
            {
                throw new InvalidOperationException("Surface material template is not assigned.");
            }
        }
    }
}