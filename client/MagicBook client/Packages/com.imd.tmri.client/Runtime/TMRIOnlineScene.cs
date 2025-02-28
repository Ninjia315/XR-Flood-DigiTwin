using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMRI.Core;
using UnityEngine.Networking;
using System;
using System.Linq;

namespace TMRI.Client
{
    [RequireComponent(typeof(AssetHandler), typeof(TMRIScene))]
    public class TMRIOnlineScene : BaseTMRICallback, ISettingsListener
    {
        [Serializable]
        public class NamedShader
        {
            public string Name;
            public Shader Shader;
        }

        public List<NamedShader> ShadersToReplace;
        
        public GameObject loadingPrefab;
        public MonoBehaviour trackerComponent;
        public bool IsAssetChanging { get; private set; } = false;
        public GameObject targetGO;
        public GameObject landmarkPrefab;
        public GameObject panoramaPrefab;
        public GameObject mapAreaPrefab;
        public GameObject mapProjectorPrefab;
        public FloodDataPlayer floodDataPlayer;
        public string currentGeoImageID;
        public LayerMask geoImagesIgnoreLayers;
        public GameObject geometryComponents;
        public LODGroup lodGroup;

        public static bool? UIVisible;
        public static List<SerializableLandmarkType> LandmarksTypesVisible;

        private Dictionary<string, GameObject> modelInstances = new();

        private ClipBox clipBox;
        private TMRIState _MBGameState;
        private AssetHandler _assetHandler;
        private bool initialized;
        private GameObject loadingGO;
        private string _loadedAsset = null;
        private string _currentMarker;
        private TMRIScene _tmriScene;
        private Dictionary<string, (string, float)> markerAssetSize = new Dictionary<string, (string, float)>();

        bool ShouldLoadingGOShow() => (_loadedAsset == null || IsAssetChanging) && !string.IsNullOrEmpty(_currentMarker);

        public interface IPanoramaBehaviour
        {
            public void LoadSettings(string imageUrl, string depthImageUrl, float northRotationDegrees, Vector3 vrPos, Vector3 vrOffset, string modelUrl, bool invertDepth);
        }

        public string GetCurrentAsset()
        {
            if (!string.IsNullOrEmpty(_currentMarker) && markerAssetSize.ContainsKey(_currentMarker))
                return markerAssetSize[_currentMarker].Item1;
            return _loadedAsset;
        }

        private void Awake()
        {
            _assetHandler = GetComponent<AssetHandler>();
            _assetHandler.OnConfigurationLoaded += Init;
            _tmriScene = GetComponent<TMRIScene>();

            if (targetGO != null)
                targetGO.SetActive(false);

            if (trackerComponent != null)
                trackerComponent.enabled = false;
        }

        public void OnTMRISettings(TMRISettings settings)
        {
            if (targetGO != null)
                targetGO.SetActive(true);
            _assetHandler.StartWithConfigURL(settings.ConfigurationURI);
        }

        private void Init(SerializableConfigFile _)
        {
            initialized = true;
            Debug.Log("SceneConfigurator: Started init.");

            _MBGameState = TMRIState.instance;
            _currentMarker = _MBGameState.ImageTarget;

            SetDefaultModels();

            if (IsARContainer())
            {
                RemoveChildren(this.gameObject);
                CreateARComponents();
            }
            else if (IsVRContainer())
            {
                targetGO = this.gameObject;
            }
        }

        private void SetDefaultModels()
        {
            var listOfMarkers = _assetHandler.ReturnTrackingmarkersFromConfig();
            if (listOfMarkers != null)
            {
                foreach (var marker in listOfMarkers)
                {
                    var runtimeIT = gameObject.AddComponent<RuntimeImageTarget>();
                    runtimeIT.FrameUpdate = true;
                    //runtimeIT.ToggleActive = true;
                    runtimeIT.TrackedImageID = marker.name;
                    runtimeIT.trackedImageSize = marker.printwidth;
                    runtimeIT.enabled = false;

                    var downloadableMarkerLink = _assetHandler.ConvertToDownloadLink(marker.link);
                    StartCoroutine(DownloadImageAndLoadAsT2D(downloadableMarkerLink, runtimeIT));

                    var initialScale = _assetHandler.ReturnAssetsFromConfig().Find(a => a.id == marker.defaultToLoad).initialScale;
                    markerAssetSize[marker.name] = (marker.defaultToLoad, initialScale);
                    Debug.Log($"{marker.name}: {markerAssetSize[marker.name]}");
                }
            }
            else
            {
                Debug.LogWarning("Marker list from AssetHandler is null.");
            }
        }

        private void Update()
        {
            if (!initialized)
                return;

            DownloadCurrentMarkerModel();

            if(lodGroup != null && Camera.main != null)
            {
                if (clipBox == null)
                    clipBox = FindAnyObjectByType<ClipBox>();

                if (clipBox != null)
                {
                    lodGroup.transform.position = clipBox.boxCollider.ClosestPoint(Camera.main.transform.position);
                }
            }
        }

        private IEnumerator DownloadImageAndLoadAsT2D(string link, RuntimeImageTarget runtimeImageTarget)
        {
            var success = false;
            Texture2D tex = null;
            var identifier = $"{runtimeImageTarget.TrackedImageID}_{link}";
            if (AssetHandler.IsSaved(identifier, Application.persistentDataPath))
            {
                var loadTask = _assetHandler.LoadFromPersistentData(identifier);
                
                while (!loadTask.IsCompleted)
                    yield return loadTask;

                tex = new Texture2D(2, 2);
                success = tex.LoadImage(loadTask.Result);
                Debug.Log($"Reading cached {runtimeImageTarget.TrackedImageID} texture success: {success}");
            }

            if (!success)
            {
                using UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(link);
                yield return webRequest.SendWebRequest();

                tex = DownloadHandlerTexture.GetContent(webRequest);
                var texBytes = tex.EncodeToPNG();
                _ = _assetHandler.SaveInPersistentData(identifier, texBytes);
            }

            if (tex != null)
            {
                runtimeImageTarget.trackedImageTexture = tex;

                yield return new WaitWhile(() => _MBGameState == null);

                runtimeImageTarget.OnTracking ??= new UnityEngine.Events.UnityEvent();
                runtimeImageTarget.OnTracking.AddListener(() =>
                {
                    if(runtimeImageTarget.TrackedImageID != _currentMarker){
                        Debug.Log("Sending OnImageTargetChange");
                        _MBGameState.OnImageTargetChange(runtimeImageTarget.TrackedImageID);
                    }
                });
                runtimeImageTarget.enabled = true;
                Debug.Log($"Enabled {runtimeImageTarget.TrackedImageID} target");
            }
            else
            {
                Debug.LogWarning("Could not get a texture for Image tracking!");
                if (trackerComponent != null)
                {
                    Debug.LogWarning($"Falling back to hardcoded image tracker ({trackerComponent.GetType()})");
                    trackerComponent.enabled = true;
                }
            }
        }

        private void HandleAssetLoading()
        {
            if (!IsAssetChanging && markerAssetSize.ContainsKey(_currentMarker))
            {
                if (_loadedAsset != markerAssetSize[_currentMarker].Item1)
                    LoadAsset(markerAssetSize[_currentMarker].Item1);

            }
        }

        private void CreateARComponents()
        {
            transform.position = Vector3.zero;
            if (targetGO == null)
            {
                targetGO = new GameObject("root");
                targetGO.transform.parent = transform;
                targetGO.transform.localPosition = Vector3.zero;
            }
            targetGO.transform.SetSiblingIndex(0);

            if (_assetHandler.ReturnDefaultDisplaySize() is Vector3 defaultSize)
            {
                if (_tmriScene.IsDefaultSize)
                    _tmriScene.ChangeDisplaySize(defaultSize);
                _tmriScene.DefaultDisplaySize = defaultSize;
            }

            SpawnLoadingGO();
        }

        private void SpawnLoadingGO()
        {
            if (IsARContainer())
            {
                loadingGO = loadingPrefab == null ? GameObject.CreatePrimitive(PrimitiveType.Quad) : Instantiate(loadingPrefab, targetGO.transform);
                loadingGO.transform.parent = transform;
                loadingGO.transform.localPosition = Vector3.zero;
                loadingGO.SetActive(false);
                loadingGO.name = "Loading placeholder";
            }
        }


        private void RemoveChildren(GameObject obj, bool fullwipe = false)
        {
            if (targetGO == null)
                return;

            var iterator = modelInstances.Select(mi => mi.Key).ToList();
            foreach (var modelInstance in iterator)
            {
                modelInstances.Remove(modelInstance, out GameObject go);
                Destroy(go);
            }
        }

        private IEnumerator GetModelAsset(string assetName, string currentMarker)
        {
            IsAssetChanging = true;

            this.ExecuteOnListeners<IAssetListener>(l => l.OnAssetLoading(assetName), FindObjectsInactive.Include);

            GameObject asset = null;

            var assetTask = _assetHandler.GetAsset(assetName);
            yield return new WaitUntil(() => assetTask.IsCompleted || assetTask.IsCanceled || assetTask.IsFaulted);
            asset = assetTask.Result;
            
            // At this point, assets should be loaded
            if (asset != null && !assetTask.IsFaulted)
            {
                var instantiatedObject = asset;

                if (modelInstances.ContainsKey(assetName))
                {
                    modelInstances[assetName].SetActive(true);
                }
                else
                {
                    instantiatedObject.transform.SetParent(targetGO.transform, worldPositionStays: false);

                    modelInstances[assetName] = instantiatedObject;
                }

                if (_assetHandler.GetAssetOrigin(assetName, out Vector3 geographicOrigin))
                {
                    instantiatedObject.transform.localPosition = new Vector3(
                        instantiatedObject.transform.localPosition.x,
                        -geographicOrigin.z,
                        instantiatedObject.transform.localPosition.z);
                }

                if (IsARContainer())
                {
                    var allAssets = _assetHandler.ReturnAssetsFromConfig();
                    var myAsset = allAssets.First(a => a.id == assetName);

                    ModifyShaders(instantiatedObject);
                    StartCoroutine(ModifyClippingAndLOD(instantiatedObject, _tmriScene.DisplaySizeWorld, markerAssetSize[currentMarker].Item2, myAsset.lodMultiplier));

                    var addedGeomData = _assetHandler.FillGeometryData(assetName, floodDataPlayer.transform);
                    if (!addedGeomData)
                        Debug.LogWarning("No geometry data found, or geoData node is incomplete.");

                    yield return AddLandmarks(myAsset, allAssets, instantiatedObject);
                    yield return AddVRPoints(myAsset, instantiatedObject);
                    yield return AddGeoImages(myAsset, instantiatedObject);
                }

                var scale = markerAssetSize[currentMarker].Item2;

                if(targetGO.transform.localScale == Vector3.one) //default
                    targetGO.transform.localScale = new Vector3(scale, scale, scale);

                if (IsVRContainer())
                {
                    instantiatedObject.SetActive(false);
                }

                this.ExecuteOnListeners<IAssetListener>(l => l.OnAssetChanged(assetName), FindObjectsInactive.Include);
            }
            else
            {
                Debug.LogError($"Assets for {assetName} could not be loaded.");
            }

            IsAssetChanging = false;
        }

        private IEnumerator AddGeoImages(SerializableAsset myAsset, GameObject instantiatedObject)
        {
            var assetName = myAsset.id;
            var geoImagesTask = _assetHandler.LoadGeoImages(assetName, mapProjectorPrefab, instantiatedObject.transform);
            yield return new WaitUntil(() => geoImagesTask.IsCompleted);

            foreach (var mp in geoImagesTask.Result)
            {
                mp.projector.ignoreLayers = geoImagesIgnoreLayers.value;
                mp.ToggleActive(currentGeoImageID == mp.ID);
            }
        }

        private IEnumerator AddVRPoints(SerializableAsset myAsset, GameObject instantiatedObject)
        {
            var assetName = myAsset.id;
            var assetPanoramasTask = _assetHandler.GetAssetPanoramas(assetName);
            yield return new WaitUntil(() => assetPanoramasTask.IsCompleted);

            foreach (var p in assetPanoramasTask.Result)
            {
                var go = Instantiate(panoramaPrefab, instantiatedObject.transform);
                go.transform.localPosition = p.position;

                if (go.TryGetComponent(out IPanoramaBehaviour panorama))
                {
                    panorama.LoadSettings(
                        p.spherical360ImageUrl,
                        p.depthImageUrl,
                        p.rotationFromNorthDegrees,
                        go.transform.localPosition,
                        p.offsetInVR,
                        p.modelUrl,
                        p.invertDepth);
                }
            }
        }

        private IEnumerator AddLandmarks(SerializableAsset myAsset, List<SerializableAsset> allAssets, GameObject instantiatedObject)
        {
            var assetName = myAsset.id;
            var landmarksTask = _assetHandler.GetAssetLandmarks(assetName);
            yield return new WaitUntil(() => landmarksTask.IsCompleted);

            foreach (var lm in landmarksTask.Result)
            {
                var go = Instantiate(landmarkPrefab, instantiatedObject.transform);
                go.transform.localPosition = lm.position;

                if (go.GetComponentInChildren<LandmarkBehaviour>() is LandmarkBehaviour lmb)
                    lmb.SetInfo(lm.label, lm.level, lm.iconTypes, lm.icons);

                if (!string.IsNullOrEmpty(lm.referenceAssetId) && allAssets.FirstOrDefault(ra => ra.id == lm.referenceAssetId) is SerializableAsset referencedAsset)
                {
                    GeoCoordinateConverter.referenceLatitude = myAsset.originLatLonAlt.x;
                    GeoCoordinateConverter.referenceLongitude = myAsset.originLatLonAlt.y;
                    GeoCoordinateConverter.referenceAltitude = myAsset.originLatLonAlt.z;
                    var minPos = Vector3.Scale(new Vector3(-1, 1, -1), GeoCoordinateConverter.GeoToUnity(
                        referencedAsset.minLatLonAlt.x,
                        referencedAsset.minLatLonAlt.y,
                        referencedAsset.minLatLonAlt.z));
                    var maxPos = Vector3.Scale(new Vector3(-1, 1, -1), GeoCoordinateConverter.GeoToUnity(
                        referencedAsset.maxLatLonAlt.x,
                        referencedAsset.maxLatLonAlt.y,
                        referencedAsset.maxLatLonAlt.z));
                    var refGO = Instantiate(mapAreaPrefab);
                    refGO.transform.SetParent(instantiatedObject.transform, false);
                    refGO.transform.localPosition = (maxPos + minPos) / 2f;
                    refGO.transform.localScale = (maxPos - minPos) / 10f;

                    if (refGO.TryGetComponent(out BasicReticlePointerInteraction behaviour))
                    {
                        behaviour.OnInteraction.AddListener(() =>
                        {
                            markerAssetSize[_currentMarker] = (referencedAsset.id, referencedAsset.initialScale);
                        });
                    }
                }
            }
        }

        private IEnumerator ModifyClippingAndLOD(GameObject instantiatedObject, Vector3 displaySize, float modelScale, float lodMultiplier)
        {
            if (instantiatedObject == null)
                yield break;

            var meshRenderers = instantiatedObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var mr in meshRenderers)
            {
                if (mr.gameObject.GetComponent<ClipTiledMesh>() == null && meshRenderers.Length > 3)
                {
                    mr.gameObject.AddComponent<ClipTiledMesh>();

                    foreach (var c in geometryComponents.GetComponents(typeof(MonoBehaviour)))
                        mr.gameObject.AddComponent(c.GetType());

                    mr.gameObject.layer = LayerMask.NameToLayer("3D Model");
                }
            }

            if (instantiatedObject.transform.childCount > 1)
            {
                var hasLODs = _assetHandler.ConfigureLODGroup(lodGroup, instantiatedObject);
                if (hasLODs)
                    lodGroup.size *= lodMultiplier;
                Debug.Log($"{instantiatedObject.name} had LODs: {hasLODs} with size {lodGroup.size}");
            }
        }

        private void RecalculateClippingBounds(Vector3 displaySize, float modelScale, float multiplier=1f)
        {
            foreach (var lodMesh in FindObjectsByType<LODMesh>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                lodMesh.ResetLODSize(modelScale, multiplier);
        }

        private void OnDestroy()
        {

        }

        private void ModifyShaders(GameObject asset)
        {
            MeshRenderer[] rendererArray = asset.GetComponentsInChildren<MeshRenderer>();

            foreach (var renderer in rendererArray)
            {

                Material[] materials = renderer.materials;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        Debug.LogError("Material is null on " + renderer.gameObject.name);
                        continue;
                    }

                    if (ShadersToReplace.FirstOrDefault(s => s.Name == materials[i].shader.name) is NamedShader s)
                        materials[i].shader = s.Shader;
                }

                renderer.materials = materials;
            }
        }

        void DownloadCurrentMarkerModel() 
        {
            if (IsARContainer())
            {
                if (IsDifferentMarker())
                {
                    if(_loadedAsset != markerAssetSize[_MBGameState.ImageTarget].Item1)
                        RemoveChildren(this.gameObject, true);

                    _currentMarker = _MBGameState.ImageTarget;
                }
                loadingGO.SetActive(ShouldLoadingGOShow());
                HandleAssetLoading();
            }
            else if (IsVRContainer())
            {
                if (IsDifferentMarker())
                {
                    RemoveChildren(targetGO);
                    _currentMarker = _MBGameState.ImageTarget;
                    _loadedAsset = null;
                }
                HandleAssetLoading();

            }
        }

        private bool IsARContainer() => gameObject.CompareTag("AR container");
        private bool IsVRContainer() => gameObject.CompareTag("VR container");
        private bool IsDifferentMarker() => _MBGameState.ImageTarget != _currentMarker;

        protected override void OnTMRIMessage(WebsocketMessage msg)
        {
            switch (msg.type)
            {
                case "ASSETANDSIZEUPDATE":
                    Debug.LogWarning(msg.data);
                    string[] msgInfo = msg.data.Split("|");
                    foreach (var info in msgInfo)
                    {
                        var tmp = info.Split("=");
                        var markerID = tmp[0];
                        var assetID = tmp[1];
                        var scale = float.Parse(tmp[2]);

                        markerAssetSize[markerID] = (assetID, scale);
                    }
                    break;
                case "DISPLAYSIZEUPDATE":
                    if (JsonConvert.DeserializeObject<SerializableVector3>(msg.data) is SerializableVector3 size)
                    {
                        var currentAsset = GetCurrentAsset();
                        if (!string.IsNullOrEmpty(currentAsset) && _assetHandler.ReturnAssetsFromConfig().FirstOrDefault(a => a.id == currentAsset) is SerializableAsset sa
                            && markerAssetSize.TryGetValue(_currentMarker, out (string, float) assetAndSize))
                        {
                            RecalculateClippingBounds(_tmriScene.DisplaySizeWorld, assetAndSize.Item2, sa.lodMultiplier);
                        }
                    }
                    break;
                case "CHANGE_LOD_LEVEL":
                    if (int.TryParse(msg.data, out int lodLevel))
                        ForceLOD(lodLevel);
                    break;

                case "TOGGLE_UI":
                    if (bool.TryParse(msg.data, out bool uiVisible))
                        UIVisible = uiVisible;
                    break;
                case "TOGGLE_LANDMARK_TYPES":
                    LandmarksTypesVisible = JsonConvert.DeserializeObject<List<SerializableLandmarkType>>(msg.data);

                    break;
            }
        }

        public void ForceLOD(int lodLevel)
        {
            lodGroup.ForceLOD(lodLevel);
        }

        private void LoadAsset(string assetID)
        {
            if (_loadedAsset != assetID)
            {
                Debug.Log($"SceneConfigurator (AR:{IsARContainer()} VR:{IsVRContainer()}): Marker has setting.");
                // Ensure the previous loading prefab is removed if it's still present
                RemoveChildren(targetGO);

                _loadedAsset = assetID;
                StartCoroutine(GetModelAsset(assetID, _currentMarker));
            }
        }

#if UNITY_EDITOR
        Vector3 _lastScale;

        private void OnGUI()
        {
            const int size = 250;
            int yOffset = 0;
            if (_assetHandler == null)
                return;

            if(targetGO != null && targetGO.transform.localScale != _lastScale)
            {
                _lastScale = targetGO.transform.localScale;

                var currentAsset = GetCurrentAsset();
                if (!string.IsNullOrEmpty(currentAsset) && _assetHandler.ReturnAssetsFromConfig().FirstOrDefault(a => a.id == currentAsset) is SerializableAsset sa)
                {
                    RecalculateClippingBounds(_tmriScene.DisplaySizeWorld, Mathf.Max(_lastScale.x, _lastScale.y, _lastScale.z), sa.lodMultiplier);
                }
            }

            foreach (var marker in _assetHandler.ReturnTrackingmarkersFromConfig())
            {
                if (GUI.Button(new Rect(Screen.width - size, Screen.height * .5f + yOffset, size, 25), marker.name))
                {
                    FindFirstObjectByType<TMRIState>().OnImageTargetChange(marker.name);
                    if (Camera.main != null)
                    {
                        if (Camera.main.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>() is UnityEngine.InputSystem.XR.TrackedPoseDriver tpd)
                            tpd.enabled = false;

                        Camera.main.transform.SetPositionAndRotation(new Vector3(0,.7f,-.7f), Quaternion.Euler(45,0,0));
                    }
                }
                yOffset += 50;
            }

            yOffset = 0;
            foreach (var asset in _assetHandler.ReturnAssetsFromConfig())
            {
                if (GUI.Button(new Rect(Screen.width - size*2, Screen.height * .5f + yOffset, size, 25), asset.id))
                {
                    markerAssetSize[_currentMarker] = (asset.id, asset.initialScale);
                    if (Camera.main != null)
                    {
                        if (Camera.main.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>() is UnityEngine.InputSystem.XR.TrackedPoseDriver tpd)
                            tpd.enabled = false;

                        Camera.main.transform.SetPositionAndRotation(new Vector3(0, .7f, -.7f), Quaternion.Euler(45, 0, 0));
                    }
                }
                yOffset += 50;
            }

            yOffset = 0;
            foreach (var lodBehavious in FindObjectsByType<Core.AssetHandler.ModelLODBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OrderBy(lod => lod.lodIndex))
            {
                // Only one time, if there's any LOD
                if(lodBehavious.lodIndex == 0)
                {
                    if (GUI.Button(new Rect(Screen.width - size * 2.5f, Screen.height * .5f + yOffset, 75, 25), $"Reset LOD"))
                    {
                        ForceLOD(-1);
                    }
                    yOffset += 50;
                }

                if (GUI.Button(new Rect(Screen.width - size * 2.5f, Screen.height * .5f + yOffset, 75, 25), $"LOD {lodBehavious.lodIndex}"))
                {
                    ForceLOD(lodBehavious.lodIndex);
                }
                yOffset += 50;
            }
        }

#endif
    }

}