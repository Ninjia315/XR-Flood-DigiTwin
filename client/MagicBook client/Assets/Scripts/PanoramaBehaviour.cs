using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Genesis;
using Newtonsoft.Json;
using NUnit.Framework.Internal;
using TMRI.Client;
using TMRI.Core;
using TriLibCore.General;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class PanoramaBehaviour : BaseTMRICallback, TMRIOnlineScene.IPanoramaBehaviour
{
    public GameObject skyboxPrefab;
    public GameObject waterPlanePrefab;
    public Material occlusionMaterial;
    public Material alternativeWaterMaterial;
    public FloodInfoPanel floodInfoPrefab;

    string Spherical360ImageUrl;
    float NorthRotationDegrees;
    Vector3 VRPosition;
    Vector3 VROffset;
    string ModelUrl;
    string DepthImageUrl;
    bool InvertDepth;

    public class PanoramaVRBehaviour : MonoBehaviour, IBaseXRCameraListener
    {
        public bool InvertDepth;
        public float GroundLevelY;
        public float WaterLevelY;
        public float WaterSpeed;
        public float Elevation;
        public Material alternativeWaterMaterial;
        public FloodInfoPanel floodInfoPrefab;

        private FloodInfoPanel floodInfoInstance;

        public void OnBaseXRCamera(BaseXRCamera cam){}
        public void OnEnableVR()
        {

        }
        public void OnXRTransitionStart(){}
        public void OnEnableAR()
        {
            // Clean up ourselves
            Destroy(gameObject);
        }

        public void SetWaterLevel(float waterDepth)
        {
            var child = transform.GetChild(0);
            child.localPosition = Vector3.up * (GroundLevelY + waterDepth);

            var waterShaderComponent = GetComponentInChildren<NVWaterShaders>();
#if UNITY_VISIONOS
            waterShaderComponent.mirrorOn = false;
            var r = waterShaderComponent.GetComponent<Renderer>();
            r.material = alternativeWaterMaterial;
#endif
            //TODO What to do when we're under the flood water?
            if (Camera.main.transform.position.y < child.position.y)
                child.position = new Vector3(child.position.x, Camera.main.transform.position.y-0.15f, child.position.z);
                //waterShaderComponent.mirrorBackSide = true;

            if (floodInfoInstance == null && waterDepth != 0f)
            {
                floodInfoInstance = Instantiate(floodInfoPrefab);
                floodInfoInstance.ListenForNetworkUpdate = false;
                floodInfoInstance.transform.parent = transform;
                floodInfoInstance.transform.SetLocalPositionAndRotation(new Vector3(0, child.localPosition.y, -3f), Quaternion.identity);

                if (floodInfoInstance.TryGetComponent(out MaintainGlobalScale gs))
                    gs.GlobalScale *= 10f;
            }

            if(floodInfoInstance != null)
                floodInfoInstance.SetFloodInfo(waterDepth, WaterSpeed, Elevation);
        }

        private void Start()
        {
            //InvokeRepeating(nameof(ReadMeshData), 0.1f, 1.0f);
            StartCoroutine(ReadMeshData());
        }

        private IEnumerator ReadMeshData()
        {
            var depthSkybox = GetComponent<DepthSkybox>();
            //var generatedMesh = depthSkybox.CreateMesh();
            //var meshFilter = GetComponent<MeshFilter>();
            //meshFilter.mesh = generatedMesh;

            yield return new WaitUntil(() => GetComponent<MeshRenderer>().sharedMaterial.mainTexture != null, new TimeSpan(0, 0, 10), () =>
            {
                Debug.LogError("Waiting for texture timed out!");
                FindFirstObjectByType<ToggleXRMode>().TransitionToAR();
            });

            var meshGO = depthSkybox.ExtractMesh();
            meshGO.transform.SetParent(transform, false);

            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.enabled = false;

            var vertices = meshGO.GetComponent<MeshFilter>().mesh.vertices;
            //var lowestY = EstimateGroundLevel(vertices, yTolerance: 2f);
            // The camera on the Google Street View car is always 2.5 meters above the ground.
            GroundLevelY = -2.5f;//lowestY - 0.5f;

            Debug.Log($"Lowest Y: {GroundLevelY}");

            SetWaterLevel(WaterLevelY);
        }

        private float EstimateGroundLevel(Vector3[] vertexList, float yTolerance = 0.1f)
        {
            // Convert NativeArray to List for easier manipulation
            //var vertexList = vertices.ToArray();

            // Step 1: Collect y-values of vertices
            var yValues = vertexList.Select(v => v.y).ToList();

            // Step 2: Find the median y-value
            yValues.Sort();
            float groundLevel = yValues[yValues.Count / 2];

            // Optional Step 3: Filter vertices around the median for better robustness
            var groundCandidates = vertexList.Where(v => Mathf.Abs(v.y - groundLevel) < yTolerance).ToList();

            if (groundCandidates.Count > 0)
            {
                // Recalculate ground level using the refined candidates
                groundLevel = groundCandidates.Select(v => v.y).Average();
            }

            return groundLevel;
        }
    }

    public void LoadSettings(string imageUrl, string depthImageUrl, float northRotationDegrees, Vector3 vrPos, Vector3 vrOffset, string modelUrl, bool invertDepth)
    {
        Spherical360ImageUrl = imageUrl;
        VRPosition = vrPos;
        NorthRotationDegrees = northRotationDegrees;
        VROffset = vrOffset;
        ModelUrl = modelUrl;
        DepthImageUrl = depthImageUrl;
        InvertDepth = invertDepth;

        // Preloading the 360 degrees texture will take up too much memory (problem in Vision Pro)
        //LoadSphericalTexture();
    }

    public async void OnInteraction()
    {
        var floodDepth = 0f;
        var floodSpeed = 0f;
        var elevation = 0f;
        //var activeFloodmesh = FindFirstObjectByType<FloodMeshInteractable>(FindObjectsInactive.Exclude);

        foreach (var activeFloodmesh in FindObjectsByType<FloodMeshInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var hitInteractive = activeFloodmesh.GetComponent<MeshCollider>().Raycast(new Ray(transform.position + transform.up * 0.1f, -transform.up), out RaycastHit hitInfo, 10f);
            Debug.DrawRay(transform.position + transform.up * 0.001f, -transform.up, Color.blue, 999f);

            //if (activeFloodmesh != null && activeFloodmesh.TryGetComponent(out MeshCollider mc))
            if (hitInteractive)// && hitInfo.collider.gameObject.TryGetComponent(out FloodMeshInteractable activeFloodmesh))
            {
                //var closestOnCollider = mc.ClosestPoint(transform.position);
                //if(mc.Raycast(new Ray(closestOnCollider + Vector3.up, Vector3.down), out RaycastHit hitInfo, maxDistance: 2f))
                {
                    floodDepth = activeFloodmesh.GetFloodValueAtRaycastHit(hitInfo, FloodVisualizationType.Depth);
                    floodSpeed = activeFloodmesh.GetFloodValueAtRaycastHit(hitInfo, FloodVisualizationType.Speed);
                    elevation = (activeFloodmesh.transform.worldToLocalMatrix * hitInfo.point).y - activeFloodmesh.transform.position.y / activeFloodmesh.transform.lossyScale.y;
                    Debug.Log($"PanoramaBehaviour raycast: {floodDepth} depth, {floodSpeed} speed and {elevation} elevation.");
                }
                break;
            }
        }

        var mat = await LoadVRModel(floodDepth, floodSpeed, elevation);

        LoadSphericalTexture(mat);

        if(!string.IsNullOrEmpty(DepthImageUrl))
            LoadDepthTexture(mat);
        
        FindFirstObjectByType<ToggleXRMode>().TransitionToVR(vrWorldPos: VRPosition, arWorldPos: transform.position);
    }

    async Task<Material> LoadVRModel(float waterDepth, float waterSpeed, float elevation)
    {
        //var go = await FindFirstObjectByType<AssetHandler>().LoadGLTFFromUrl("VR model", ModelUrl);
        //go.transform.parent = GameObject.FindWithTag("VR container").transform;
        //go.transform.position = VRPosition;
        //Debug.Log("done loading VR model");

        var parentContainer = GameObject.FindWithTag("VR container").transform.GetChild(0);

        //var viewerYOffset = new Vector3(0, -0.1f, 0);
        var instance = Instantiate(skyboxPrefab, parentContainer);
        instance.transform.position = VRPosition - VROffset;
        //instance.transform.eulerAngles = new Vector3(0, (180 - NorthRotationDegrees) % 360, 0);
        var vrBehaviour = instance.AddComponent<PanoramaVRBehaviour>();
        vrBehaviour.floodInfoPrefab = floodInfoPrefab;
        vrBehaviour.WaterLevelY = waterDepth;
        vrBehaviour.WaterSpeed = waterSpeed;
        vrBehaviour.Elevation = elevation;
        vrBehaviour.alternativeWaterMaterial = alternativeWaterMaterial;

        var waterPlane = Instantiate(waterPlanePrefab, instance.transform);
        //waterPlane.transform.localPosition = new Vector3(0, -waterDepth, 0);

        var renderer = instance.GetComponent<MeshRenderer>();
        var m = new Material(renderer.sharedMaterial);
        renderer.sharedMaterial = m;

        m.SetFloat("_SquareRoot", 0);
        m.SetFloat("_Rotation", (NorthRotationDegrees + 90) % 360);

        return m;
    }

    async void LoadSphericalTexture(Material m)
    {
        var tex = await AssetHandler.LoadTexture(Spherical360ImageUrl, destroyCancellationToken);
        
        m.SetTexture("_MainTex", tex);

        if (string.IsNullOrEmpty(DepthImageUrl))
        {
            // ============================================================
            // Create Skybox depth mesh from equirectangular image
            // ============================================================
            //var skybox = tex;
            var depthEstimator = new DepthEstimator();
            depthEstimator.GenerateDepth(tex);

            var depthTexture = depthEstimator.PostProcessDepth();
            var range = new Vector2(depthEstimator.MinDepth, depthEstimator.MaxDepth);

            //var renderer = instance.GetComponent<MeshRenderer>();
            //var m = new Material(renderer.sharedMaterial);
            //renderer.sharedMaterial = m;
            
            m.SetTexture("_Depth", depthTexture);
            m.SetFloat("_Scale", InvertDepth ? 1 : 70);
            m.SetFloat("_Min", range.x);
            m.SetFloat("_Max", range.y);
            m.SetFloat("_RadialCoords", 0);
            m.SetFloat("_InverseDepth", InvertDepth ? 1 : 0);

            depthEstimator.Dispose();
        }
    }

    async void LoadDepthTexture(Material m)
    {
        var tex = await AssetHandler.LoadTexture(DepthImageUrl, destroyCancellationToken);
        var texPixels = tex.GetPixels32();
        var rFloatTex = new Texture2D(tex.width, tex.height, UnityEngine.TextureFormat.RFloat, mipChain: false);
        rFloatTex.SetPixels32(texPixels);
        rFloatTex.Apply();
        
        m.SetTexture("_Depth", rFloatTex);
        m.SetFloat("_Scale", InvertDepth ? 1 : 70); // Checked manually to convert to meters
        m.SetFloat("_Max", 4f); // Multiplier of the _Scale in meters for points in infinite
        m.SetFloat("_RadialCoords", 1);
        m.SetFloat("_InverseDepth", InvertDepth ? 1 : 0);

    }

    

    protected override void OnTMRIMessage(WebsocketMessage msg)
    {
        switch (msg.type)
        {
            case "TOGGLE_VR":
                if (bool.TryParse(msg.data, out bool uiVisible))
                {
                    foreach(Transform child in transform)
                        child.gameObject.SetActive(uiVisible);
                }
                break;
        }
    }
}
