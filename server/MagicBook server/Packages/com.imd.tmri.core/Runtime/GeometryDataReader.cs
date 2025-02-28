using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TMRI.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class GeometryDataReader : MonoBehaviour, IAssetListener
{
    public string assetID;
    public string dataPathOrURL;
    public bool fromStreamingAssets;
    public string optionalFilenameFormat;
    public int filenameFormatMaxFilesCount;
    public int startIndex = 0;
    public char delimiter = ',';
    public int numColumns;
    public int altitudeColumnIndex;
    public int floodDepthColumnIndex;
    public int lonLatOrderedQuadStartIndex;

    public float scaleMultiplier = 1f;
    public double referenceLatitude;
    public double referenceLongitude;
    public double referenceAltitude;
    public double referenceElevation;
    public double minLatitude;
    public double minLongitude;
    public double maxLatitude;
    public double maxLongitude;

    public float depthMultiplier = 1f;
    public GenerateMeshFromData GenerateMeshPrefab;
    public GameObject OverrideChunkedMeshPrefab;

    public List<string> DataUrls { get; set; } = new();

    public class GeometryQuad
    {
        public float depth;
        public float speed;
        public float elevation;
        public Vector3 p1;
        public Vector3 p2;
        public Vector3 p3;
        public Vector3 p4;
        public Vector3 center => new Vector3(
                (p1.x+p2.x+p3.x+p4.x)/4f,
                (p1.y+p2.y+p3.y+p4.y)/4f,
                (p1.z+p2.z+p3.z+p4.z)/4f
            );
    }

    public async void Start()
    {
        var minXZ = GeoCoordinateConverter.GeoToUnity(minLatitude, minLongitude, 0.0, referenceLatitude, referenceLongitude, referenceAltitude, scaleMultiplier);
        var maxXZ = GeoCoordinateConverter.GeoToUnity(maxLatitude, maxLongitude, 0.0, referenceLatitude, referenceLongitude, referenceAltitude, scaleMultiplier);
        var size = new Vector3(maxXZ.x - minXZ.x, float.PositiveInfinity, maxXZ.z - minXZ.z);
        var bounds = new Bounds(Vector3.zero, size);
        //Debug.Log($"Bounds: {bounds}");

        var numberOfFiles = DataUrls.Any() ? DataUrls.Count : 1;
        if (fromStreamingAssets)
        {
            var path = Path.Combine(Application.streamingAssetsPath, dataPathOrURL);
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!new Uri(path).IsFile)
#else
            if (Directory.Exists(path))
#endif
            {
                numberOfFiles = filenameFormatMaxFilesCount;
            }
        }

        for (int i=startIndex; i< numberOfFiles; i++)
        {
            var persistentDataPath = Application.persistentDataPath;
            //var quads = await Task.Run(() => ReadData(persistentDataPath, i), destroyCancellationToken);
            var quads = await ReadData(persistentDataPath, i);

            if (this == null)
                return;

            //Debug.Log($"Quads: {quads.Count}");
            if (!quads.Any())
                continue;

            var gm = Instantiate(GenerateMeshPrefab, transform);
            gm.gameObject.name = i.ToString();
            gm.quads = quads.Where(q => bounds.Contains(q.center)).ToList();
            gm.MinWorldXZ = minXZ;
            gm.MaxWorldXZ = maxXZ;
            gm.OverrideChunkedMeshPrefab = OverrideChunkedMeshPrefab;
            //Debug.Log($"Filtered quads: {gm.quads.Count}");

            gm.ReconstructOnStart = true;
        }
    }

    public void SetReferenceOrigin(Vector3 geographicOrigin)
    {

    }

    public async Task<List<GeometryQuad>> ReadData(string persistentDataPath, int currentFileIndex = 0)
    {
        var path = DataUrls.Any() ? DataUrls[currentFileIndex] : dataPathOrURL;
        if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
        {
            if (Regex.Match(path, @"(?:drive\.google\.com\/(?:file\/d\/|open\?id=))([^\/&]+)") is Match m && m.Success)
            {
                var fileId = m.Groups[1].Value;
                path = $"https://drive.google.com/uc?export=download&id={fileId}";
            }

            string[] l;
            if (AssetHandler.IsSaved(path, persistentDataPath))
            {
                var bytes = await AssetHandler.LoadFromPersistentData(path, destroyCancellationToken, persistentDataPath);
                l = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length).Split('\n');
            }
            else
            {
                l = await GetDataLinesFromURL(path);

                if (l.Length > 0)
                {
                    var forcedUTF8Encoding = System.Text.Encoding.UTF8.GetBytes(string.Join('\n', l));
                    _ = AssetHandler.SaveInPersistentData(path, forcedUTF8Encoding, destroyCancellationToken, persistentDataPath);
                }
            }
            return await ReadDataLines(l);
            
        }
        else //if (fromStreamingAssets)
        {
            path = Path.Combine(Application.streamingAssetsPath, path);
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!new Uri(path).IsFile)
#else
            if (Directory.Exists(path))
#endif
            {
                if (!string.IsNullOrEmpty(optionalFilenameFormat))
                {
                    var combinedFormat = Path.Combine(path, optionalFilenameFormat);
                    path = string.Format(combinedFormat, currentFileIndex % filenameFormatMaxFilesCount);
                    //currentFileIndex = (currentFileIndex + 1) % filenameFormatMaxFilesCount;
                }
                else
                {
                    var allFiles = Directory.EnumerateFiles(path);
                    path = allFiles.ElementAt(currentFileIndex % allFiles.Count());
                    //currentFileIndex = (currentFileIndex + 1) % allFiles.Count();
                }
                
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        var allLines = await GetDataLinesFromURL(path);
#else
        if (!File.Exists(path))
            return new List<GeometryQuad>();

        var allLines = await File.ReadAllLinesAsync(path, destroyCancellationToken);
#endif
        return await ReadDataLines(allLines);
    }

    async Task<string[]> GetDataLinesFromURL(string uri)
    {
        Debug.Log($"GeometryDataReader GET request to: {uri}");

        //return Task<string[]>.Run(async () =>
        //{
        using UnityWebRequest webRequest = UnityWebRequest.Get(uri);

        await webRequest.SendWebRequest();
        //var asyncOp = webRequest.SendWebRequest();
        //while (!asyncOp.isDone)
            //await Task.Yield();

        switch (webRequest.result)
        {
            case UnityWebRequest.Result.ConnectionError:
            case UnityWebRequest.Result.DataProcessingError:
            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError("Error: " + webRequest.error);
                return new string[0];
            case UnityWebRequest.Result.Success:
                //Debug.Log(":\nReceived: " + webRequest.downloadHandler.text.Length);

                return webRequest.downloadHandler.text.Split('\n');
        }
        return new string[0];

        //});
    }

    async Task<List<GeometryQuad>> ReadDataLines(string[] allLines)
    {
        //Debug.Log($"Reading {allLines.Length} lines of data...");
        var LoadedQuads = new List<GeometryQuad>();
        await Task.Run(() =>
        {
            foreach (var dataLine in allLines)
            {
                var d = dataLine.Split(delimiter);
                if (d.Length < Math.Max(lonLatOrderedQuadStartIndex + 7, Math.Max(altitudeColumnIndex, floodDepthColumnIndex)))
                    continue;

                if (!float.TryParse(d[altitudeColumnIndex], out float elevation) ||
                    !float.TryParse(d[floodDepthColumnIndex], out float floodDepth) ||
                    !float.TryParse(d[floodDepthColumnIndex+1], out float floodSpeed) ||
                    !float.TryParse(d[lonLatOrderedQuadStartIndex], out float p1Lon) ||
                    !float.TryParse(d[lonLatOrderedQuadStartIndex + 1], out float p1Lat) ||
                    !float.TryParse(d[lonLatOrderedQuadStartIndex + 2], out float p2Lon) ||
                    !float.TryParse(d[lonLatOrderedQuadStartIndex + 3], out float p2Lat) ||
                    !float.TryParse(d[lonLatOrderedQuadStartIndex + 4], out float p3Lon) ||
                    !float.TryParse(d[lonLatOrderedQuadStartIndex + 5], out float p3Lat) ||
                    !float.TryParse(d[lonLatOrderedQuadStartIndex + 6], out float p4Lon) ||
                    !float.TryParse(d[lonLatOrderedQuadStartIndex + 7], out float p4Lat))
                {
                    //Debug.LogWarning(dataLine);
                    continue;
                }

                var depth = elevation + floodDepth * depthMultiplier;
                var p1 = GeoCoordinateConverter.GeoToUnity(p1Lat, p1Lon, depth, referenceLatitude, referenceLongitude, referenceAltitude, scaleMultiplier);
                var p2 = GeoCoordinateConverter.GeoToUnity(p2Lat, p2Lon, depth, referenceLatitude, referenceLongitude, referenceAltitude, scaleMultiplier);
                var p3 = GeoCoordinateConverter.GeoToUnity(p3Lat, p3Lon, depth, referenceLatitude, referenceLongitude, referenceAltitude, scaleMultiplier);
                var p4 = GeoCoordinateConverter.GeoToUnity(p4Lat, p4Lon, depth, referenceLatitude, referenceLongitude, referenceAltitude, scaleMultiplier);

                LoadedQuads.Add(new GeometryQuad { p1 = p1, p2 = p2, p3 = p3, p4 = p4, depth = floodDepth, speed = floodSpeed, elevation = elevation});
            }
        });

        //OnLoadedQuads?.Invoke();
        return LoadedQuads;
    }

    public void OnAssetLoading(string assetID)
    {
        gameObject.SetActive(false);
    }

    public void OnAssetChanged(string assetID)
    {
        if (this.assetID == assetID)
            gameObject.SetActive(true);
    }

}


#if UNITY_EDITOR
[CustomEditor(typeof(GeometryDataReader))]
public class GeometryDataReaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Rebuild mesh"))
        {
            var script = (GeometryDataReader)target;
            //ReconstructAsync(script);
            foreach (Transform child in script.transform)
                Destroy(child.gameObject);

            script.Start();
        }
    }

    private async void ReconstructAsync(GeometryDataReader script)
    {
        Debug.Log("Start reconstruct");

        var i = 0;
        var minXZ = GeoCoordinateConverter.GeoToUnity(script.minLatitude, script.minLongitude, 0.0, script.referenceLatitude, script.referenceLongitude, script.referenceAltitude, script.scaleMultiplier);
        var maxXZ = GeoCoordinateConverter.GeoToUnity(script.maxLatitude, script.maxLongitude, 0.0, script.referenceLatitude, script.referenceLongitude, script.referenceAltitude, script.scaleMultiplier);
        var size = new Vector3(maxXZ.x - minXZ.x, float.PositiveInfinity, maxXZ.z - minXZ.z);
        var bounds = new Bounds(Vector3.zero, size);
        var quads = await script.ReadData(Application.persistentDataPath, i);
        var gm = Instantiate(script.GenerateMeshPrefab, script.transform);
        gm.gameObject.name = i.ToString();
        gm.quads = quads.Where(q => bounds.Contains(q.center)).ToList();

        //var cachedTimestep = script.CurrentTimeStep;//script.dataReader.currentFileIndex;
        //for (int i = script.CurrentTimeStep; i < script.dataReader.filenameFormatMaxFilesCount; i++)
        //{
        //    var tsk = script.dataReader.ReadData(currentFileIndex: i);
        //    var quads = await tsk;
        //    if (tsk.Exception != null)
        //        Debug.LogError(tsk.Exception);
        //    float minDepth = 0f;
        //    float maxDepth = 1f;
        //    var meshes = await Task.Run(() =>
        //        script.ReconstructMeshFromData(quads, script.WithinBounds, out minDepth, out maxDepth));

        //    script.ApplyMeshes(meshes, minDepth, maxDepth, i, script.transform.gameObject);
        //    //cachedTimestep++;
        //}
    }
}
#endif