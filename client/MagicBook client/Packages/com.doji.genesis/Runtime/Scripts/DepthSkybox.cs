using System;
using Unity.Collections;
using UnityEngine;

namespace Genesis {

    public class DepthSkybox : MonoBehaviour {

        private void Awake() {
            // "Disable" frustum culling because we extrude vertices in the shader.
            // The sphere object would be erroneously frustum culled otherwise
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.sharedMesh.bounds = new Bounds(transform.position, Vector3.one * 100000f);
        }

        public GameObject ExtractMesh()
        {
            Mesh mesh = CreateMesh();
            GameObject g = new GameObject();
            g.name = $"{gameObject.name} (Mesh)";
            var mf = g.AddComponent<MeshFilter>();
            var mr = g.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            Material mat = new Material(Shader.Find("Custom/UnlitTexture"));
            mat.mainTexture = (Texture2D)gameObject.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex");

            mr.sharedMaterial = mat;
            mf.sharedMesh = mesh;

            float rotationY = gameObject.GetComponent<MeshRenderer>().sharedMaterial.GetFloat("_Rotation");
            g.transform.localEulerAngles = new Vector3(0, rotationY, 0);
            return g;
        }

        public Mesh CreateMesh()
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();

            if (meshFilter == null || meshRenderer == null)
            {
                return null;
            }

            Texture2D depthTex = (Texture2D)meshRenderer.sharedMaterial.GetTexture("_Depth");
            if (!depthTex.isReadable)
            {
                throw new InvalidOperationException($"The texture must be readable. (Check if Read/Write is set in the texture's import settings)");
            }
            if (!DepthSampler.IsFormatSupported(depthTex.graphicsFormat))
            {
                throw new NotSupportedException($"The texture format {depthTex.graphicsFormat} of this depth texture is not supported.");
            }
            IDepthSampler sampler = DepthSampler.Get(depthTex);

            float scale = meshRenderer.sharedMaterial.GetFloat("_Scale");
            float max = meshRenderer.sharedMaterial.GetFloat("_Max");
            bool inverse = meshRenderer.sharedMaterial.GetFloat("_InverseDepth") > 0.5f;
            Mesh mesh = meshFilter.sharedMesh;
            Mesh extracted;
            NativeArray<Vector3> vertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<ushort> indices = new NativeArray<ushort>((int)mesh.GetIndexCount(0), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<Vector2> uvs = new NativeArray<Vector2>(mesh.vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using (var data = Mesh.AcquireReadOnlyMeshData(mesh))
            {
                data[0].GetVertices(vertices);
                data[0].GetIndices(indices, 0);
                data[0].GetUVs(0, uvs);
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 uv = uvs[i];
                uv.x = 1 - uv.x;
                uvs[i] = uv;

                float depth = sampler.SampleBilinear(uv);
                if (inverse)
                    depth = scale / depth;
                else
                    depth = scale * depth;

                depth = Mathf.Clamp(depth, 0, max * scale);
                vertices[i] = vertices[i] * depth;
            }

            extracted = new Mesh();
            extracted.SetVertices(vertices);
            extracted.SetIndices(indices, MeshTopology.Triangles, 0);
            extracted.SetUVs(0, uvs);
            extracted.RecalculateNormals();
            extracted.UploadMeshData(false);
            extracted.name = $"{gameObject.name}_Mesh";

            vertices.Dispose();
            indices.Dispose();
            uvs.Dispose();

            return extracted;
        }
    }
}