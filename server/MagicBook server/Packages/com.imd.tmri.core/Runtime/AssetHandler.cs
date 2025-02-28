using GLTFast;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TMRI.Core
{
    [RequireComponent(typeof(GoogleDriveDownloader))]
    public class AssetHandler : MonoBehaviour
    {
        const string CONFIG_KEY = "AssetHandler.SerializedConfig";

        private Dictionary<string, AssetBundle> _availableAssets = new();
        private Dictionary<string, bool> AssetStatus = new();
        private Dictionary<string, GameObject> _loadedAssets = new();
        private GoogleDriveDownloader _googleDriveDownloader;
        private SerializableConfigFile _config = null;
        public string currentPlatform = null;
        public GenerateMeshFromData meshPrefab;
        public GameObject chunkedMeshPrefab;

        private const double VISIONOS_MEMORY_TRIANGLE_LIMIT = 1E6;
        private const double VISIONOS_RUNTIME_TRIANGLE_LIMIT = 2E5;

        //public static AssetHandler Instance { get; private set; }
        public Action<SerializableConfigFile> OnConfigurationLoaded;
        public Action OnConfigurationError;

        private bool IsNotNullAndEmpty(string str) => !string.IsNullOrEmpty(str);

        public bool HasConfig => _config != null;
        public List<SerializableTrackingmarker> ReturnTrackingmarkersFromConfig() =>
            _config?.markers ?? new List<SerializableTrackingmarker>();

        public List<SerializableAsset> ReturnAssetsFromConfig() =>
            _config?.assets ?? new List<SerializableAsset>();

        public Vector3 ReturnDefaultDisplaySize() => _config?.display_size;

        public string ConvertToDownloadLink(string link)
        {
            if (_googleDriveDownloader)
                return _googleDriveDownloader.ConvertDriveDownloadLink(link);

            return link;
        }

        public IEnumerable<ModelLOD> ReturnValidModelLODs(SerializableAsset assetConfig)
        {
            IEnumerable<ModelLOD> orderedLODs = assetConfig.models;
#if UNITY_VISIONOS
            int totalTriangles = 0;
            orderedLODs = orderedLODs.OrderBy(m => m.lodLevel).TakeWhile(m => (totalTriangles += m.triangles) < VISIONOS_MEMORY_TRIANGLE_LIMIT).OrderByDescending(m => m.lodLevel);
#else
            orderedLODs = orderedLODs.OrderByDescending(m => m.lodLevel).Take(1);
#endif
            return orderedLODs;
        }

        public bool HasCache(out SerializableConfigFile cachedConfig)
        {
            cachedConfig = null;
            if (PlayerPrefs.HasKey(CONFIG_KEY))
            {
                cachedConfig = JsonConvert.DeserializeObject<SerializableConfigFile>(PlayerPrefs.GetString(CONFIG_KEY));
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool HasCache(SerializableConfigFile config, out Dictionary<string,bool> trackingMarker, out Dictionary<string, bool> model)
        {
            trackingMarker = new();
            model = new();
            foreach(var m in config.markers)
            {
                trackingMarker[m.name] = IsSaved($"{m.name}_{m.link}");
            }

            foreach (var asset in config.assets.Where(a => a.HasModelLODs).Select(a => (a.id, ReturnValidModelLODs(a))))
            {
                foreach (var m in asset.Item2)
                {
                    var saved = false;
                    if (Uri.IsWellFormedUriString(m.url, UriKind.Absolute))
                        saved = IsSaved($"{asset.id}_{m.url}");
                    else
                        saved = File.Exists(Path.Combine(Application.streamingAssetsPath, m.url));

                    model[$"{asset.id} LOD{m.lodLevel}"] = saved;
                }
            }
            
            return trackingMarker.All(m => m.Value) && model.All(m => m.Value);
        }

        public enum BuildPlatform
        {
            StandaloneOSX,
            StandaloneWindows,
            iOS,
            Android,
            WSAPlayer,
            VisionOS
        }

        public static string GetRuntimePlatform()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return BuildPlatform.StandaloneOSX.ToString();
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return BuildPlatform.StandaloneWindows.ToString();
                case RuntimePlatform.IPhonePlayer:
                    return BuildPlatform.iOS.ToString();
                case RuntimePlatform.Android:
                    return BuildPlatform.Android.ToString();
                case RuntimePlatform.WSAPlayerARM:
                case RuntimePlatform.WSAPlayerX64:
                case RuntimePlatform.WSAPlayerX86:
                    return BuildPlatform.WSAPlayer.ToString();
                case (RuntimePlatform)50: //RuntimePlatform.VisionOS
                    return BuildPlatform.VisionOS.ToString();
                default:
                    Debug.LogWarning($"No mapping for platform {Application.platform} with ID {(int)Application.platform}!");
                    return "NoTarget"; //BuildTarget.NoTarget.ToString();
            }
        }

        public void StartWithConfigURL(string configurationFileURI)
        {
            currentPlatform = GetRuntimePlatform();
            Debug.Log($"AssetHandler: Detected platform is {currentPlatform}.");

            if (Uri.IsWellFormedUriString(configurationFileURI, UriKind.Absolute))
            {
                StartCoroutine(LoadConfigFileFromWeb(configurationFileURI));
            }
            else // Assume local streaming assets folder
            {
                var path = Path.Combine(Application.streamingAssetsPath, configurationFileURI);

#if UNITY_ANDROID && !UNITY_EDITOR
                StartCoroutine(LoadConfigFileFromWeb(path));
#else
                LoadConfigFileFromLocal(path);
#endif
            }

        }

        private void LoadConfigFileFromCache()
        {
            Debug.Log("AssetHandler: Loading configuration from cache");
            if (PlayerPrefs.HasKey(CONFIG_KEY))
            {
                _config = JsonConvert.DeserializeObject<SerializableConfigFile>(PlayerPrefs.GetString(CONFIG_KEY));
                OnConfigurationLoaded?.Invoke(_config);
            }
            else
            {
                Debug.LogWarning("AssetHandler: No cached configuration found!");
            }
        }

        private void OnEnable()
        {
            OnConfigurationLoaded += DoWhenConfigLoaded;
            OnConfigurationError += LoadConfigFileFromCache;
        }

        private void OnDisable()
        {
            OnConfigurationLoaded -= DoWhenConfigLoaded;
            OnConfigurationError -= LoadConfigFileFromCache;
        }

        private void DoWhenConfigLoaded(SerializableConfigFile _)
        {
            _googleDriveDownloader = GetComponent<GoogleDriveDownloader>();
            _googleDriveDownloader.Initialize(this);

            foreach (var marker in _config.markers)
            {
                if (IsNotNullAndEmpty(marker.defaultToLoad) && _config.assets.Any(asset => asset.id == marker.defaultToLoad))
                {
                    // Uncomment the following if async pre-loading the data (before finding the marker) is desired
                    //GetAsset(marker.defaultToLoad);
                }
            }
        }

        private IEnumerator LoadConfigFileFromWeb(string uri)
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            webRequest.timeout = 5;

            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + webRequest.error);
                OnConfigurationError?.Invoke();
            }
            else
            {
                _config = JsonConvert.DeserializeObject<SerializableConfigFile>(webRequest.downloadHandler.text);
                PlayerPrefs.SetString(CONFIG_KEY, webRequest.downloadHandler.text);

                OnConfigurationLoaded?.Invoke(_config);
            }
        }

        private void LoadConfigFileFromLocal(string path)
        {
            try
            {
                var fileText = File.ReadAllText(path);

                _config = JsonConvert.DeserializeObject<SerializableConfigFile>(fileText);
                PlayerPrefs.SetString(CONFIG_KEY, fileText);

                OnConfigurationLoaded?.Invoke(_config);
            }
            catch(Exception ex)
            {
                Debug.LogError(ex.Message);
                OnConfigurationError?.Invoke();
            }
        }

        public async Task<GameObject> GetAsset(string name)
        {
            var assetConfig = _config.assets.FirstOrDefault(asset => asset.id == name);
            if (assetConfig == null)
            {
                Debug.LogError("Asset in configuration not found.");
                return null;
            }

            if (_loadedAssets.TryGetValue(name, out GameObject asset) && asset != null)
            {
                //TODO should AssetHandler really keep a reference to the loadedAssets as well?
                return asset;
            }

            AssetStatus[name] = true;

            if(assetConfig.HasModelLODs)
            {
                var assetParent = new GameObject($"LOD_group_{name}");
                var loadingTasks = new List<(ModelLOD, Task<GameObject>)>();
                var orderedLODs = ReturnValidModelLODs(assetConfig);
                
                foreach (var m in orderedLODs)
                    loadingTasks.Add((m, LoadGLTFFromUrl(name, m.url)));

                await Task.WhenAll(loadingTasks.Select(t => t.Item2));

                var succesfulTasks = loadingTasks.Where(t => t.Item2.IsCompletedSuccessfully).ToList();
                var lodIndex = 0;
                foreach (var lodAndTask in succesfulTasks)
                {
                    // Load glTF model
                    // TODO load byte stream, check mime/file type and based on it use glTFast (.glb, .gltf) or TriLib (.obj, .fbx, etc.)

                    var modelLOD = lodAndTask.Item1;
                    var go = lodAndTask.Item2.Result;
                    if (go == null)
                        continue;

                    go.transform.parent = assetParent.transform;

                    if(modelLOD.originLatLonAlt != null)
                    {
                        // Implies that origins of the LOD is not the same as the main asset data, so apply the position offset
                        var offset = GeoCoordinateConverter.GeoToUnity(
                            modelLOD.originLatLonAlt.x,
                            modelLOD.originLatLonAlt.y,
                            modelLOD.originLatLonAlt.z,
                            assetConfig.originLatLonAlt.x,
                            assetConfig.originLatLonAlt.y,
                            assetConfig.originLatLonAlt.z,
                            scale: 1);

                        go.transform.localPosition = new Vector3(-offset.x, 0, -offset.z);
                    }

                    var lodBehaviour = go.AddComponent<ModelLODBehaviour>();
                    lodBehaviour.screenVisibleFraction = modelLOD.screenVisibleFraction;
                    lodBehaviour.lodIndex = lodIndex;
                    lodBehaviour.triangles = modelLOD.triangles;
                    lodBehaviour.disableCulling = succesfulTasks.Count == 1;

                    lodIndex++;
                }

                _loadedAssets[name] = assetParent;
                AssetStatus[name] = false;
                return assetParent;
            }
            else if(assetConfig.assetBundles?.Any() ?? false)
            {
                // Try to find an assetbundle with the same platform
                var assetPlatformLink = assetConfig.assetBundles.FirstOrDefault(apl => apl.platforms.Contains(currentPlatform));
                if(assetPlatformLink == null)
                {
                    Debug.LogWarning($"AssetHandler: Asset with name {name} is missing a downloadlink for the platform {currentPlatform}.");
                    AssetStatus[name] = false;

                    return null;
                }

                _googleDriveDownloader.DownloadAssetBundle(assetPlatformLink, name);

                while (AssetIsCurrentlyProcessing(name))
                {
                    await Task.Delay(10);
                }

                return _loadedAssets[name];
            }

            return null;
        }

        public async Task<List<Landmark>> GetAssetLandmarks(string assetID)
        {
            if (_config == null || _config.assets == null)
                return new();

            var asset = _config.assets.FirstOrDefault(a => a.id == assetID);

            if (asset == null)
                return new();

            if (asset.originLatLonAlt == null)
                return new();

            var returnList = new List<Landmark>();
            var iconDict = new Dictionary<int, Texture2D>();

            if (_config.landmarkTypes?.Any() ?? false)
            {
                var iconTexTasks = new Dictionary<int, Task<Texture2D>>();
                foreach(var lmt in _config.landmarkTypes)
                {
                    iconTexTasks[lmt.id] = LoadTexture(ConvertToDownloadLink(lmt.iconUrl), destroyCancellationToken);
                }
                await Task.WhenAll(iconTexTasks.Values);
                foreach(var lmt in iconTexTasks.Where(t => t.Value.IsCompletedSuccessfully))
                {
                    iconDict[lmt.Key] = lmt.Value.Result;
                }
            }

            if (!string.IsNullOrEmpty(asset.landmarkListUrl))
            {
                var landMarkLines = await ReadCSVData(asset.landmarkListUrl);
                foreach(var lmLine in landMarkLines)
                {
                    var data = lmLine.Split(',');
                    if (data.Length != 4)
                        continue;
                    if (!(double.TryParse(data[1], out double lat) && double.TryParse(data[2], out double lng)))
                        continue;

                    var icons = new List<int>();
                    if (!string.IsNullOrEmpty(data[3]))
                        foreach (var iconStr in data[3].Split(';'))
                        {
                            if(int.TryParse(iconStr, out int icon))
                                icons.Add(icon);
                        }

                    var unityLandmark = new Landmark
                    {
                        label = data[0],
                        icons = iconDict.Where(i => icons.Contains(i.Key)).ToDictionary(i => i.Key, i => i.Value),
                        iconTypes = _config.landmarkTypes.Any()
                            ? _config.landmarkTypes.Where(lt => icons.Contains(lt.id)).ToList()
                            : icons.Select(i => new SerializableLandmarkType { id = i }).ToList(),
                        position = Vector3.Scale(new Vector3(-1, 1, -1), GeoCoordinateConverter.GeoToUnity(
                            lat,
                            lng,
                            asset.originLatLonAlt.z, //altitude is same as the Y offset to the origin
                            asset.originLatLonAlt.x,
                            asset.originLatLonAlt.y,
                            asset.originLatLonAlt.z,
                            asset.conversionValue)
                        )
                    };

                    returnList.Add(unityLandmark);
                }
            }

            int i = 0;
            foreach (var landmark in asset.landmarks)
            {
                if (string.IsNullOrEmpty(landmark.label) || landmark.position == null)
                {
                    Debug.LogError($"Landmark {i}'s label and/or position is empty or invalid! Skipping...");
                    continue;
                }
                var geomToUnityLandmark = new Landmark
                {
                    label = landmark.label,
                    level = landmark.level,
                    icons = iconDict.Where(i => landmark.icons.Contains(i.Key)).ToDictionary(i => i.Key, i => i.Value),
                    iconTypes = _config.landmarkTypes.Any() ? _config.landmarkTypes.Where(lt => landmark.icons.Contains(lt.id)).ToList() : landmark.icons.Select(i => new SerializableLandmarkType { id = i }).ToList(),
                    position = Vector3.Scale(new Vector3(-1,1,-1), GeoCoordinateConverter.GeoToUnity(
                    landmark.position.x,
                        landmark.position.y,
                        landmark.position.z,
                        asset.originLatLonAlt.x,
                        asset.originLatLonAlt.y,
                        asset.originLatLonAlt.z,
                        asset.conversionValue)),
                    referenceAssetId = landmark.referenceAssetId
                };

                i++;

                returnList.Add(geomToUnityLandmark);
            }

            return returnList;
        }

        public async Task<List<MapProjector>> LoadGeoImages(string assetID, GameObject mapProjectorPrefab, Transform parent)
        {
            if (_config == null || _config.assets == null)
                return new();

            var asset = _config.assets.FirstOrDefault(a => a.id == assetID);

            if (asset == null)
                return new();

            var geoImages = asset.geoImages ?? new List<SerializableGeoImage>();
            var returnList = new List<MapProjector>();
            foreach (var geoImage in geoImages)
            {
                var imageTask = LoadTexture(ConvertToDownloadLink(geoImage.imageUrl), destroyCancellationToken);
                var legendTask = string.IsNullOrEmpty(geoImage.legendImageUrl)
                    ? Task.CompletedTask
                    : LoadTexture(ConvertToDownloadLink(geoImage.legendImageUrl), destroyCancellationToken);

                await Task.WhenAll(imageTask, legendTask);
                
                if (!imageTask.IsCompletedSuccessfully)
                    continue;

                var go = Instantiate(mapProjectorPrefab, parent);
                if (go.TryGetComponent(out MapProjector mapProjector))
                {
                    mapProjector.ID = geoImage.id;
                    mapProjector.minLatLon = new Vector2(geoImage.minLatLon.x, geoImage.minLatLon.y);
                    mapProjector.maxLatLon = new Vector2(geoImage.maxLatLon.x, geoImage.maxLatLon.y);
                    mapProjector.referenceLatLonAlt = asset.originLatLonAlt;
                    mapProjector.assetID = assetID;
                    mapProjector.imageTexture = imageTask.Result;
                    mapProjector.opacity = Mathf.Clamp01(geoImage.opacity);
                    mapProjector.configureOnStart = true;

                    if(legendTask is Task<Texture2D> lt && lt.IsCompletedSuccessfully)
                        mapProjector.legendTexture = lt.Result;

                    returnList.Add(mapProjector);
                }
            }
            return returnList;
        }

        public bool FillGeometryData(string assetID, Transform container)
        {
            if (_config == null || _config.assets == null)
                return false;

            var asset = _config.assets.FirstOrDefault(a => a.id == assetID);

            if (asset == null)
                return false;

            if (asset.originLatLonAlt == null || asset.minLatLonAlt == null || asset.maxLatLonAlt == null)
                return false;

            if (asset.geoData == null || !asset.geoData.dataUrls.Any())
                return false;

            var go = new GameObject(assetID);
            go.transform.SetParent(container, false);

            var geomDataComponent = go.AddComponent<GeometryDataReader>();
            geomDataComponent.assetID = assetID;
            geomDataComponent.DataUrls = asset.geoData.dataUrls;
            geomDataComponent.delimiter = asset.geoData.delimiter;
            geomDataComponent.filenameFormatMaxFilesCount = asset.geoData.numFiles;
            geomDataComponent.startIndex = asset.geoData.fileStartIndex;
            geomDataComponent.numColumns = asset.geoData.numColumns;
            geomDataComponent.floodDepthColumnIndex = asset.geoData.dataValueColumnIndex;
            geomDataComponent.altitudeColumnIndex = asset.geoData.elevationColumnIndex;
            geomDataComponent.referenceLatitude = asset.originLatLonAlt.x;
            geomDataComponent.referenceLongitude = asset.originLatLonAlt.y;
            geomDataComponent.referenceAltitude = asset.originLatLonAlt.z;
            geomDataComponent.minLatitude = asset.minLatLonAlt.x;
            geomDataComponent.minLongitude = asset.minLatLonAlt.y;
            geomDataComponent.maxLatitude = asset.maxLatLonAlt.x;
            geomDataComponent.maxLongitude = asset.maxLatLonAlt.y;
            geomDataComponent.optionalFilenameFormat = asset.geoData.filenameFormat;
            geomDataComponent.lonLatOrderedQuadStartIndex = asset.geoData.latLngShapeStartColumnIndex;
            geomDataComponent.GenerateMeshPrefab = meshPrefab;
            geomDataComponent.OverrideChunkedMeshPrefab = chunkedMeshPrefab;

            go.transform.localEulerAngles = new Vector3(asset.geoData.xDegreesOffset, 0f, 0f);

            return true;
        }

        public async Task<List<SerializablePanorama>> GetAssetPanoramas(string assetID)
        {
            if (_config == null || _config.assets == null)
                return new();

            var asset = _config.assets.FirstOrDefault(a => a.id == assetID);

            if (asset == null)
                return new();

            if (asset.originLatLonAlt == null)
                return asset.panoramas;

            var returnList = new List<SerializablePanorama>();

            foreach (var panorama in asset.panoramas)
            {
                if ((string.IsNullOrEmpty(panorama.spherical360ImageUrl) || panorama.position == null) && !panorama.HasMultiplePanoramas)
                    continue;

                if (panorama.HasMultiplePanoramas)
                {
                    var allPanoramas = await GetPanoramasFromUrl(panorama.multiplePanoramasUrl);
                    foreach(var p in allPanoramas)
                        returnList.Add(GeoToUnityPanorama(p, asset));
                }
                else
                {
                    returnList.Add(GeoToUnityPanorama(panorama, asset));
                }
            }

            return returnList;
        }

        SerializablePanorama GeoToUnityPanorama(SerializablePanorama panorama, SerializableAsset asset)
        {
            Debug.Log($"Converting {panorama.position} using {asset.originLatLonAlt}");
            return new SerializablePanorama
            {
                spherical360ImageUrl = ConvertToDownloadLink(panorama.spherical360ImageUrl),
                rotationFromNorthDegrees = panorama.rotationFromNorthDegrees,
                offsetInVR = panorama.offsetInVR,
                position = (SerializableVector3)Vector3.Scale(
                                new Vector3(-1, 1, -1),
                                GeoCoordinateConverter.GeoToUnity(
                                    panorama.position.x,
                                    panorama.position.y,
                                    panorama.position.z,
                                    asset.originLatLonAlt.x,
                                    asset.originLatLonAlt.y,
                                    asset.originLatLonAlt.z,
                                    asset.conversionValue)
                                ),
                modelUrl = panorama.modelUrl,
                depthImageUrl = ConvertToDownloadLink(panorama.depthImageUrl),
                invertDepth = panorama.invertDepth
            };
        }

        public async Task<GameObject> LoadGLTFFromUrl(string name, string modelUrl)
        {
            var go = new GameObject(name);
            var fromStreamingAssets = !Uri.IsWellFormedUriString(modelUrl, UriKind.Absolute);
            var gltfAsset = go.AddComponent<GltfAsset>();
            gltfAsset.StreamingAsset = fromStreamingAssets;
            gltfAsset.LoadOnStartup = false;

            var identifier = $"{name}_{modelUrl}";
            bool success;
            if (fromStreamingAssets)
            {
                gltfAsset.Url = modelUrl;
                success = await gltfAsset.Load(gltfAsset.FullUrl);
            }
            else
            {
                byte[] fileBytes;
                modelUrl = ConvertToDownloadLink(modelUrl);

                if (IsSaved(identifier))
                    fileBytes = await LoadFromPersistentData(identifier);
                else
                {
                    fileBytes = await _googleDriveDownloader.DownloadFileFromGoogleDrive(modelUrl);
                    var saveTask = SaveInPersistentData(identifier, fileBytes);
                    await saveTask;
                    if (saveTask.Exception != null)
                        Debug.LogError(saveTask.Exception);
                }

                var importer = new GltfImport();
                success = await importer.LoadGltfBinary(fileBytes, new Uri(modelUrl), cancellationToken: destroyCancellationToken);

                if(success)
                    success = await importer.InstantiateMainSceneAsync(go.transform, cancellationToken: destroyCancellationToken);
            }
            
            if (!success)
            {
                Debug.LogError($"Something went wrong downloading gltf from '{gltfAsset.FullUrl}'");
                return null;
            }

            return go;
        }

        public async Task<bool> SaveInPersistentData(string identifier, byte[] blobToSave)
        {
            return await SaveInPersistentData(identifier, blobToSave, destroyCancellationToken, Application.persistentDataPath);
        }

        public static async Task<bool> SaveInPersistentData(string identifier, byte[] blobToSave, CancellationToken cancellationToken, string persistentDataPath)
        {
            var hash = identifier.GetHashCode().ToString();
            var path = Path.Combine(persistentDataPath, hash);
            Debug.Log($"Saving {identifier}...");
            var t = File.WriteAllBytesAsync(path, blobToSave, cancellationToken);
            await t;
            Debug.Log($"Saved {identifier} at '{path}' {(t.IsCompletedSuccessfully ? "succesfully" : "failed")}");
            return t.IsCompletedSuccessfully;
        }

        public async Task<byte[]> LoadFromPersistentData(string identifier)
        {
            return await LoadFromPersistentData(identifier, destroyCancellationToken, Application.persistentDataPath);
        }

        public static async Task<byte[]> LoadFromPersistentData(string identifier, CancellationToken cancellationToken, string persistentDataPath)
        {
            var hash = identifier.GetHashCode().ToString();
            var path = Path.Combine(persistentDataPath, hash);
            Debug.Log($"Loading {identifier} at '{path}'");

            return await File.ReadAllBytesAsync(path, cancellationToken);
        }
        public bool IsSaved(string identifier)
        {
            return IsSaved(identifier, Application.persistentDataPath);
        }
        public static bool IsSaved(string identifier, string persistentDataPath)
        {
            var hash = identifier.GetHashCode().ToString();
            var path = Path.Combine(persistentDataPath, hash);
            var success = File.Exists(path);
            Debug.Log($"Checking '{path}'... Exists in storage: {success}");
            return success;
        }

        public bool AssetIsCurrentlyProcessing(string name) =>
            AssetStatus.ContainsKey(name) && AssetStatus[name];

        public bool IsAnyAssetProcessing() => AssetStatus.Any(a => a.Value == true);

        public void OnAssetBundleDownloaded(string name, AssetBundle assetBundle)
        {
            if (assetBundle != null)
            {
                _availableAssets[name] = assetBundle;
                StartCoroutine(LoadAssetsFromBundle(name, assetBundle));
            }
            else
            {
                Debug.LogError($"Failed to download AssetBundle: {name}");
            }
        }

        private IEnumerator LoadAssetsFromBundle(string name, AssetBundle assetBundle)
        {
            string[] assetNames = assetBundle.GetAllAssetNames();
            int batchSize = 5; // Number of assets to load per frame
            int assetCount = assetNames.Length;
            var assetsParent = new GameObject(name);

            for (int i = 0; i < assetCount; i++)
            {
                var assetRequest = assetBundle.LoadAssetAsync<GameObject>(assetNames[i]);
                yield return assetRequest;

                if (assetRequest.asset is GameObject asset)
                {
                    Instantiate(asset, assetsParent.transform);
                }
                else
                {
                    Debug.LogWarning($"Asset {assetNames[i]} is not a GameObject.");
                }

                // Yield control back to Unity every batchSize assets
                if ((i + 1) % batchSize == 0)
                {
                    yield return null;
                }
            }

            assetBundle.Unload(false);

            _loadedAssets[name] = assetsParent;

            Debug.Log($"{name} assets loaded and ready.");
            AssetStatus[name] = false;
        }

        private void OnDestroy()
        {

            AssetBundle.UnloadAllAssetBundles(false);
        }

        public bool GetAssetOrigin(string assetID, out Vector3 geographicOrigin)
        {
            geographicOrigin = Vector3.zero;

            if (_config == null || _config.assets == null)
                return false;

            var asset = _config.assets.FirstOrDefault(a => a.id == assetID);

            if (asset == null)
                return false;

            if (asset.originLatLonAlt == null)
                return false;

            geographicOrigin = asset.originLatLonAlt;
            return true;
        }

        async Task<List<SerializablePanorama>> GetPanoramasFromUrl(string url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                var panorama = await GetPanoramaWeb(url);
                return new List<SerializablePanorama> { panorama };
            }
            else // Assume local streaming assets folder
            {
                var path = Path.Combine(Application.streamingAssetsPath, url);

#if UNITY_ANDROID && !UNITY_EDITOR
                var panorama = await GetPanoramaWeb(path);
                return new List<SerializablePanorama> { panorama };
#else
                return await GetPanoramasLocal(path);
#endif
            }
        }

        async Task<SerializablePanorama> GetPanoramaWeb(string url)
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.timeout = 5;

            var asyncOp = webRequest.SendWebRequest();

            while (!asyncOp.isDone)
                await Task.Yield();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + webRequest.error);
                return null;
            }
            else
            {
                return JsonConvert.DeserializeObject<SerializablePanorama>(webRequest.downloadHandler.text);
            }
        }

        async Task<List<SerializablePanorama>> GetPanoramasLocal(string path)
        {
            var returnList = new List<SerializablePanorama>();
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!new Uri(path).IsFile)
#else
            if (Directory.Exists(path))
#endif
            {
                var allMetaFiles = Directory.GetFiles(path, "*.json");
                foreach(var metaFile in allMetaFiles)
                {
                    var text = await File.ReadAllTextAsync(metaFile, destroyCancellationToken);
                    var panoramaMeta = JsonConvert.DeserializeObject<PanoramaMetadata>(text);
                    var name = Path.GetFileNameWithoutExtension(panoramaMeta.filename);
                    returnList.Add(new SerializablePanorama
                    {
                        depthImageUrl = Path.Combine(path, $"{name}.depthmap.{panoramaMeta.fileType}"),
                        rotationFromNorthDegrees = panoramaMeta.rotation,
                        spherical360ImageUrl = Path.Combine(path, panoramaMeta.filename),
                        position = new SerializableVector3(panoramaMeta.lat, panoramaMeta.lng, panoramaMeta.elevation),
                        offsetInVR = new SerializableVector3(0, 0, 0)
                    });
                }
            }

            return returnList;
        }

        public bool ConfigureLODGroup(LODGroup lodGroup, GameObject instantiatedObject, Action<IEnumerable<MeshRenderer>> onIncludedRenderer = null)
        {
            var lodGOs = instantiatedObject.GetComponentsInChildren<ModelLODBehaviour>();
            var lods = new LOD[lodGOs.Length];

            var i = 0;
            foreach(var lodBehaviour in lodGOs.OrderBy(l => l.lodIndex))
            {
                //var child = instantiatedObject.transform.GetChild(i);
                var meshRenderers = lodBehaviour.GetComponentsInChildren<MeshRenderer>();
                onIncludedRenderer?.Invoke(meshRenderers);

                var visibleFractionFromSettings = lodBehaviour.screenVisibleFraction;//child.GetComponent<ModelLODBehaviour>().screenVisibleFraction;
                
                if (visibleFractionFromSettings == 0f)//lodsNotSetup)
                    lods[i] = new LOD(1f - (1f / lods.Length * (i+1)), meshRenderers);
                else
                    lods[i] = new LOD(visibleFractionFromSettings, meshRenderers);
                i++;
            }

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();

            var camFovFraction = Camera.main.fieldOfView / 90f;
            lodGroup.size *= camFovFraction *
#if UNITY_VISIONOS && !UNITY_EDITOR
                1.0f;
#else
                0.1f;
#endif

            Debug.Log($"Set up {lods.Length} LODs with size {lodGroup.size} (cam: {camFovFraction})");
            return lods.Length > 0;
        }

        internal class PanoramaMetadata
        {
            public string filename;
            public string fileType;
            public float lat;
            public float lng;
            public float elevation;
            public float rotation;
        }

        public class ModelLODBehaviour : MonoBehaviour
        {
            public float screenVisibleFraction;
            public int lodIndex;
            public bool disableCulling;
            public int triangles;
            public int triangleThreshold = (int)VISIONOS_RUNTIME_TRIANGLE_LIMIT;

            Dictionary<MeshRenderer, BoxCollider> renderersCollidersReference = new();

            private void LateUpdate()
            {
                if (lodIndex != 0 || disableCulling)
                    return;

                if(renderersCollidersReference.Count == 0 || renderersCollidersReference.Any(rc => rc.Value == null))
                {
                    foreach(var mr in GetComponentsInChildren<MeshRenderer>(true))
                    {
                        renderersCollidersReference[mr] = mr.GetComponent<BoxCollider>();
                    }
                }

                var allMeshRenderers = renderersCollidersReference.Where(rc => rc.Value != null);//GetComponentsInChildren<MeshRenderer>(true);
                var allMeshRenderersCount = allMeshRenderers.Count();
                if (allMeshRenderersCount == 0)
                    return;

                var visibleMeshRenderers = allMeshRenderers.Where(rc => rc.Key.enabled &&
                    Camera.main.WorldToViewportPoint(rc.Value.ClosestPointOnBounds(Camera.main.transform.position)) is Vector3 v &&
                    v.x == Mathf.Clamp01(v.x) && v.y == Mathf.Clamp01(v.y) && v.z > 0f);

                var trianglesPerMesh = triangles / allMeshRenderersCount;
                var visibleTriangles = trianglesPerMesh * visibleMeshRenderers.Count();

                if (visibleTriangles > triangleThreshold)
                {
                    var camPos = Camera.main.transform.position;
                    foreach (var rc in visibleMeshRenderers.OrderByDescending(m => (camPos-m.Key.transform.position).sqrMagnitude))
                    {
                        rc.Key.enabled = false;
                        visibleTriangles -= trianglesPerMesh;

                        if (visibleTriangles < triangleThreshold)
                            break;
                    }
                }
            }
        }

        public static async Task<Texture2D> LoadTexture(string texturePath, CancellationToken cancellationToken)
        {
            var tex = new Texture2D(2, 2);

            if (Uri.IsWellFormedUriString(texturePath, UriKind.Absolute))
            {
                var persistentDataPath = Application.persistentDataPath;
                if (IsSaved(texturePath, persistentDataPath))
                {
                    var imageBytes = await LoadFromPersistentData(texturePath, cancellationToken, persistentDataPath);
                    var success = tex.LoadImage(imageBytes);
                    if (!success)
                        Debug.LogWarning($"Failed to load texture from persistant data (original path: '{texturePath}')");
                    return tex;
                }
                else
                {
                    tex = await LoadTextureFromWeb(texturePath, tex, cancellationToken);
                }
            }
            else // Assume local streaming assets folder
            {
                var path = Path.Combine(Application.streamingAssetsPath, texturePath);

#if UNITY_ANDROID && !UNITY_EDITOR
            //StartCoroutine(LoadConfigFileFromWeb(path));
            tex = await LoadConfigFileFromWeb(path, tex);
#else
                tex = await LoadTextureFromLocal(path, tex);
#endif
            }
            return tex;
        }

        static async Task<Texture2D> LoadTextureFromLocal(string path, Texture2D tex)
        {
            var imageBytes = await File.ReadAllBytesAsync(path);

            var success = tex.LoadImage(imageBytes);
            if (!success)
                Debug.LogWarning($"Failed to load image locally from '{path}'");
            return tex;
        }

        static async Task<Texture2D> LoadTextureFromWeb(string uri, Texture2D tex, CancellationToken cancellationToken)
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            webRequest.timeout = 5;

            await webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + webRequest.error);
                return null;
            }
            else
            {
                var success = tex.LoadImage(webRequest.downloadHandler.data);
                if (!success)
                    Debug.LogWarning($"Failed to load web image from '{uri}'");
                else
                {
                    var saveTask = SaveInPersistentData(uri, webRequest.downloadHandler.data, cancellationToken, Application.persistentDataPath);
                    await saveTask;
                    if (!saveTask.Result)
                    {
                        Debug.LogWarning($"Failed to save texture (original path: '{uri}')");
                        Debug.LogError(saveTask.Exception);
                    }
                }

                return tex;
            }
        }

        private async Task<string[]> ReadCSVData(string path)
        {
            async Task<string[]> GetDataLinesFromURL(string uri)
            {
                using UnityWebRequest webRequest = UnityWebRequest.Get(uri);

                await webRequest.SendWebRequest();

                if(webRequest.result == UnityWebRequest.Result.Success)
                { 
                    return webRequest.downloadHandler.text.Split('\n');
                }
                return new string[0];
            }

            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                string[] l;
                path = ConvertToDownloadLink(path);
                if (IsSaved(path))
                {
                    var bytes = await LoadFromPersistentData(path);
                    l = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length).Split('\n');
                }
                else
                {
                    l = await GetDataLinesFromURL(path);
                    var forcedUTF8Encoding = System.Text.Encoding.UTF8.GetBytes(string.Join('\n', l));
                    var success = await SaveInPersistentData(path, forcedUTF8Encoding);
                    if (!success)
                        Debug.LogWarning($"Saving of geometry data ('{path}') failed!");
                }
                return l;

            }
            else
            {
                path = Path.Combine(Application.streamingAssetsPath, path);
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            var allLines = await GetDataLinesFromURL(path);
#else
            if (!File.Exists(path))
                return new string[0];

            var allLines = await File.ReadAllLinesAsync(path, destroyCancellationToken);
#endif
            return allLines;
        }
    }

    public interface IAssetListener
    {
        public void OnAssetLoading(string assetID);
        public void OnAssetChanged(string assetID);
    }

}//namespace TMRI.Client