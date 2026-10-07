using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace SphSwe.Rendering
{
    /// <summary>
    /// GPUで水面高さを変位させるための固定グリッドメッシュを生成する。
    /// このMesh自体の頂点高さは更新せず、描画時にGPU頂点バッファから取得する。
    /// </summary>
    internal static class FluidSurfaceMeshFactory
    {
        public static Mesh Create(
            Vector2 simulationCenter,
            Vector2 simulationAreaSize,
            int vertexCountX,
            int vertexCountZ)
        {
            if (vertexCountX < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(vertexCountX),
                    vertexCountX,
                    "X vertex count must be at least two."
                );
            }

            if (vertexCountZ < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(vertexCountZ),
                    vertexCountZ,
                    "Z vertex count must be at least two."
                );
            }

            var vertexCount = checked(vertexCountX * vertexCountZ);
            var quadCount = checked((vertexCountX - 1) * (vertexCountZ - 1));

            var vertices = new Vector3[vertexCount];
            var textureCoordinates = new Vector2[vertexCount];
            var triangleIndices = new int[checked(quadCount * 6)];

            CreateVertices(
                simulationCenter,
                simulationAreaSize,
                vertexCountX,
                vertexCountZ,
                vertices,
                textureCoordinates
            );

            CreateTriangleIndices(
                vertexCountX,
                vertexCountZ,
                triangleIndices
            );

            var mesh = new Mesh
            {
                name = "Fluid Surface Mesh",
                indexFormat = vertexCount > ushort.MaxValue
                    ? IndexFormat.UInt32
                    : IndexFormat.UInt16
            };

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, textureCoordinates);
            mesh.SetIndices(
                triangleIndices,
                MeshTopology.Triangles,
                0
            );
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);

            return mesh;
        }

        private static void CreateVertices(
            Vector2 simulationCenter,
            Vector2 simulationAreaSize,
            int vertexCountX,
            int vertexCountZ,
            Vector3[] vertices,
            Vector2[] textureCoordinates)
        {
            var simulationMinimumPosition = simulationCenter - simulationAreaSize * 0.5f;

            for (var z = 0; z < vertexCountZ; z++)
            {
                var normalizedZ = (float)z / (vertexCountZ - 1);
                var simulationPositionZ = simulationMinimumPosition.y + simulationAreaSize.y * normalizedZ;

                for (var x = 0; x < vertexCountX; x++)
                {
                    var normalizedX = (float)x / (vertexCountX - 1);
                    var simulationPositionX = simulationMinimumPosition.x + simulationAreaSize.x * normalizedX;
                    var vertexIndex = z * vertexCountX + x;

                    vertices[vertexIndex] = new Vector3(
                        simulationPositionX,
                        0f,
                        simulationPositionZ
                    );

                    textureCoordinates[vertexIndex] = new Vector2(normalizedX, normalizedZ);
                }
            }
        }

        private static void CreateTriangleIndices(
            int vertexCountX,
            int vertexCountZ,
            int[] triangleIndices)
        {
            var triangleIndex = 0;

            for (var z = 0; z < vertexCountZ - 1; z++)
            {
                for (var x = 0; x < vertexCountX - 1; x++)
                {
                    var bottomLeftVertexIndex =
                        z * vertexCountX + x;

                    var bottomRightVertexIndex =
                        bottomLeftVertexIndex + 1;

                    var topLeftVertexIndex =
                        bottomLeftVertexIndex + vertexCountX;

                    var topRightVertexIndex =
                        topLeftVertexIndex + 1;

                    triangleIndices[triangleIndex++] =
                        bottomLeftVertexIndex;

                    triangleIndices[triangleIndex++] =
                        topLeftVertexIndex;

                    triangleIndices[triangleIndex++] =
                        bottomRightVertexIndex;

                    triangleIndices[triangleIndex++] =
                        bottomRightVertexIndex;

                    triangleIndices[triangleIndex++] =
                        topLeftVertexIndex;

                    triangleIndices[triangleIndex++] =
                        topRightVertexIndex;
                }
            }
        }
    }
}