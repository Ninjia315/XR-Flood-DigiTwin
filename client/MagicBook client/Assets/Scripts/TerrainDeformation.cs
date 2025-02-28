using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using TMRI.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TerrainDeformation : MonoBehaviour
{
    public Terrain myTerrain;
    public GeometryDataReader dataReader;
    
    [Range(0, 1)]
    public float heightMultiplier = 1f;
    public Texture2D maskMapTexture;
    public int SectionSize = 1;
    public bool BuildTerrainOnStart;

    int xResolution;
    int zResolution;
    //float[,] heights;
    List<(Vector3, Vector3, Vector3, Vector3)> quads = new();
    List<(int, int, float)> terrainVerts = new();

    void Start()
    {
        //var copy = Instantiate(myTerrain.terrainData);
        //myTerrain.terrainData = copy;
        //myTerrain.terrainData = new TerrainData();
        //myTerrain.terrainData.heightmapResolution = 100000;

        Debug.Log($"left:{myTerrain.leftNeighbor==null} right:{myTerrain.rightNeighbor == null} top:{myTerrain.topNeighbor == null} bottom:{myTerrain.bottomNeighbor == null}");
        //heights = myTerrain.terrainData.GetHeights(0, 0, xResolution, zResolution);

        if(BuildTerrainOnStart)
            LoadTerrainFromData();
    }

    public void LoadTerrainFromData()
    {
        var copy = Instantiate(myTerrain.terrainData);
        myTerrain.terrainData = copy;

        xResolution = myTerrain.terrainData.heightmapResolution;
        zResolution = myTerrain.terrainData.heightmapResolution;

        

        quads.Clear();
        terrainVerts.Clear();

        var t = myTerrain;
        ClearTerrain(t, clearHoles:true, clearHeights:true, clearAlpha:true);

        //foreach(var quad in dataReader.LoadedQuads)
        //{
        //    quads.Add(new(quad.p1, quad.p2, quad.p3, quad.p4));

        //    try
        //    {
        //        SetTerrainSection(quad.center, SectionSize);
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.LogError(e.Message);
        //    }

        //}

        myTerrain.terrainData.SyncHeightmap();

        t.terrainData.terrainLayers = new TerrainLayer[1];
        
        float[,,] map = new float[t.terrainData.alphamapWidth, t.terrainData.alphamapHeight, 1];

        // For each point on the alphamap...
        for (int y = 0; y < t.terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < t.terrainData.alphamapWidth; x++)
            {
                // Get the normalized terrain coordinate that
                // corresponds to the point.
                float normX = x * 1.0f / (t.terrainData.alphamapWidth - 1);
                float normY = y * 1.0f / (t.terrainData.alphamapHeight - 1);

                // Get the steepness value at the normalized coordinate.
                var angle = t.terrainData.GetSteepness(normX, normY);

                // Steepness is given as an angle, 0..90 degrees. Divide
                // by 90 to get an alpha blending value in the range 0..1.
                //var frac = angle / 90.0;
                //map[x, y, 0] = (float)frac;
                //map[x, y, 1] = (float)(1 - frac);

                var xBase = (int)((normY) * xResolution);
                var yBase = (int)((normX) * zResolution);
                var frac = t.terrainData.GetHeight(xBase, yBase) / 10f;
                map[x, y, 0] = frac;

                if (angle / 90.0 > 0.0f)
                {
                    var holes = new bool[1, 1];
                    holes[0, 0] = true;
                    myTerrain.terrainData.SetHoles(yBase, xBase, holes);
                }
            }
        }
        t.terrainData.SetAlphamaps(0, 0, map);
    }

    void SetNewHeight(int x, int z, int ox, int oz, float height)
    {
        if (!terrainVerts.Any(tv => tv.Item1 == x && tv.Item2 == z))
        {
            var h = new float[1, 1];
            h[0, 0] = height;//myTerrain.terrainData.GetHeight(ox, oz);
            if(height > 20)
                Debug.Log($"set NEW {x},{z} to height {height}");
            myTerrain.terrainData.SetHeightsDelayLOD(x, z, h);
        }
    }

    void ClearTerrain(Terrain t, bool clearHoles, bool clearHeights, bool clearAlpha)
    {
        if (clearHoles)
        {
            var allHoles = new bool[t.terrainData.holesResolution, t.terrainData.holesResolution];
            for (int x = 0; x < t.terrainData.holesResolution; x++)
                for (int y = 0; y < t.terrainData.holesResolution; y++)
                    allHoles[x, y] = false;

            t.terrainData.SetHoles(0, 0, allHoles);
        }

        if (clearHeights)
        {
            var allHeights = new float[t.terrainData.heightmapResolution, t.terrainData.heightmapResolution];
            for (int x = 0; x < t.terrainData.heightmapResolution; x++)
                for (int y = 0; y < t.terrainData.heightmapResolution; y++)
                    allHeights[x, y] = 0f;

            t.terrainData.SetHeights(0, 0, allHeights);
        }

        if (clearAlpha)
        {
            var alphaMaps = new float[t.terrainData.alphamapWidth, t.terrainData.alphamapHeight, t.terrainData.alphamapLayers];
            for (int y = 0; y < t.terrainData.alphamapHeight; y++)
                for (int x = 0; x < t.terrainData.alphamapWidth; x++)
                    for (int l = 0; l < t.terrainData.alphamapLayers; l++)
                        alphaMaps[x, y, l] = 1f;

            t.terrainData.SetAlphamaps(0, 0, alphaMaps);
        }
    }

    private void SetTerrain(Vector3 point)
    {
        int terX = (int)((point.x / myTerrain.terrainData.size.x) * xResolution);
        int terZ = (int)((point.z / myTerrain.terrainData.size.z) * zResolution);

        float y = point.y * heightMultiplier;
        float[,] height = new float[1, 1];
        height[0, 0] = y;
        //heights[terX, terZ] = y;
        Debug.Log($"set {terX},{terZ} to height {y}");
        myTerrain.terrainData.SetHeightsDelayLOD(terX, terZ, height);

        terrainVerts.Add(new(terX, terZ, y));
    }
    
    private void SetTerrainSection(Vector3 point, int size)
    {
        point -= myTerrain.transform.position;
        int terX = (int)((point.x / myTerrain.terrainData.size.x) * xResolution)-size/2;
        int terZ = (int)((point.z / myTerrain.terrainData.size.z) * zResolution)-size/2;

        if (terX < 0) {
            if (myTerrain.leftNeighbor == null && !Terrain.activeTerrains.Any(trn => trn.GetPosition() == myTerrain.GetPosition() - new Vector3(myTerrain.terrainData.size.x,0,0)))
            {
                var terrainCopyGO = Instantiate(gameObject);
                var terrainCopy = terrainCopyGO.GetComponent<Terrain>();
                var terrainDeform = terrainCopyGO.GetComponent<TerrainDeformation>();

                terrainCopyGO.transform.Translate(new Vector3(-terrainCopy.terrainData.size.x, 0, 0));

                myTerrain.SetNeighbors(terrainCopy, myTerrain.topNeighbor, myTerrain.rightNeighbor, myTerrain.bottomNeighbor);

                terrainCopy.SetNeighbors(null, null, myTerrain, null);
                if(!terrainDeform.BuildTerrainOnStart)
                    terrainDeform.LoadTerrainFromData();
            }

            terX = 0;
        }
        if (terX > xResolution)
        {
            if (myTerrain.rightNeighbor == null && !Terrain.activeTerrains.Any(trn => trn.GetPosition() == myTerrain.GetPosition() + new Vector3(myTerrain.terrainData.size.x, 0, 0)))
            {
                var terrainCopyGO = Instantiate(gameObject);
                var terrainCopy = terrainCopyGO.GetComponent<Terrain>();
                var terrainDeform = terrainCopyGO.GetComponent<TerrainDeformation>();

                terrainCopyGO.transform.Translate(new Vector3(terrainCopy.terrainData.size.x, 0, 0));

                myTerrain.SetNeighbors(myTerrain.leftNeighbor, myTerrain.topNeighbor, terrainCopy, myTerrain.bottomNeighbor);
                terrainCopy.SetNeighbors(myTerrain, null, null, null);
                if (!terrainDeform.BuildTerrainOnStart)
                    terrainDeform.LoadTerrainFromData();
            }

            terX = xResolution;
        }
        if (terZ < 0)
        {
            if (myTerrain.bottomNeighbor == null && !Terrain.activeTerrains.Any(trn => trn.GetPosition() == myTerrain.GetPosition() - new Vector3(0, 0, myTerrain.terrainData.size.z)))
            {
                var terrainCopyGO = Instantiate(gameObject);
                var terrainCopy = terrainCopyGO.GetComponent<Terrain>();
                var terrainDeform = terrainCopyGO.GetComponent<TerrainDeformation>();

                terrainCopyGO.transform.Translate(new Vector3(0, 0, -terrainCopy.terrainData.size.z));

                myTerrain.SetNeighbors(myTerrain.leftNeighbor, myTerrain.topNeighbor, myTerrain.rightNeighbor, terrainCopy);
                terrainCopy.SetNeighbors(null, myTerrain, null, null);
                if (!terrainDeform.BuildTerrainOnStart)
                    terrainDeform.LoadTerrainFromData();
            }
            terZ = 0;
        }
        if (terZ > zResolution)
        {
            if (myTerrain.topNeighbor == null && !Terrain.activeTerrains.Any(trn => trn.GetPosition() == myTerrain.GetPosition() + new Vector3(0, 0, myTerrain.terrainData.size.z)))
            {
                var terrainCopyGO = Instantiate(gameObject);
                var terrainCopy = terrainCopyGO.GetComponent<Terrain>();
                var terrainDeform = terrainCopyGO.GetComponent<TerrainDeformation>();

                terrainCopyGO.transform.Translate(new Vector3(0, 0, terrainCopy.terrainData.size.z));

                myTerrain.SetNeighbors(myTerrain.leftNeighbor, terrainCopy, myTerrain.rightNeighbor, myTerrain.bottomNeighbor);
                terrainCopy.SetNeighbors(null, null, null, myTerrain);
                if (!terrainDeform.BuildTerrainOnStart)
                    terrainDeform.LoadTerrainFromData();
            }
            terZ = zResolution;
        }

        var heights = new float[size, size];//myTerrain.terrainData.GetHeights(terX, terZ, size, size);
        var holes = new bool[size, size];
        //float y = heights[0, 0];

        for (var x = 0; x < size; x++)
            for (var y = 0; y < size; y++)
            {
                heights[x, y] = point.y * heightMultiplier;
                holes[x, y] = true;
            }

        myTerrain.terrainData.SetHeightsDelayLOD(terX, terZ, heights);

        //var alphaX = (int)((point.x / myTerrain.terrainData.alphamapWidth) * myTerrain.terrainData.alphamapResolution);
        //var alphaZ = (int)((point.z / myTerrain.terrainData.alphamapHeight) * myTerrain.terrainData.alphamapResolution);

        //float[,,] alphaMaps = myTerrain.terrainData.GetAlphamaps(alphaX, alphaZ, size, size);
        //alphaMaps[0, 0, 0] = 1f;

        //myTerrain.terrainData.SetAlphamaps(alphaX, alphaZ, alphaMaps);
       
        
        myTerrain.terrainData.SetHolesDelayLOD(terX, terZ, holes);
    }

    //private void raiseTerrain(Vector3 point)
    //{
    //    int terX = (int)((point.x / myTerrain.terrainData.size.x) * xResolution);
    //    int terZ = (int)((point.z / myTerrain.terrainData.size.z) * zResolution);
    //    float y = heights[terX, terZ];
    //    y += 0.001f;
    //    float[,] height = new float[1, 1];
    //    height[0, 0] = y;
    //    heights[terX, terZ] = y;
    //    myTerrain.terrainData.SetHeights(terX, terZ, height);
    //}

    //private void lowerTerrain(Vector3 point)
    //{
    //    int terX = (int)((point.x / myTerrain.terrainData.size.x) * xResolution);
    //    int terZ = (int)((point.z / myTerrain.terrainData.size.z) * zResolution);
    //    float y = heights[terX, terZ];
    //    y -= 0.001f;
    //    float[,] height = new float[1, 1];
    //    height[0, 0] = y;
    //    heights[terX, terZ] = y;
    //    myTerrain.terrainData.SetHeights(terX, terZ, height);
    //}

    private void OnDrawGizmos()
    {
        foreach(var q in quads)
        {
            Gizmos.DrawLine(q.Item1, q.Item2);
            Gizmos.DrawLine(q.Item2, q.Item3);
            Gizmos.DrawLine(q.Item3, q.Item4);
            Gizmos.DrawLine(q.Item4, q.Item1);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TerrainDeformation))]
public class TerrainDeformationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Rebuild terrain"))
        {
            var script = (TerrainDeformation)target;
            script.LoadTerrainFromData();
        }
    }
}
#endif