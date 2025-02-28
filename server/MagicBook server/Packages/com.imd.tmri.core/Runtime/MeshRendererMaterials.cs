using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TMRI.Core
{
    [RequireComponent(typeof(Renderer))]
    public class MeshRendererMaterials : MonoBehaviour, IClipBoxListener
    {
        public List<FloodShader> AvailableMaterials { get; private set; } = new();

        private void Start()
        {
            if(GetComponentInParent<FloodDataPlayer>() is FloodDataPlayer fdp)
            {
                SetMaterialByFloodType(fdp.lastVisualizationType);
            }
        }

        public void OnClipBoxChanged(Matrix4x4 worldToLocal, Matrix4x4 localToWorld)
        {
            var meshBounds = GetComponent<Collider>().bounds;
            Debug.Log($"1 Mesh bounds: {meshBounds.min} {meshBounds.max}");
            var mesh = GetComponent<MeshFilter>().mesh;
            var minVertexX = mesh.vertices.Min(v => v.x);
            var minVertexZ = mesh.vertices.Min(v => v.z);
            var maxVertexX = mesh.vertices.Max(v => v.x);
            var maxVertexZ = mesh.vertices.Max(v => v.z);
            var minBounds = transform.TransformPoint(maxVertexX, 0, maxVertexZ);
            var maxBounds = transform.TransformPoint(minVertexX, 0, minVertexZ); 
            Debug.Log($"2 Mesh bounds: {minBounds} {maxBounds}");

            foreach (var m in AvailableMaterials.Where(m => m.FloodType == FloodVisualizationType.Depth))
            {
                var tex = (Texture2D)m.FloodMaterial.GetTexture("_HeightMap");

                // Get the dimensions of the texture
                int texWidth = tex.width;
                int texHeight = tex.height;

                // Define the 8 corners of the clipBox in local space
                Vector3[] clipBoxCorners = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
                    new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f),
                    new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f),
                };

                //// Transform the corners into world space and then into the height map's UV space
                //Vector2[] uvCorners = clipBoxCorners
                //    .Select(corner => clipBox.MultiplyPoint3x4(corner)) // Transform to world space
                //    .Select(worldPos => new Vector2(worldPos.x, worldPos.z)) // Project onto X-Z plane
                //    .Select(xzPos => new Vector2((xzPos.x + 0.5f), (xzPos.y + 0.5f))) // Map to UV range (assuming unit mapping)
                //    .ToArray();

                //// Find the bounding rectangle in UV space
                //float minU = uvCorners.Min(c => c.x);
                //float maxU = uvCorners.Max(c => c.x);
                //float minV = uvCorners.Min(c => c.y);
                //float maxV = uvCorners.Max(c => c.y);

                // Transform corners into world space
                Vector3[] worldCorners = clipBoxCorners.Select(corner => localToWorld.MultiplyPoint(corner)).ToArray();
                var worldMin = new Vector3(worldCorners.Min(w => w.x),0, worldCorners.Min(w => w.z));
                var worldMax = new Vector3(worldCorners.Max(w => w.x),0, worldCorners.Max(w => w.z));
                Debug.Log($"World bounds: {worldMin} {worldMax}");

                // Map world space corners to UV space based on mesh bounds
                //Vector2[] uvCorners = worldCorners.Select(worldPos =>
                //{
                //    float u = Mathf.InverseLerp(meshBounds.min.x, meshBounds.max.x, worldPos.x);
                //    float v = Mathf.InverseLerp(meshBounds.min.z, meshBounds.max.z, worldPos.z);
                //    return new Vector2(u, v);
                //}).ToArray();
                //Vector2[] uvCorners = worldCorners.Select(worldPos =>
                //{
                //    float u = (worldPos.x - minBounds.x) / (maxBounds.x - minBounds.x);
                //    float v = (worldPos.z - minBounds.z) / (maxBounds.z - minBounds.z);
                //    return new Vector2(u, v);
                //}).ToArray();
                

                //float minU = uvCorners.Min(c => c.x);
                //float maxU = uvCorners.Max(c => c.x);
                //float minV = uvCorners.Min(c => c.y);
                //float maxV = uvCorners.Max(c => c.y);
                var minUV = new Vector2((worldMin.x - minBounds.x) / (maxBounds.x - minBounds.x), (worldMin.z - minBounds.z) / (maxBounds.z - minBounds.z));//new Vector2(minU, minV);
                var maxUV = new Vector2((worldMax.x - minBounds.x) / (maxBounds.x - minBounds.x), (worldMax.z - minBounds.z) / (maxBounds.z - minBounds.z));//new Vector2(maxU, maxV);
                Debug.Log($"UV corners: {minUV} {maxUV}");

                // Convert UV bounds to pixel bounds
                int x = Mathf.Clamp(Mathf.FloorToInt(minUV.x * texWidth), 0, texWidth - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt(minUV.y * texHeight), 0, texHeight - 1);
                int width = Mathf.Clamp(Mathf.CeilToInt((maxUV.x - minUV.x) * texWidth), 0, texWidth - x);
                int height = Mathf.Clamp(Mathf.CeilToInt((maxUV.y - minUV.y) * texHeight), 0, texHeight - y);

                var pixels = tex.GetPixels(x, y, width, height);
                Debug.Log($"Get pixels: {x},{y},{width},{height}");

                var filteredPixels = pixels.Where(p => p.r != 0f).ToArray();
                var minHeight = filteredPixels.Length > 0 ? filteredPixels.Min(p => p.r) : 0.0f;
                var maxHeight = filteredPixels.Length > 0 ? filteredPixels.Max(p => p.r) : 1.0f;
                m.FloodMaterial.SetFloat("_MinHeight", minHeight);
                m.FloodMaterial.SetFloat("_MaxHeight", maxHeight);
            }
        }

        public void SetMaterialByFloodType(FloodVisualizationType type)
        {
            if (AvailableMaterials.FirstOrDefault(m => m.FloodType == type) is FloodShader fs)
            {
                GetComponent<Renderer>().material = fs.FloodMaterial;
            }
        }

        public void CloneMaterials(List<FloodShader> original)
        {
            AvailableMaterials = new List<FloodShader>();
            foreach(var fs in original)
            {
                AvailableMaterials.Add(new FloodShader
                {
                    FloodType = fs.FloodType,
                    FloodMaterial = new Material(fs.FloodMaterial)
                });
            }
        }
    }

    public interface IClipBoxListener
    {
        public void OnClipBoxChanged(Matrix4x4 worldToLocal, Matrix4x4 localToWorld);
    }
}