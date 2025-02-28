using System.Collections.Generic;
using UnityEngine;


public class MapSizeCalculator : MonoBehaviour
{

    static private float length;
    static private float width;
    public class MeshData
    {
        public Vector3 Position;    
        public Vector3 Size;        

        public MeshData(Vector3 position, Vector3 size)
        {
            Position = position;
            Size = size;
        }
    }

   
    public void CalculateMapSize()
    {
        
        MeshRenderer[] allMeshes = GetComponentsInChildren<MeshRenderer>();
        if (allMeshes == null || allMeshes.Length == 0)
        {
            Debug.LogError("MapSizeCalculator.cs: No Mesh Renderers found in the children of " + gameObject.name);
            return;
        }

        List<MeshData> meshDataList = new List<MeshData>();
        foreach (var mesh in allMeshes)
        {
            
            Vector3 meshPosition = mesh.transform.position;

            
            Vector3 meshScale = mesh.transform.localScale;

            
            Vector3 meshBoundsSize = mesh.bounds.size;
            //Debug.Log($"{gameObject.name} Mesh Bounds Size: {meshBoundsSize}");

           
            meshDataList.Add(new MeshData(meshPosition, meshBoundsSize));
        }

        
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;
        MeshData minXMesh = null;
        MeshData maxXMesh = null;
        MeshData minZMesh = null;
        MeshData maxZMesh = null;

        

        
        foreach (var meshData in meshDataList)
        {
            Vector3 meshPosition = meshData.Position;
            if (meshPosition.x < minX)
            {
                minX = meshPosition.x;
                minXMesh = meshData;
            }
            if (meshPosition.x > maxX)
            {
                maxX = meshPosition.x;
                maxXMesh = meshData;
            }
            if (meshPosition.z < minZ)
            {
                minZ = meshPosition.z;
                minZMesh = meshData;
            }
            if (meshPosition.z > maxZ)
            {
                maxZ = meshPosition.z;
                maxZMesh = meshData;
            }
        }


        
        float totalMapLength = (maxX + maxXMesh.Size.x / 2) - (minX - minXMesh.Size.x / 2);
        float totalMapWidth = (maxZ + maxZMesh.Size.z / 2) - (minZ - minZMesh.Size.z / 2);

        //Debug.Log($"{gameObject.name} MinX Mesh Position: {minXMesh.Position.x}, MinX Mesh Size: {minXMesh.Size.x}");
        //Debug.Log($"{gameObject.name} MaxX Mesh Position: {maxXMesh.Position.x}, MaxX Mesh Size: {maxXMesh.Size.x}");
        //Debug.Log($"{gameObject.name} MinZ Mesh Position: {minZMesh.Position.z}, MinZ Mesh Size: {minZMesh.Size.z}");
        //Debug.Log($"{gameObject.name} MaxZ Mesh Position: {maxZMesh.Position.z}, MaxZ Mesh Size: {maxZMesh.Size.z}");

        //Debug.Log($"{gameObject.name} Total Map Length: " + totalMapLength);
        //Debug.Log($"{gameObject.name} Total Map Width: " + totalMapWidth);
        length = Mathf.Round(totalMapLength);
        width = Mathf.Round(totalMapWidth);
    }

    public float GetLength()
    {
        return length;
    }

    public float GetWidth()
    {
        return width;
    }

    public float AveragePositionHeight()
    {
        MeshRenderer[] allMeshes = GetComponentsInChildren<MeshRenderer>();
        if (allMeshes == null || allMeshes.Length == 0)
        {
            Debug.LogError("MapSizeCalculator.cs: No Mesh Renderers found in the children of " + gameObject.name);
            return 0;
        }
        float totalHeight = 0;
        foreach (var mesh in allMeshes)
        {
            totalHeight += mesh.transform.position.y;
        }
        Debug.Log($"{gameObject.name} AVERAGE POSITION Height: {totalHeight/allMeshes.Length}");
        return totalHeight / allMeshes.Length;
    }
}
