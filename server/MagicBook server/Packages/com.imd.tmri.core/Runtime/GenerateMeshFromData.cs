using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TMRI.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class GenerateMeshFromData : MonoBehaviour, IShaderChangeEventListener, IFloodVisualizationListener
{
    //public GeometryDataReader dataReader;
    public float vertexConsideredIdenticalThreshold = 0.001f;
    public bool ReconstructOnStart;
    public bool FlipNormals;
    //public bool MergeNeighbours;
    public int ParallelTasksCount = 1;
    //public Shader ShaderToVisualizeFlood;
    //public List<ShaderHandler.NamedShader> ShaderToVisualizeFlood = new();
    public bool MinMaxBasedOnChunk;
    //public Bounds WithinBounds;
    public int PreloadedMeshCount;
    [SerializeField]
    GameObject ChunkedMeshPrefab;

    public List<FloodShader> FloodShaders;
    public Material DefaultMaterial;
    public Vector3 MinWorldXZ;
    public Vector3 MaxWorldXZ;
    public bool UniformUVs;

    public float MaxHeightOverallChunks;

    GameObject getChunkedMeshPrefab => OverrideChunkedMeshPrefab != null ? OverrideChunkedMeshPrefab : ChunkedMeshPrefab;
    CancellationTokenSource tasksCancellation = new CancellationTokenSource();

    Material GetFloodMaterial => FloodShaders?.FirstOrDefault(fs => fs.FloodType == lastVisualizationType)?.FloodMaterial ?? DefaultMaterial;

    public string EventKey { get => "FloodMesh"; set { } }
    public List<GeometryDataReader.GeometryQuad> quads { get; set; }
    public GameObject OverrideChunkedMeshPrefab { get; set; }

    KeyValuePair<GameObject, Mesh>[] preloadedMeshes = new KeyValuePair<GameObject, Mesh>[0];
    System.Threading.SynchronizationContext main;
    //Bounds transformedBounds;
    FloodVisualizationType lastVisualizationType;

    [SerializeField]
    internal List<SerializableMesh> SerialiableMeshes = new();

    private void OnDestroy()
    {
        tasksCancellation.Cancel();
    }

    public void OnChangeToShader(Shader shader)
    {
        //GetComponent<MeshRenderer>().material.shader = shader;
        //foreach (var mr in GetComponentsInChildren<MeshRenderer>())
        //{
        //    mr.material.shader = shader;
        //}
    }

    public void SetMaxFilesFromString(string numberString)
    {
        //if (int.TryParse(numberString, out int res))
        //{
        //    dataReader.filenameFormatMaxFilesCount = res;

        //    CurrentTimeStep %= res;
        //    OnMaxFilesChanged?.Invoke(res);
        //}
    }

    public void SetFloodVisualizationType(FloodVisualizationType type)
    {
        //FloodVisualization = type;

        //foreach (var mr in FindObjectsByType<MeshRendererMaterials>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        foreach (var mr in GetComponentsInChildren<MeshRendererMaterials>(true))
        {
            //Debug.Log($"SetMaterialByFloodType type {type} to {mr.gameObject.name}");
            mr.SetMaterialByFloodType(type);
        }

        //if (lastMeshAndRenderers != null)
        //    foreach (var m in lastMeshAndRenderers)
        //    {
        //        Debug.Log($"SetMeshRendererMaterialProperties type {type} to {m.Value.gameObject.name}");
        //        SetMeshRendererMaterialProperties(m.Key, m.Value);
        //    }

        lastVisualizationType = type;
    }

    public void OnFloodTimeStepUpdated(int timeStep)
    {
        
    }

    private void Start()
    {
        if (PreloadedMeshCount > 0)
            preloadedMeshes = new KeyValuePair<GameObject, Mesh>[PreloadedMeshCount];

        for(int i=0; i<preloadedMeshes.Length; i++)
        {
            var mesh = new Mesh();

            var obj = ChunkedMeshPrefab == null ? new GameObject($"ChunkedMesh_{i}") : Instantiate(getChunkedMeshPrefab);
            obj.transform.SetParent(transform, worldPositionStays: false);
            //obj.layer = 6; // Interactable

            var meshFilter = obj.AddComponent<MeshFilter>();
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            var meshCollider = obj.AddComponent<MeshCollider>();

            //meshCollider.sharedMesh = mesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = true;
            meshFilter.mesh = mesh;
            meshRenderer.material = GetFloodMaterial;//GetComponent<MeshRenderer>().material;
            //meshRenderer.material.shader = GetComponent<MeshRenderer>().material.shader;

            preloadedMeshes[i] = new KeyValuePair<GameObject, Mesh>(obj, mesh);
        }

        main = System.Threading.SynchronizationContext.Current;
        //dataReader.OnLoadedQuads += () => StartCoroutine(ReconstructMeshFromDataAsync());
        //dataReader.OnLoadedQuads += () => ReconstructMeshFromData();

        ///OnMaxFilesChanged?.Invoke(dataReader.filenameFormatMaxFilesCount);

        if (ReconstructOnStart)
            //StartCoroutine(ReconstructWithQuads(quads));
            ReconstructWithQuads(quads);
    }

    //public List<GameObject> Temp_Hardcoded_Floods = new();
    //public Dictionary<Vector3, GameObject> FloodOriginParents = new();
    //public Vector3 currentOrigin;
    //public GameObject currentParent;
    //public void OnFloodOrigin(Vector3 origin, string assetName)
    //{
    //    currentOrigin = origin;

    //    GeoCoordinateConverter.referenceLatitude = 34.594804763793945;
    //    GeoCoordinateConverter.referenceLongitude = 135.69652557373047;
    //    GeoCoordinateConverter.referenceAltitude = 37.94;
    //    //GeoCoordinateConverter.referenceElevation = dataReader.referenceElevation;

    //    var offset = GeoCoordinateConverter.GeoToUnity4(origin.x, origin.y, origin.z, 1f);
    //    transform.localPosition = new Vector3(offset.x, 0f, offset.z);

    //    foreach (var obj in Temp_Hardcoded_Floods)
    //    {
    //        obj.SetActive(false);
    //        if (obj.name == assetName)
    //        {
    //            obj.SetActive(true);
    //            currentParent = obj;
    //        }
    //    }

    //    //startTime = Time.time + 1f;
    //}

    public void OnAssetIsChanging(string assetName)
    {
        //foreach (var obj in Temp_Hardcoded_Floods)
        foreach(Transform obj in transform)
        {
            ///obj.gameObject.SetActive(false);
        }
    }

    

    //public void SetPlaySettings(float animationSpeedSeconds, int animationStep = -1)
    //{
    //    if(this != null)
    //        CancelInvoke(nameof(DoReadDataAsync));

    //    if(animationStep >= 0)
    //        dataReader.currentFileIndex = animationStep;

    //    InvokeRepeating(nameof(DoReadDataAsync), animationSpeedSeconds, animationSpeedSeconds);
    //}

    //public async void DoReadDataAsync()
    private async void ReconstructWithQuads(List<GeometryDataReader.GeometryQuad> quads)
    {
        //if (dataReader != null && dataReaderLastRequestedTimeStep < dataReader.filenameFormatMaxFilesCount)
        {
            //transformedBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

            //Debug.Log($"Bounds:{transformedBounds}, {transformedBounds.size}");

            //var cachedTimeStep = dataReaderLastRequestedTimeStep;//dataReader.currentFileIndex;
            //dataReaderLastRequestedTimeStep++;
            //var quads = await Task.Run(() => dataReader.ReadData(currentFileIndex: cachedTimeStep), tasksCancellation.Token);
            if (quads == null || !quads.Any())
                return;
                //yield break;

            float minDepthAbs = 0f;
            float maxDepthAbs = 1f;
            var meshes = await Task.Run(() => ReconstructMeshFromData(quads, out minDepthAbs, out maxDepthAbs), tasksCancellation.Token);
            //var meshes = ReconstructMeshFromData(quads, out minDepthAbs, out maxDepthAbs);

            float minSpeed = quads.Min(q => q.speed);
            float maxSpeed = quads.Max(q => q.speed);
            float minDepth = quads.Min(q => q.depth);
            float maxDepth = quads.Max(q => q.depth);

            var minQuadX = Mathf.Min(quads[0].p1.x, quads[0].p2.x, quads[0].p3.x, quads[0].p4.x);
            var minQuadZ = Mathf.Min(quads[0].p1.z, quads[0].p2.z, quads[0].p3.z, quads[0].p4.z);
            var maxQuadX = Mathf.Max(quads[0].p1.x, quads[0].p2.x, quads[0].p3.x, quads[0].p4.x);
            var maxQuadZ = Mathf.Max(quads[0].p1.z, quads[0].p2.z, quads[0].p3.z, quads[0].p4.z);
            var (quadWidthWorld, quadHeightWorld) = (maxQuadX - minQuadX, maxQuadZ - minQuadZ);//(28.7f, 22.8f);

            foreach (var sMesh in meshes)
            {
                var worldVerts = sMesh.vertices;//.Select(v => transform.localToWorldMatrix.MultiplyPoint3x4(v));

                var minWorldX = worldVerts.Min(v => v.x);
                var minWorldZ = worldVerts.Min(v => v.z);
                var maxWorldX = worldVerts.Max(v => v.x);
                var maxWorldZ = worldVerts.Max(v => v.z);

                //var (textureWidth, textureHeight) = CalculateTextureDimensions(worldVerts.ToArray());
                var (textureWidth, textureHeight) = (Mathf.CeilToInt((maxWorldX - minWorldX) / quadWidthWorld), Mathf.RoundToInt((maxWorldZ - minWorldZ) / quadHeightWorld));

                var floodSpeedTexture = GenerateHeightTexture(quads.Select(q => (new[] { q.p1, q.p2, q.p3, q.p4 }, q.speed)).ToList(), textureWidth, textureHeight, minWorldX, maxWorldX, minWorldZ, maxWorldZ, minSpeed, maxSpeed);
                var floodDepthTexture = GenerateHeightTexture(quads.Select(q => (new[] { q.p1, q.p2, q.p3, q.p4 }, q.depth)).ToList(), textureWidth, textureHeight, minWorldX, maxWorldX, minWorldZ, maxWorldZ, minDepth, maxDepth);
                //var floodDepthTexture = GenerateHeightTexture(quads.Select(q => q.depth).ToList(), textureWidth, textureHeight, 0, 10f);

                sMesh.speedTexture = floodSpeedTexture;
                sMesh.depthTexture = floodDepthTexture;
            }

            if (this != null)
                ApplyMeshes(meshes, minDepthAbs, maxDepthAbs, minDepth, maxDepth, minSpeed, maxSpeed);
        }
    }

    internal class SerializableMesh
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uv;
        public bool hasIncorrectTriangles;
        public Texture2D depthTexture;
        public Texture2D speedTexture;
        public int width;
        public int height;
    }

    public static Texture2D GenerateHeightTexture(
        List<float> quads,
        int textureWidth,
        int textureHeight,
        float minHeight,
        float maxHeight)
    {
        // Create a new texture
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RFloat, false);
        //Debug.Log($"Tex width {textureWidth} height {textureHeight}");
        // Fill the texture with flood_speed values
        Color32[] pixels = new Color32[textureWidth * textureHeight];

        // Calculate grid dimensions
        int gridWidth = Mathf.CeilToInt(Mathf.Sqrt(quads.Count));
        int gridHeight = Mathf.CeilToInt((float)quads.Count / gridWidth);

        for (int i = 0; i < quads.Count; i++)
        {
            var quad = quads[i];

            // Normalize flood_speed to [0, 1]
            float normalizedFloodSpeed = Mathf.InverseLerp(minHeight, maxHeight, quad);
            byte floodSpeedByte = (byte)(normalizedFloodSpeed * 255); // Convert to 8-bit

            // Calculate the quad's position in the grid
            int quadX = i % gridWidth;
            int quadY = i / gridWidth;

            // Map the quad's grid position to texture space
            int xMin = Mathf.FloorToInt((float)quadX / gridWidth * textureWidth);
            int xMax = Mathf.FloorToInt((float)(quadX + 1) / gridWidth * textureWidth);
            int yMin = Mathf.FloorToInt((float)quadY / gridHeight * textureHeight);
            int yMax = Mathf.FloorToInt((float)(quadY + 1) / gridHeight * textureHeight);

            //Debug.Log($"quad {i} xMin {xMin} xMax {xMax} yMin {yMin} yMax {yMax}");
            // Assign flood_speed to the corresponding region in the texture
            for (int x = xMin; x < xMax; x++)
            {
                for (int y = yMin; y < yMax; y++)
                {
                    pixels[y * textureWidth + x] = new Color32(floodSpeedByte, 0, 0, 255);
                    //Debug.Log($"Set color {pixels[y * textureWidth + x]} to pixel {y * textureWidth + x}");
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return texture;
    }

    private Texture2D GenerateHeightTexture(
        List<(Vector3[], float)> quads,
        int textureWidth,
        int textureHeight,
        float minWorldX,
        float maxWorldX,
        float minWorldZ,
        float maxWorldZ,
        float minFloodSpeed,
        float maxFloodSpeed)
    {
        // Create a new texture
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RFloat, false);

        // Initialize pixel array
        Color32[] pixels = new Color32[textureWidth * textureHeight];

        // Calculate world-to-texture space scaling factors
        float xScale = (textureWidth-1) / (maxWorldX - minWorldX);
        float zScale = (textureHeight-1) / (maxWorldZ - minWorldZ);

        // Populate texture pixels based on quad flood speeds
        //foreach (var quad in quads)
        //{
        //    foreach (var vertex in quad.Item1)
        //    {
        //        // Normalize world position to texture coordinates
        //        float normalizedX = (vertex.x - minWorldX) * xScale;
        //        float normalizedZ = (vertex.z - minWorldZ) * zScale;

        //        int x = Mathf.Clamp(Mathf.RoundToInt(normalizedX), 0, textureWidth - 1);
        //        int z = Mathf.Clamp(Mathf.RoundToInt(normalizedZ), 0, textureHeight - 1);

        //        // Normalize flood speed to [0, 1] and map to a byte
        //        float normalizedFloodSpeed = Mathf.InverseLerp(minFloodSpeed, maxFloodSpeed, quad.Item2);
        //        byte floodSpeedByte = (byte)(normalizedFloodSpeed * 255);

        //        // Set the pixel value
        //        pixels[z * textureWidth + x] = new Color32(floodSpeedByte, 0, 0, 255);
        //    }
        //}
        foreach (var quad in quads)
        {
            // Get the bounding box of the quad in texture space
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var vertex in quad.Item1)
            {
                float normalizedX = (vertex.x - minWorldX) * xScale;
                float normalizedZ = (vertex.z - minWorldZ) * zScale;

                minX = Mathf.Min(minX, normalizedX);
                maxX = Mathf.Max(maxX, normalizedX);
                minZ = Mathf.Min(minZ, normalizedZ);
                maxZ = Mathf.Max(maxZ, normalizedZ);
            }

            int xStart = Mathf.Clamp(Mathf.FloorToInt(minX), 0, textureWidth - 1);
            int xEnd = Mathf.Clamp(Mathf.CeilToInt(maxX), 0, textureWidth - 1);
            int zStart = Mathf.Clamp(Mathf.FloorToInt(minZ), 0, textureHeight - 1);
            int zEnd = Mathf.Clamp(Mathf.CeilToInt(maxZ), 0, textureHeight - 1);

            // Fill the entire quad area
            for (int x = xStart; x <= xEnd; x++)
            {
                for (int z = zStart; z <= zEnd; z++)
                {
                    float normalizedFloodSpeed = Mathf.InverseLerp(minFloodSpeed, maxFloodSpeed, quad.Item2);
                    byte floodSpeedByte = (byte)(normalizedFloodSpeed * 255);
                    pixels[z * textureWidth + x] = new Color32(floodSpeedByte, 0, 0, 255);
                }
            }
        }

        // Apply pixels to texture
        texture.SetPixels32(pixels);
        texture.Apply();

        return texture;
    }


    private SerializableMesh GenerateMeshFromQuadsChunk(List<Vector3[]> quads, float mergeThreshold)
    {
        //Mesh mesh = new Mesh();
        var mesh = new SerializableMesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Step 1: Add vertices and triangles for each quad
        foreach (var quad in quads)
        {
            int startIndex = vertices.Count;
            vertices.AddRange(quad); // Add 4 vertices of the quad

            // Add 2 triangles for the quad
            if (FlipNormals)
            {
                triangles.Add(startIndex);
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex);
                triangles.Add(startIndex + 3);
                triangles.Add(startIndex + 2);
            }
            else
            {
                triangles.Add(startIndex);
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex);
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 3);
            }

            // Add UVs (assume a quad is unit square in UV space, normalized later)
            uvs.Add(new Vector2(0, 0)); // Bottom-left
            uvs.Add(new Vector2(1, 0)); // Bottom-right
            uvs.Add(new Vector2(1, 1)); // Top-right
            uvs.Add(new Vector2(0, 1)); // Top-left
        }

        // The merging and UV normalization logic is similar to the previous implementation

        // Compact vertices, merge neighbors, and normalize UVs
        float cellSize = mergeThreshold;
        var spatialHash = new ConcurrentDictionary<Vector3Int, List<int>>();

        Vector3Int GetCell(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.y / cellSize),
                Mathf.FloorToInt(position.z / cellSize)
            );
        }

        Parallel.For(0, vertices.Count, i =>
        {
            Vector3Int cell = GetCell(vertices[i]);
            spatialHash.AddOrUpdate(
                cell,
                new List<int> { i },
                (key, existingList) =>
                {
                    lock (existingList) { existingList.Add(i); }
                    return existingList;
                });
        });

        Vector3[] mergedVertices = new Vector3[vertices.Count];
        Vector2[] mergedUVs = new Vector2[vertices.Count];
        int[] remap = new int[vertices.Count];
        bool[] visited = new bool[vertices.Count];
        object lockObj = new object();

        Parallel.For(0, vertices.Count, i =>
        {
            if (visited[i]) return;

            Vector3 current = vertices[i];
            Vector3Int cell = GetCell(current);
            Vector3 averagePos = current;
            Vector2 averageUV = uvs[i];
            int count = 1;

            lock (lockObj)
            {
                visited[i] = true;
                remap[i] = i;
            }

            foreach (int dx in new int[] { -1, 0, 1 })
            {
                foreach (int dy in new int[] { -1, 0, 1 })
                {
                    foreach (int dz in new int[] { -1, 0, 1 })
                    {
                        Vector3Int neighborCell = cell + new Vector3Int(dx, dy, dz);
                        if (spatialHash.TryGetValue(neighborCell, out List<int> indices))
                        {
                            foreach (int j in indices)
                            {
                                if (i != j && !visited[j] && Vector3.Distance(current, vertices[j]) <= mergeThreshold)
                                {
                                    lock (lockObj)
                                    {
                                        visited[j] = true;
                                        remap[j] = i;
                                        averagePos += vertices[j];
                                        averageUV += uvs[j];
                                        count++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            lock (lockObj)
            {
                mergedVertices[i] = averagePos / count;
                mergedUVs[i] = averageUV / count;
            }
        });

        Dictionary<int, int> newIndexMap = new Dictionary<int, int>();
        List<Vector3> compactedVertices = new List<Vector3>();
        List<Vector2> compactedUVs = new List<Vector2>();

        for (int i = 0; i < vertices.Count; i++)
        {
            int mergedIndex = remap[i];
            if (!newIndexMap.ContainsKey(mergedIndex))
            {
                newIndexMap[mergedIndex] = compactedVertices.Count;
                compactedVertices.Add(mergedVertices[mergedIndex]);
                compactedUVs.Add(mergedUVs[mergedIndex]);
            }
        }

        int[] remappedTriangles = new int[triangles.Count];
        for (int i = 0; i < triangles.Count; i++)
        {
            remappedTriangles[i] = newIndexMap[remap[triangles[i]]];

            if (remappedTriangles[i] > compactedVertices.Count)
                mesh.hasIncorrectTriangles = true;
        }

        Vector3 min = compactedVertices[0];
        Vector3 max = compactedVertices[0];
        foreach (var vertex in compactedVertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }

        // Calculate the size of the mesh
        Vector3 size = max - min;

        // Determine the larger dimension to ensure uniform UV scaling
        float maxDimension = Mathf.Max(size.x, size.z);

        // Update UV coordinates to maintain a square pattern
        for (int i = 0; i < compactedUVs.Count; i++)
        {
            Vector3 vertex = compactedVertices[i];
            compactedUVs[i] = new Vector2(
                (vertex.x - min.x) / (UniformUVs ? maxDimension : size.x),
                (vertex.z - min.z) / (UniformUVs ? maxDimension : size.z)
            );
        }

        if (remappedTriangles.Length % 3 != 0)
            //|| compactedVertices.Count < remappedTriangles.Length)
            mesh.hasIncorrectTriangles = true;

        mesh.vertices = compactedVertices.ToArray();
        mesh.triangles = remappedTriangles;
        mesh.uv = compactedUVs.ToArray();
        //mesh.RecalculateNormals();
        //mesh.RecalculateBounds();

        //Texture2D heightTexture = null;// GenerateHeightTexture(mergedVertices.ToList(), resolution: 256);

        //return new(mesh, heightTexture);

        return mesh;
    }

    private static (int textureWidth, int textureHeight) CalculateTextureDimensions(Vector3[] meshVertices)
    {
        // Estimate number of quads (assuming 4 vertices per quad before merging)
        int numQuads = meshVertices.Length / 4;

        // Calculate square texture size (keeping it power-of-two for efficiency)
        int textureSize = Mathf.CeilToInt(Mathf.Sqrt(numQuads));

        // Return as square dimensions
        return (textureSize, textureSize);
    }

    private List<SerializableMesh> GenerateMeshesFromQuads(List<Vector3[]> quads, float mergeThreshold)
    {
        const int MaxVerticesPerMesh = 65534; // Unity's limit for a single mesh
        var meshes = new List<SerializableMesh>();

        // Split quads into chunks that can fit within the vertex limit
        List<List<Vector3[]>> quadChunks = new List<List<Vector3[]>>();
        List<Vector3[]> currentChunk = new List<Vector3[]>();
        int currentVertexCount = 0;

        foreach (var quad in quads)
        {
            if (currentVertexCount + quad.Length > MaxVerticesPerMesh)
            {
                // Start a new chunk
                quadChunks.Add(currentChunk);
                currentChunk = new List<Vector3[]>();
                currentVertexCount = 0;
            }

            currentChunk.Add(quad);
            currentVertexCount += quad.Length;
        }

        // Add the last chunk
        if (currentChunk.Count > 0)
            quadChunks.Add(currentChunk);

        // Process each chunk into its own mesh
        foreach (var chunk in quadChunks)
        {
            meshes.Add(GenerateMeshFromQuadsChunk(chunk, mergeThreshold));
        }

        return meshes;
    }

    internal List<SerializableMesh> ReconstructMeshFromData(List<GeometryDataReader.GeometryQuad> quads, out float minDepth, out float maxDepth)
    {
        List<SerializableMesh> sMeshes = new();
        minDepth = 0f;
        maxDepth = 1f;
        //transformedBounds = new Bounds(WithinBounds.center, new Vector3(
        //        WithinBounds.size.x / Mathf.Abs(transform.lossyScale.x),
        //        WithinBounds.size.y / Mathf.Abs(transform.lossyScale.y),
        //        WithinBounds.size.z / Mathf.Abs(transform.lossyScale.z)));
        //Debug.Log($"Bounds:{transformedBounds}, {transformedBounds.size}");

        //Debug.Log($"Total quads: {loadedQuads.Count}\tFiltered:{loadedQuads.Where(q => bounds.Contains(q.center)).Count()}");
        //await Task.Run(() =>
        //{
            //var loadedQuadsClone = new List<GeometryDataReader.GeometryQuad>(loadedQuads);
            //var quads = loadedQuadsClone
            //    .Where(q => bounds.Contains(q.center));
            var quadsVertices = quads
                .Select(q => new Vector3[4] { q.p1, q.p2, q.p3, q.p4 }).ToList();
            
            sMeshes = GenerateMeshesFromQuads(quadsVertices, vertexConsideredIdenticalThreshold);
            var ordered = sMeshes.SelectMany(m => m.vertices).OrderBy(v => v.y).ToList();

            if (ordered.Any())
            {
                minDepth = ordered[0].y;
                maxDepth = ordered[ordered.Count - 1].y;
            }

        //});

        //if (!sMeshes.Any())
            return sMeshes;
    }

    List<KeyValuePair<SerializableMesh, MeshRenderer>> lastMeshAndRenderers = new();

    internal void ApplyMeshes(List<SerializableMesh> sMeshes, float minDepth, float maxDepth, float minHeight, float maxHeight, float minSpeed, float maxSpeed)
    {
        if (PreloadedMeshCount <= 0)
        {
            //foreach (Transform child in transform)
            //Destroy(child.gameObject);
        }
        else
        {
            foreach (var mesh in preloadedMeshes)
            {
                mesh.Key.GetComponent<MeshCollider>().sharedMesh = null;
                mesh.Value.Clear();
            }
        }

        lastMeshAndRenderers.Clear();
        //var timeStepParentGO = new GameObject(timeStep.ToString());
        //timeStepParentGO.transform.SetParent(parentGO.transform, worldPositionStays: false);
        //timeStepParentGO.SetActive(false);
        int i = 0;
        foreach (var sMesh in sMeshes)
        {
            var mesh = PreloadedMeshCount > 0 ? preloadedMeshes[i].Value : new Mesh();

            if(!sMesh.hasIncorrectTriangles)
            {
                mesh.vertices = sMesh.vertices;
                mesh.triangles = sMesh.triangles;
                mesh.uv = sMesh.uv;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }
            else
            {
                Debug.LogWarning("Mesh had incorrect triangle indices so skipping it!");
            }

            if (MinMaxBasedOnChunk)
            {
                minDepth = mesh.vertices.Min(v => v.y);
                maxDepth = mesh.vertices.Max(v => v.y);
            }

            MeshRenderer meshRenderer;
            if (PreloadedMeshCount > 0)
            {
                meshRenderer = preloadedMeshes[i].Key.GetComponent<MeshRenderer>();
                var meshCollider = preloadedMeshes[i].Key.GetComponent<MeshCollider>();
                //meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = mesh;

                meshRenderer.material.SetFloat("_MinDepth", minDepth);
                meshRenderer.material.SetFloat("_MaxDepth", maxDepth);
            }
            else
            {
                GameObject obj = Instantiate(getChunkedMeshPrefab);//new GameObject($"ChunkedMesh_{i}");
                obj.name = $"ChunkedMesh_{i}";
                obj.transform.SetParent(transform, worldPositionStays: false);
                //obj.layer = 6;
                var meshFilter = obj.GetComponent<MeshFilter>();
                meshRenderer = obj.GetComponent<MeshRenderer>();
                var meshCollider = obj.GetComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                //meshCollider.convex = true;
                //meshCollider.isTrigger = true;

                var rendererMaterials = obj.GetComponent<MeshRendererMaterials>();
                rendererMaterials.CloneMaterials(FloodShaders);
                
                foreach (var s in rendererMaterials.AvailableMaterials)
                {
                    switch (s.FloodType)
                    {
                        case FloodVisualizationType.Realistic:
                            break;
                        case FloodVisualizationType.Depth:
                        case FloodVisualizationType.Depth_Alt:
                            s.FloodMaterial.SetTexture("_HeightMap", sMesh.depthTexture);
                            break;
                        case FloodVisualizationType.Speed:
                            s.FloodMaterial.SetTexture("_HeightMap", sMesh.speedTexture);
                            break;
                        default:
                            break;
                    }

                    s.FloodMaterial.SetFloat("_MinDepth", minDepth);
                    s.FloodMaterial.SetFloat("_MaxDepth", maxDepth);
                    s.FloodMaterial.SetFloat("_MinHeight", minHeight);
                    s.FloodMaterial.SetFloat("_MaxHeight", maxHeight);
                    s.FloodMaterial.SetFloat("_MinSpeed", minSpeed);
                    s.FloodMaterial.SetFloat("_MaxSpeed", maxSpeed);

                    MaxHeightOverallChunks = Mathf.Max(maxHeight, MaxHeightOverallChunks);
                }

                meshFilter.mesh = mesh;
                //meshRenderer.material = GetComponent<MeshRenderer>().material;//yourMaterial;
            }

            //SetMeshRendererMaterialProperties(sMesh, meshRenderer);
            SetFloodVisualizationType(lastVisualizationType);

            

            //SerialiableMeshes.Add(sMesh);
            //lastMeshAndRenderers.Add(new KeyValuePair<SerializableMesh, MeshRenderer>(sMesh, meshRenderer));
            //Debug.Log($"Reconstruction: {mesh.vertices.Length} verts, {mesh.triangles.Length} tris, {mesh.uv.Length} uvs");
            i++;
        }
    }

    //void SetMeshRendererMaterialProperties(SerializableMesh sMesh, MeshRenderer meshRenderer)
    //{
    //    if (meshRenderer == null || meshRenderer.gameObject == null)
    //        return;

    //    meshRenderer.material = GetFloodMaterial;

    //    //switch (FloodVisualization)
    //    switch(lastVisualizationType)
    //    {
    //        case FloodVisualizationType.Realistic:
    //            break;
    //        case FloodVisualizationType.Depth:
    //            meshRenderer.material.SetTexture("_HeightMap", sMesh.depthTexture);
    //            break;
    //        case FloodVisualizationType.Speed:
    //            meshRenderer.material.SetTexture("_HeightMap", sMesh.speedTexture);
    //            break;
    //        default:
    //            break;
    //    }
    //}

#if UNITY_EDITOR
    Mesh meshTodraw;
    List<(Vector3, string)> drawLabels = new();

    private void OnDrawGizmosSelected()
    {
        //if (Application.isPlaying)
        //{
        //    if (meshTodraw == null && TryGetComponent<MeshFilter>(out MeshFilter mf) && mf.mesh != null)
        //        meshTodraw = mf.mesh;

        //    if (drawLabels.Any())
        //    {
        //        foreach(var v in drawLabels)
        //            Handles.Label(new Vector3(-v.Item1.x,v.Item1.y, -v.Item1.z), v.Item2);
        //        return;
        //    }
        //    //foreach (var t in meshTodraw.triangles)
        //    for(int i=0; i< dataReader.LoadedQuads.Count; i += dataReader.LoadedQuads.Count / 1000)
        //    {
        //        if (i > dataReader.LoadedQuads.Count)
        //            break;

        //        drawLabels.Add(new(dataReader.LoadedQuads[i].center, dataReader.LoadedQuads[i].depth.ToString("F4")));
        //    }

        //    drawLabels = drawLabels.Distinct().ToList();
        //}
    }

#endif
}
