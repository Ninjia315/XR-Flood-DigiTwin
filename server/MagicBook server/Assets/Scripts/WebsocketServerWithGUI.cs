using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Globalization;
using UnityEngine.UI;
using TMRI.Core;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;
using Unity.VisualScripting.FullSerializer;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using UnityEditor.MemoryProfiler;
#endif


public class WebsocketServerWithGUI : WebsocketServerRoom
{
    public bool debug;
    public static WebsocketServerWithGUI Instance { get; private set; }
    public Button Increment_Display_size_Btn;
    public Button Decrement_Display_size_Btn;
    public List<TMP_InputField> Display_size_xyz_elements;
    public GameObject Displaybox;
    public GameObject MarkerPosition;
    //GUI elements and support variables
    public GameObject ConnectedClientsStartpoint;
    public GameObject MarkerEditContentsStartpoint;
    public TMP_Text ServerAddressLabel;
    public TMP_InputField DownloadlinkTextfield;
    public TMP_InputField ModelScaleTextfield;
    public TMP_Dropdown ConnectionDD;
    public TMP_Dropdown GeoImageDropdown;
    public GameObject MapProjectorPrefab;

    public GameObject ClientTextPrefab;
    public GameObject MarkerNameHeadPrefab;
    public GameObject MarkerDropdownPrefab;
    public GameObject PlusButtonPrefab;
    public GameObject MinusButtonPrefab;
    public GameObject ResetPoseButtonPrefab;
    public GameObject ScaleRatioDisplayPrefab;
    public GameObject LengthDisplay;
    public GameObject WidthDisplay;
    public List<GameObject> MapSizeUIDisplayElements;
    public GameObject MarkerAssetControlContainer;
    public GameObject MapAssetScalingControlContainer;
    public GameObject MapPointerPrefab;
    public LandmarkBehaviour LandmarkPrefab;
    public GameObject UpdatedMapCenterHeightPlanePrefab;
    public GameObject virtualMapPoint { get; private set;}
    public UnityEvent<bool> mapPointUpdated;
    public UnityEvent resetYawPitchOnViewMode;
    public float display_size_stepsize = 0.1f;
    public AssetHandler assetHandler;
    public Slider geoDataStepSlider;

    public float fixedRatioStep = 10f;     // The step size for the fixed ratio
    public DragMap dragMap;
    public GameObject LoadingFeedback;
    public FloodDataPlayer FloodPlayer;
    public List<FloodShader> FloodShaders;
    public UnityEvent<float> OnFloodStepUpdate;
    public List<NamedShader> ShadersToReplace;
    public LayerMask GeoImageProjectorIgnoreLayers;
    public GameObject UILandmarkTypesContainer;
    public Toggle UILandmarkTypePrefab;
    public Toggle UILandmarkTypeToggleAll;
    public Toggle VRVisibleToggle;
    public GameObject VRPointPrefab;

    private Dictionary<string, IPAddress> connections;
    private List<string> clientsOnGUI = new();
    private List<GameObject> currentTextObjects = new();
    private List<GameObject> MarkerHeader = new();
    private List<GameObject> MarkerDropdowns = new();
    private List<GameObject> Spacer = new();
    private List<GameObject> pointerList = new();
    private GameObject lastContainer;
    private Vector3 lastmapCenterPostion;
    private Dictionary<string, Dictionary<string, float>> markerModelScalesRatioDisplay = new Dictionary<string, Dictionary<string, float>>();

    //Configuration file
    private SerializableConfigFile _config = null;
    private string lastSentAssetConfiguration = null;
    private string _lastSentDisplaySize = null;
    private string _lastMapPosition = null;
    private string _lastMapRotation = null;
    private const string PlayerPrefConfigKey = "CONFIG_URL";
    private const int VRpointLandmarkType = -100;

    private MapSizeCalculator mapSizeCalculator;
    private Dictionary<string, GameObject> instantiatedAssets = new();
    private Vector3 initialCamPos;
    private FloodVisualizationType currentFloodType;
    private bool showAffectedBuildings;
    private float currentGeoImageOpacity = 1f;
    private Quaternion initialCamRot;
    private TMP_InputField scaleRatioInputPlaceHolder;
    private Dictionary<SerializableLandmarkType, bool> loadedLandmarkTypesVisibilty = new();
    private bool uiVisible = true;

    public enum WaterColor { LightGreen, Blue, Muddy }
    public WaterColor selectedwaterColor = WaterColor.LightGreen;

    [Serializable]
    public class NamedShader
    {
        public string Name;
        public Shader Shader;
    }

    public void DownloadButtonClicked()
    {
        // Check if link is Google Drive sharing URL, and if so convert to download URL
        var resultString = DownloadlinkTextfield.text.Trim();

        if (Regex.Match(resultString, @"(?:drive\.google\.com\/(?:file\/d\/|open\?id=))([^\/&]+)") is Match m && m.Success)
        {
            var fileId = m.Groups[1].Value;
            resultString = $"https://drive.google.com/uc?export=download&id={fileId}";
        }

        // Save the config url in case the server restarts
        PlayerPrefs.SetString(PlayerPrefConfigKey, resultString);
        DownloadlinkTextfield.text = resultString;

        if(assetHandler == null)
            assetHandler = gameObject.AddComponent<AssetHandler>();
        else if(assetHandler.HasConfig)
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            return;
        }

        assetHandler.OnConfigurationLoaded += (cfg) =>
        {
            _config = cfg;
            ServerAddressLabel.color = CompareAddressColor();

            UpdateSliderGUIWithMarkersFromConfig();

            InsertDisplaySizeFromConfigurationAndSetup();
            MarkerSizeDisplay();        // Display the marker size
            CheckeMapPositionUpdate();   // Once downloaded the _config, Check the map position Update along x-z plane and send the updated position to the client
            CheckMapRotationUpdate();   // Check the map rotation Update and send the updated rotation to the client
            FillLandmarkTypesUI(cfg);
            LoadAssets();
        };

        LoadingFeedback.SetActive(true);
        assetHandler.StartWithConfigURL(resultString);
    }

    private async void FillLandmarkTypesUI(SerializableConfigFile config)
    {
        if(!config.landmarkTypes?.Any() ?? true)
        {
            UILandmarkTypesContainer.SetActive(false);
            return;
        }

        var iconTexTasks = new Dictionary<int, Task<Texture2D>>();
        foreach (var lmt in _config.landmarkTypes)
        {
            iconTexTasks[lmt.id] = AssetHandler.LoadTexture(lmt.iconUrl, destroyCancellationToken);
        }

        await Task.WhenAll(iconTexTasks.Values);

        foreach (var lmt in config.landmarkTypes)
        {
            loadedLandmarkTypesVisibilty[lmt] = true; //All landmarks are visible at default

            var uiToggle = Instantiate(UILandmarkTypePrefab, UILandmarkTypesContainer.transform);
            if (uiToggle.GetComponentInChildren<TMP_Text>() is TMP_Text label)
                label.text = lmt.name;

            if (uiToggle.GetComponentInChildren<RawImage>() is RawImage icon)
            {
                if (iconTexTasks[lmt.id].IsCompletedSuccessfully)
                    icon.texture = iconTexTasks[lmt.id].Result;
                else
                    icon.gameObject.SetActive(false);
            }

            uiToggle.onValueChanged.AddListener((val) =>
            {
                loadedLandmarkTypesVisibilty[lmt] = val;

                this.ExecuteOnListeners<LandmarkBehaviour.ILandmarkTypeListener>(listener => listener.OnLandmarkTypeVisible(lmt.id, val));

                UpdateLandmarkTypesVisible();
            });

            uiToggle.gameObject.SetActive(true);
            UILandmarkTypeToggleAll.onValueChanged.AddListener(val => uiToggle.isOn = val);
        }

        UILandmarkTypePrefab.gameObject.SetActive(false);
    }

    public void ChangeAssetsClicked()
    {
        ChangeModelSizeClicked();

        LoadAssets();
        ResetPoseClicked();

        foreach (var inst in instantiatedAssets.Values)
            inst.SetActive(false);

        foreach (var markerAsset in GetMarkerAssetGUIConfiguration())
        {
            if (instantiatedAssets.ContainsKey(markerAsset.Value))
            {
                instantiatedAssets[markerAsset.Value].SetActive(true);
            }
        }
    }

    IEnumerable<KeyValuePair<string, string>> GetMarkerAssetGUIConfiguration()
    {
        foreach (var markersetting in MarkerDropdowns.Zip(MarkerHeader, Tuple.Create))
        {
            TMP_Dropdown tempDD = markersetting.Item1.GetComponent<TMP_Dropdown>();
            TMP_Text tempHeader = markersetting.Item2.GetComponent<TMP_Text>();
            yield return new KeyValuePair<string, string>(tempHeader.text, tempDD.options[tempDD.value].text);
        }
    }

    float ConvertRatioScaleViceVersa(float scaleOrRatioInMeters)
    {
        const float CM_TO_METERS = 0.01f;
        return CM_TO_METERS / scaleOrRatioInMeters;
    }

    float GetRoundedRatio(float scaleMeters)
    {
        var preciseRatio = ConvertRatioScaleViceVersa(scaleMeters);
        return Mathf.Round(preciseRatio);
    }

    public void ChangeModelSizeClicked()
    {
        void ScaleAround(GameObject target, Vector3 pivot, Vector3 newScale)
        {
            Vector3 A = target.transform.localPosition;
            Vector3 B = pivot;

            Vector3 C = A - B; // diff from object pivot to desired pivot/origin

            float RS = newScale.x / target.transform.localScale.x; // relataive scale factor

            // calc final position post-scale
            Vector3 FP = B + C * RS;

            // finally, actually perform the scale/translation
            target.transform.localScale = newScale;
            target.transform.localPosition = FP;
        }

        if (_config != null)
        {
            string concatstring = "";
            
            foreach (var markersetting in GetMarkerAssetGUIConfiguration())
            {
                concatstring = concatstring + markersetting.Key + "=";
                concatstring = concatstring + markersetting.Value;

                foreach (var maker in markerModelScalesRatioDisplay)
                {
                    foreach (var model in maker.Value)
                    {
                        if (markersetting.Key == maker.Key && markersetting.Value == model.Key)
                        {
                            var scale = model.Value;
                            var vectorScale = new Vector3(scale, scale, scale);

                            if (instantiatedAssets.ContainsKey(model.Key))
                            {
                                var obj = instantiatedAssets[model.Key].transform;

                                // Store the world position of the model before scaling
                                Vector3 posBefore = obj.InverseTransformPoint(Vector3.zero);

                                // Apply scaling to the model
                                obj.localScale = vectorScale;

                                // Compute the difference in world position due to scaling
                                Vector3 posAfter = obj.InverseTransformPoint(Vector3.zero);
                                Vector3 positionOffset = posBefore - posAfter;

                                var offsetWorld = obj.TransformVector(positionOffset);

                                dragMap.transform.position -= offsetWorld;
                            }

                            concatstring = concatstring + "=" + scale.ToString() + "|";

                            if (FloodPlayer.currentDataReader != null)
                            {
                                FloodPlayer.transform.localScale = vectorScale;

                                if (assetHandler.GetAssetOrigin(model.Key, out Vector3 geographicOrigin))
                                {
                                    FloodPlayer.currentDataReader.transform.localPosition = new Vector3(
                                        FloodPlayer.currentDataReader.transform.position.x,
                                        geographicOrigin.z * FloodPlayer.currentDataReader.transform.localScale.y,
                                        FloodPlayer.currentDataReader.transform.position.z);
                                }
                            }

                            // Update the physical length and width of the visivle area
                            LengthWidthConversion(scale, false);
                        }
                    }
                }
            }
            concatstring = concatstring.Remove(concatstring.Length - 1);
            Debug.Log($"[ChangeModelSizeClicked]: {concatstring}");
            var msg = new WebsocketMessage()
            {
                type = "ASSETANDSIZEUPDATE",
                data = concatstring
            };
            lastSentAssetConfiguration = concatstring;
            AllSessionsBroadcast(JsonConvert.SerializeObject(msg));

            // Since we are scaling the 3D model with a pivot at the world origin, we probably also changed the drag map's position
            OnMapPositionUpdated(dragMap.transform.position);
        }
        else
        {
            Debug.Log("No Configuartion available.");
        }

    }

    public void ResetPoseClicked()  // Future work: Reset to the initial pose from config file
    {
        if (dragMap != null)
        {
            GameObject goalPlane = dragMap.gameObject;  // Get the reference of the goal plane
            goalPlane.transform.position = new Vector3(0, 0, 0); // Reset the position of the goal plane
            goalPlane.transform.rotation = new Quaternion(0, 0, 0, 1); // Reset the rotation of the goal plane
            dragMap.OnDrag?.Invoke(goalPlane.transform.position); // Invoke the OnDrag event
            dragMap.OnRotation?.Invoke(new Vector4(0, goalPlane.transform.rotation.y, 0, goalPlane.transform.rotation.w)); // Invoke the OnRotation event
        }
        else
        {
            Debug.LogError("DragMap is not assigned! Reset_Button does nothing!");
        }

        //Reset the camera position and rotation, reset initialYaw and initialPitch on dragMap class
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = initialCamPos;
            cam.transform.rotation = initialCamRot;
            resetYawPitchOnViewMode?.Invoke();
        }

    }

    public void ToggleUIClicked()
    {
        //PREVENT TOGGLE UI BUTTON TO BE CLICKED BEFORE CLICKING THE DOWNLOAD BUTTON
        if (_config == null)
        {
            Debug.LogWarning("No Configuration available, please download the configuration first!");
            return;
        }

        if (assetHandler.IsAnyAssetProcessing())
        {
            // If any asset is processing, then show the warning message
            Debug.LogWarning("Map is Downloading, Toggle UI could only be activated after finishing download!");
            return;
        }

        uiVisible = !uiVisible;

        //Toggle the active state of client UI info
        if (currentTextObjects.Count > 0)
        {
            ConnectedClientsStartpoint.SetActive(uiVisible);
        }
        else
        {
            //Debug.LogWarning("No client text objects found!");
        }

        //Toggle the displaybox area visibility
        if (Displaybox != null)
        {
            foreach(Transform child in Displaybox.transform)
            {
                child.gameObject.SetActive(uiVisible);
            }
        }
        else
        {
            Debug.LogWarning("Displaybox object is not found!");
        }

        //Toggle map visibility between occluded box Shader and Standard Shader
        if (dragMap.transform.childCount > 0)
        {
            foreach (Transform childContainer in dragMap.transform)
            {
                if (uiVisible)
                    foreach ( Transform mapAsset in childContainer)
                        ModifyShaders(mapAsset.gameObject);
            }
        }
        else
        {
            Debug.LogWarning("No map asset found!");
        }

        if (LengthDisplay != null && WidthDisplay != null)
        {
            LengthDisplay.SetActive(uiVisible);
            WidthDisplay.SetActive(uiVisible);
        }
        else
        {
            Debug.LogWarning("LengthDisplay or WidthDisplay object is not found!");
        }

        foreach (var pointer in pointerList)
        {
            pointer.SetActive(uiVisible);
        }

        var msg = new WebsocketMessage()
        {
            type = "TOGGLE_UI",
            data = uiVisible.ToString()
        };

        AllSessionsBroadcast(JsonConvert.SerializeObject(msg));
    }


    public void OnVRJumpInClicked(bool value)
    {
        this.ExecuteOnListeners<LandmarkBehaviour.ILandmarkTypeListener>(listener => listener.OnLandmarkTypeVisible(VRpointLandmarkType, value));

        var msg = new WebsocketMessage()
        {
            type = "TOGGLE_VR",
            data = value.ToString()
        };

        AllSessionsBroadcast(JsonConvert.SerializeObject(msg));
    }

    public void ToggleControlOrView()
    {
        dragMap.isMapControl = !dragMap.isMapControl;
    }

    public void WaterColorChangedClicked()
    {
        if (!assetHandler.IsAnyAssetProcessing())
        {
            selectedwaterColor++; //once button clicked, change to the next color
            foreach (Transform childContainer in dragMap.transform)
            {
                foreach (Transform mapAsset in childContainer)
                {
                    MeshRenderer[] rendererArray = mapAsset.gameObject.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer renderer in rendererArray)
                    {
                        foreach (Material material in renderer.materials)
                        {
                            if (material.shader.name == ShadersToReplace[1].Name) //Check if the shader is the water shader
                            {
                                //wrap around the water color integer to 0 if it exceeds the number of water colors
                                if ((int)selectedwaterColor == (Enum.GetValues(typeof(WaterColor)).Length))
                                {
                                    selectedwaterColor = WaterColor.LightGreen;
                                }
                                ApplyWaterColor(material, selectedwaterColor);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Map is Downloading, ColorChanged UI could only be activated after finishing download!");
        }
    }

    public void ApplyWaterColor(Material waterMaterial, WaterColor color)
    { 
        if (waterMaterial != null)
        {
            switch (color)
            {
                case WaterColor.LightGreen:
                    waterMaterial.color = Color.HSVToRGB(45f/ 360f, 33f/ 100f, 89f/ 100f);
                    break;

                case WaterColor.Blue:
                    waterMaterial.color = Color.HSVToRGB(191f/ 360f, 33f/ 100f, 89f/ 100f);
                    break;
                case WaterColor.Muddy:
                    waterMaterial.color = Color.HSVToRGB(38f/ 360f, 69f/ 100f, 88f/ 100f);
                    break;
                default:
                    Debug.LogWarning("ApplyWaterColor: Invalid Water Color!");
                    break;
            }
        }
        else
        {
            Debug.LogWarning("ApplyWaterColor: Water Material is not found!");
        }
    }

    private void InsertDisplaySizeFromConfigurationAndSetup()
    {
        for (int index = 0; index < Display_size_xyz_elements.Count; index++)
        {
            var element = Display_size_xyz_elements[index];
            element.text = ((Vector3)_config.display_size)[index].ToString();
            element.onSubmit.AddListener(text => CheckFloatAcceptable(element, text));
            element.onDeselect.AddListener(text => CheckFloatAcceptable(element, text));
        }

        // Update the display box size from the configuration file
        Displaybox.transform.localScale = (Vector3)_config.display_size;// * 2;
    }

    private void CheckFloatAcceptable(TMP_InputField input, string msg)
    {
        if (float.TryParse(msg, out _))
        {
            input.image.color = Color.white;
            CheckSizeVectorAcceptable();
        }
        else
        {
            input.image.color = Color.red;
        }
    }

    private void CheckSizeVectorAcceptable(bool forceSend=false)
    {
        if (Display_size_xyz_elements.All(element => float.TryParse(element.text, out _)))
        {
            string tmp = JsonConvert.SerializeObject(new SerializableVector3(
                float.Parse(Display_size_xyz_elements[0].text),
                float.Parse(Display_size_xyz_elements[1].text),
                float.Parse(Display_size_xyz_elements[2].text)
            ));

            // Update the visual display box size in control panel
            var visualBoxSize = new Vector3(
                float.Parse(Display_size_xyz_elements[0].text),
                float.Parse(Display_size_xyz_elements[1].text),
                float.Parse(Display_size_xyz_elements[2].text));// * 2;
            Displaybox.transform.localScale = visualBoxSize;

            // Update the physical length and width of the visivle area when eiditing the display size
            var tempScale = markerModelScalesRatioDisplay[_config.markers[0].name][ _config.markers[0].defaultToLoad];

            LengthWidthConversion(tempScale, false);       

            if (forceSend || _lastSentDisplaySize == null || !_lastSentDisplaySize.Equals(tmp))
            {
                _lastSentDisplaySize = SendDisplaySizeChange(tmp);
            }
        }
    }

    string SendDisplaySizeChange(string data)
    {
        var msg = new WebsocketMessage()
        {
            type = "DISPLAYSIZEUPDATE",
            data = data
        };
        
        AllSessionsBroadcast(JsonConvert.SerializeObject(msg));
        return data;
    }

    public void OnModelLODDropdownValueChange(int dropdownIndex)
    {
        var lodLevel = dropdownIndex - 1;

        Displaybox.GetComponent<LODGroup>().ForceLOD(lodLevel);

        var msg = new WebsocketMessage()
        {
            type = "CHANGE_LOD_LEVEL",
            data = lodLevel.ToString()
        };
        
        AllSessionsBroadcast(JsonConvert.SerializeObject(msg));
    }

    // Check the map position Update along x-z plane and send the updated position to the client
    private void CheckeMapPositionUpdate()
    {
        if (dragMap == null)
        {
            Debug.LogError("DragMap is not assigned!");
        }
        else
        {
            //Add a listener to the dragMap.OnDrag event
            dragMap.OnDrag.AddListener(OnMapPositionUpdated);
        }
    }

    private void OnMapPositionUpdated(Vector3 updatedPosition)
    {
        //Create the JSON string of the updated position
        string tmp = JsonConvert.SerializeObject(new SerializableVector3(updatedPosition.x, 0, updatedPosition.z));

        //Create the websocket message
        var msg = new WebsocketMessage()
        {
            type = "MAPPOSITIONUPDATE",
            data = tmp
        };

        _lastMapPosition = JsonConvert.SerializeObject(msg);

        //Send the updated position to all the clients
        AllSessionsBroadcast(JsonConvert.SerializeObject(msg));
    }

    // Check the map rotation Update and send the updated rotation to the client
    private void CheckMapRotationUpdate()
    {
        if (dragMap == null)
        {
            Debug.LogError("DragMap is not assigned!");
        }
        else
        {
            //Add a listener to the dragMap.OnRotation event
            dragMap.OnRotation.AddListener(OnMapRotationUpdated);
        }
    }

    private void OnMapRotationUpdated(Vector4 rotation)
    {
        string tmp = JsonConvert.SerializeObject(new SerializableQuaternion(0, rotation.y, 0, rotation.w));

        var msg = new WebsocketMessage()
        {
            type = "MAPROTATIONUPDATE",
            data = tmp
        };

        _lastMapRotation = JsonConvert.SerializeObject(msg);

        //Send the updated rotation to all the clients
        AllSessionsBroadcast(JsonConvert.SerializeObject(msg));
    }

    //Marker size Display
    private void MarkerSizeDisplay()
    {
        float markerWidth = _config.markers[0].printwidth; // Set the first default marker width
        MarkerPosition.transform.localScale = new Vector3(markerWidth, markerWidth, 0.01f);
    }
    private Color CompareAddressColor()
    {
        return _config == null ? Color.white : (ServerAddressLabel.text.Contains(_config.ipAddress) ? Color.green : new Color(1,.5f,0));
    }

    private void SetupMarkerDropdown(GameObject dd, SerializableTrackingmarker marker)
    {
        if (dd != null)
        {
            // Get the TMP_Dropdown component from the GameObject
            TMP_Dropdown dropdown = dd.GetComponent<TMP_Dropdown>();

            if (dropdown != null)
            {
                // Clear existing options
                dropdown.options.Clear();

                // Add new options to the dropdown
                foreach (var option in _config.assets)
                {
                    dropdown.options.Add(new TMP_Dropdown.OptionData(option.id));
                }

                // Refresh the dropdown to show the new options
                dropdown.RefreshShownValue();
                dropdown.value = dropdown.options.FindIndex(option => option.text == marker.defaultToLoad);
                dropdown.onValueChanged.AddListener((_) => ChangeAssetsClicked());
            }
            else
            {
                Debug.LogError("TMP_Dropdown component not found on the GameObject.");
            }
        }
        else
        {
            Debug.LogError("Dropdown reference is not set.");
        }
    }
        

    public class AssetEnabledClient : RoomConnectedClient
    {
        private WebsocketServerWithGUI _wssvwg = WebsocketServerWithGUI.Instance;

        protected override void OnOpen()
        {
            base.OnOpen();

            if (!string.IsNullOrEmpty(_wssvwg.lastSentAssetConfiguration))
            {
                Debug.Log($"Sending last sent asset configuration to new client: {_wssvwg.lastSentAssetConfiguration}");

                var msg = new WebsocketMessage()
                {
                    type = "ASSETANDSIZEUPDATE",
                    data = _wssvwg.lastSentAssetConfiguration
                };
                Send(JsonConvert.SerializeObject(msg));
            }
            else
            {
                _wssvwg.doInUnityThread += _wssvwg.ChangeModelSizeClicked;
            }
            
            if (!string.IsNullOrEmpty(_wssvwg._lastSentDisplaySize))
            {
                Debug.Log($"Sending last sent asset configuration to new client: {_wssvwg._lastSentDisplaySize}");
                _wssvwg.SendDisplaySizeChange(_wssvwg._lastSentDisplaySize);
            }
            else
            {
                _wssvwg.doInUnityThread += () => _wssvwg.CheckSizeVectorAcceptable(forceSend: true);
            }

            if (!string.IsNullOrEmpty(_wssvwg._lastMapPosition))
            {
                Debug.Log($"Sending last sent map position to new client: {_wssvwg._lastMapPosition}");
                _wssvwg.AllSessionsBroadcast(_wssvwg._lastMapPosition);
            }
            else
            {
                _wssvwg.doInUnityThread += () => _wssvwg.OnMapPositionUpdated(_wssvwg.dragMap.transform.position);
            }

            if (!string.IsNullOrEmpty(_wssvwg._lastMapRotation))
            {
                Debug.Log($"Sending last sent map rotation to new client: {_wssvwg._lastMapRotation}");
                _wssvwg.AllSessionsBroadcast(_wssvwg._lastMapRotation);
            }

            if(_wssvwg.FloodPlayer != null)
            {
                _wssvwg.doInUnityThread += _wssvwg.UpdateFlood;
            }

            if(_wssvwg.GeoImageDropdown != null)
            {
                var id = _wssvwg.GeoImageDropdown.options[_wssvwg.GeoImageDropdown.value].text;
                
                _wssvwg.doInUnityThread += () => _wssvwg.UpdateGeoImageVisualization(id, _wssvwg.currentGeoImageOpacity);
            }

            if(_wssvwg.loadedLandmarkTypesVisibilty != null)
            {
                _wssvwg.doInUnityThread += _wssvwg.UpdateLandmarkTypesVisible;
            }
        }

    }

    //Singleton
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
            
    }

    bool IsVirtualAdapter(NetworkInterface networkInterface)
    {
        // Check if the network interface name contains known keywords for virtual adapters
        string[] virtualKeywords = { "virtual", "vmware", "virtualbox", "vpn", "tunnel", "vethernet" };

        foreach (string keyword in virtualKeywords)
        {
            if (networkInterface.Name.ToLower().Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }

    Dictionary<string, IPAddress> GetPossibleConnections()
    {
        Dictionary<string, IPAddress> connentions = new();
        try
        {
            // Get all IPv4 addresses associated with the local machine
            IPAddress[] ipAddresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToArray();

            // Get all network interfaces
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (IPAddress ipAddress in ipAddresses)
            {
                // Find the network interface that matches the IP address
                NetworkInterface matchingInterface = networkInterfaces.FirstOrDefault(ni => ni.OperationalStatus == OperationalStatus.Up &&
                (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet || ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) && ni.GetIPProperties().UnicastAddresses.Any(addrInfo => addrInfo.Address.Equals(ipAddress) && !IsVirtualAdapter(ni)));
                if (matchingInterface != null)
                {
                    string type = matchingInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? "Ethernet" : "Wifi";
                    connentions[type] = ipAddress;
                }
            }
            return connentions;
        }
        catch (Exception e)
        {
            Debug.LogError("Error getting IP address: " + e.Message);
            return null;
        }
    }

    public static Dictionary<string,IPAddress> GetAllLocalIPv4(NetworkInterfaceType _type)
    {
        Dictionary<string, IPAddress> ipAddrList = new();
        foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
            {
                foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddrList[$"{ip.Address} ({_type})"] = ip.Address;
                    }
                }
            }
        }
        return ipAddrList;
    }

    // Start is called before the first frame update
    void Start()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            initialCamPos = cam.transform.position;
            initialCamRot = cam.transform.rotation;
        }

        DownloadlinkTextfield.text = PlayerPrefs.GetString(PlayerPrefConfigKey);

        //Create an empty GameObject to store the updated center position of the mapPlane that is Activated currently
        virtualMapPoint = new GameObject("virtualMapCenter");

        // Iterate through existing assets (if any) to enable offline models
        foreach (Transform preloadedAsset in dragMap.transform)
        {
            //instantiatedAssets[preloadedAsset.gameObject.name] = preloadedAsset.gameObject;
        }

        SetupConnectionDD();
        AddCustomListenersGUI();
        StartWebsocketWithDropdownSetting(ConnectionDD);
        CheckTouchMapScalingEvent();
    }

    private void AddCustomListenersGUI()
    {
        Increment_Display_size_Btn.onClick.AddListener(() => IncreaseDecreaseClicked(true));
        Decrement_Display_size_Btn.onClick.AddListener(() => IncreaseDecreaseClicked(false));
    }

    void IncreaseDecreaseClicked(bool isIncrease)
    {

        foreach (var inputfield in Display_size_xyz_elements)
        {
            if (!float.TryParse(inputfield.text, out float value)) continue;
            float result = value + (isIncrease ? display_size_stepsize : -display_size_stepsize);
            inputfield.text = result <= 0 ? display_size_stepsize.ToString() : result.ToString();
        }
        CheckSizeVectorAcceptable();
    }

    void SetupConnectionDD()
    {
        connections = GetAllLocalIPv4(NetworkInterfaceType.Ethernet).Concat(GetAllLocalIPv4(NetworkInterfaceType.Wireless80211)).ToDictionary(d => d.Key, d => d.Value); //GetPossibleConnections();

        if (ConnectionDD != null)
        {
            // Clear existing options
            ConnectionDD.options.Clear();
            // Add new options to the dropdown
            foreach (var option in connections.Keys)
            {
                //Debug.Log(option.name);
                ConnectionDD.options.Add(new TMP_Dropdown.OptionData(option));
                if (debug)
                {
                    ConnectionDD.options.Add(new TMP_Dropdown.OptionData("DebugConnection"));
                }
            }

            // Refresh the dropdown to show the new options
            ConnectionDD.RefreshShownValue();
        }
        else
        {
            Debug.LogError("Dropdown reference is not set.");
        }
    }

    public void StartWebsocketWithDropdownSetting(TMP_Dropdown change)
    {
        wssv?.Stop();
        Debug.LogWarning(wssv != null ? $"Websocket Server on {wssv?.Address} stopped." : "Initializing Websocket Server");
        initialized = false;
        wssv = null;
        wssv = new WebSocketServer(Port, secure: false);

        wssv.AddWebSocketService<AssetEnabledClient>(Endpoint, (ts) =>
        {
            Debug.Log($"RoomConnectedClient {ts.ID} initialization...");
        });

        wssv.Start();
        if (!wssv.WebSocketServices.TryGetServiceHost(Endpoint, out room))
        {
            Debug.LogError("couldn't get websocket server host");
        }

        ServerAddressLabel.text = string.Join('\n', connections.Keys);//FullAddress;
        ServerAddressLabel.color = CompareAddressColor();
    }

    private void UpdateSliderGUIWithMarkersFromConfig()
    {
        // support one marker for now
        if (_config == null)
        {
            return;
        }

        // Clear existing objects
        ClearAndDestroyGameObjects(MarkerHeader);
        ClearAndDestroyGameObjects(MarkerDropdowns);
        ClearAndDestroyGameObjects(Spacer);

        // Check if MarkerAssetControl container has child, if so, remove all the child objects
        if (MarkerAssetControlContainer.transform.childCount > 0)
        {
            foreach (Transform child in MarkerAssetControlContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
     
        // Iterate through each marker in the config and loading the MarkerAssetControlContainer
        foreach (var marker in _config.markers)
        {
            // Create and set up TMP_Text
            GameObject tmpHeader = Instantiate(MarkerNameHeadPrefab, MarkerAssetControlContainer.transform);
            TMP_Text tmpText = tmpHeader.GetComponent<TMP_Text>();
            tmpText.text = marker.name; 
            MarkerHeader.Add(tmpHeader);

            // Create and set up TMP_Dropdown
            GameObject tmpDD = Instantiate(MarkerDropdownPrefab, MarkerAssetControlContainer.transform);
            SetupMarkerDropdown(tmpDD, marker);
            MarkerDropdowns.Add(tmpDD);

            // Create and set up plusButton and minusButton for scaling
            GameObject plusButton = Instantiate(PlusButtonPrefab, MapAssetScalingControlContainer.transform);
            GameObject minusButton = Instantiate(MinusButtonPrefab, MapAssetScalingControlContainer.transform);
            // Create ScaleRatioDisplay Instance
            GameObject scaleRatioDisplay = Instantiate(ScaleRatioDisplayPrefab, MapAssetScalingControlContainer.transform);
            TMP_InputField scaleRatioInput = scaleRatioDisplay.GetComponentInChildren<TMP_InputField>();

            //Get the current selected dropdown value
            TMP_Dropdown dropMenu = tmpDD.GetComponent<TMP_Dropdown>();
            string initialDropOptionText = dropMenu.options[dropMenu.value].text;

            // Read and iterate from _config.assets, find correspongding assets.id and input initialScale
            foreach (var asset in _config.assets)
            {
                if (initialDropOptionText == asset.id)
                {
                    scaleRatioInput.text = GetRoundedRatio(asset.initialScale).ToString(); // Display Ratio
                }
            }

            // Update the model InitialScale to markerModelScales
            foreach (var asset in _config.assets)
            {
                // This dictionary is used to store the scale for each model for each marker
                if (markerModelScalesRatioDisplay.ContainsKey(marker.name))
                {
                    
                    markerModelScalesRatioDisplay[marker.name].Add(asset.id, asset.initialScale);
                }
                else
                    markerModelScalesRatioDisplay[marker.name] = new Dictionary<string, float> { { asset.id, asset.initialScale } };
            }

            // Add onValueChanged listener to dropMenu and update current Scale from the markerModelScales with currentDropMenu changing
            dropMenu.onValueChanged.AddListener((int value) =>
            {
                // Get current dropMenu.options[dropMenu.value].text
                string currentDropOptionText = dropMenu.options[value].text;

                //Read Modified Scale from markerModelScales and show it in TMP_InputField
                foreach (var asset in _config.assets)
                {
                    if (currentDropOptionText == asset.id)
                    {
                        scaleRatioInput.text = GetRoundedRatio(markerModelScalesRatioDisplay[marker.name][asset.id]).ToString(); // Display Ratio
                    }
                }
            });

            // Add event listener to increment and decrement buttonComponent
            Button incrementButtonComponent = plusButton.GetComponent<Button>();
            Button decrementButtonComponent = minusButton.GetComponent<Button>();

            // Add event listener to increment and decrement buttonComponent for scaleRatioInput
            incrementButtonComponent.onClick.AddListener(() =>
            {
                ScaleRatioOnDecrement(scaleRatioInput);
                ChangeModelSizeClicked();
            });

            decrementButtonComponent.onClick.AddListener(() =>
            {
                ScaleRatioOnIncrement(scaleRatioInput);
                ChangeModelSizeClicked();
            });

            // Add event listener to ScaleRatioInput to update the user physical length input in meters (Ratio)
            scaleRatioInput.onValueChanged.AddListener((string physcialLengthInput) =>
            {
                if (float.TryParse(physcialLengthInput, out float physicalLength))
                {
                    if (physicalLength <= 0) // prevent negative scale and zero scale input
                    {
                        Debug.LogWarning("Non-Postive number is invalid. Please enter a valid number.");
                        return;
                    }

                    // Convert the physical input to the backend scale value (0.0000X)
                    float backendScale = ConvertRatioScaleViceVersa(physicalLength);

                    // Update the markerModelScales with backendScale
                    markerModelScalesRatioDisplay[marker.name][dropMenu.options[dropMenu.value].text] = backendScale;
                }
                else
                {
                    Debug.LogError("Invalid input. Please enter a valid number.");
                }


            });

            // Add event listener to ScaleRatioInput when user finish editing the input
            scaleRatioInput.onEndEdit.AddListener((string physcialLengthInput) =>
            {
                if (float.TryParse(physcialLengthInput, out float physicalLength))
                {
                    if (physicalLength <= 0) // prevent negative scale and zero scale input
                    {
                        Debug.LogWarning("Non-Postive number is invalid. Please enter a valid number.");
                        return;
                    }

                    // Convert the physical input to the backend scale value (0.0000X)
                    float backendScale = ConvertRatioScaleViceVersa(physicalLength);

                    // Update the markerModelScales with backendScale
                    markerModelScalesRatioDisplay[marker.name][dropMenu.options[dropMenu.value].text] = backendScale;
                    ChangeModelSizeClicked();
                }
                else
                {
                    Debug.LogError("Invalid input. Please enter a valid number.");
                }
            });

        }

    }

    private void UpdateGUIWithMarkersFromConfig()
    {
        if (_config == null)
        {
            return;
        }

        // Clear existing objects
        ClearAndDestroyGameObjects(MarkerHeader);
        ClearAndDestroyGameObjects(MarkerDropdowns);
        ClearAndDestroyGameObjects(Spacer);

        // Get initial sibling index to start placing new objects
        int siblingIndex = MarkerEditContentsStartpoint.transform.GetSiblingIndex();

        // Iterate through each marker in the config
        foreach (var marker in _config.markers)
        {
            // Create and set up TMP_Text
            GameObject tmpHeader = Instantiate(MarkerNameHeadPrefab, MarkerEditContentsStartpoint.transform.parent);
            TMP_Text tmpText = tmpHeader.GetComponent<TMP_Text>();
            tmpText.text = marker.name;
            MarkerHeader.Add(tmpHeader);

            // Create and set up TMP_Dropdown
            GameObject tmpDD = Instantiate(MarkerDropdownPrefab, MarkerEditContentsStartpoint.transform.parent);
            SetupMarkerDropdown(tmpDD, marker);
            MarkerDropdowns.Add(tmpDD);

            // Set sibling indices for TMP_Text and TMP_Dropdown
            tmpHeader.transform.SetSiblingIndex(++siblingIndex);
            tmpDD.transform.SetSiblingIndex(++siblingIndex);

            // Create 1 spacers and update MarkerEditContentsStartpoint
            siblingIndex = CreateSpacers(tmpDD, 1, siblingIndex);

            //method calling to update the GUI with "+, -" buttons and "scale inputfield"
            siblingIndex = UpdateGUIWithModelSize(tmpDD, marker, siblingIndex);
        }

    }

    private void UpdateResetButton(int siblingIndex)
    {
        // Create and set up ResetPoseButton
        GameObject resetButton = Instantiate(ResetPoseButtonPrefab, MarkerEditContentsStartpoint.transform.parent);
        siblingIndex++;
        resetButton.transform.SetSiblingIndex(siblingIndex);
        Button resetButtonComponent = resetButton.GetComponent<Button>();
        resetButtonComponent.onClick.AddListener(() => ResetPoseClicked());
    }

    private int CreateSpacers(GameObject posToFillAfter, int count, int startingIndex)
    {
        Transform parent = posToFillAfter.transform.parent;
        GameObject lastSpacer = null;

        for (int i = 0; i < count; i++)
        {
            lastSpacer = new GameObject("Spacer", typeof(RectTransform));
            lastSpacer.transform.SetParent(parent);
            lastSpacer.transform.localScale = Vector3.one; // Ensure the scale is 1
            lastSpacer.transform.SetSiblingIndex(++startingIndex);
            Spacer.Add(lastSpacer);
        }

        // Return the last index used
        return startingIndex;
    }


    // Create a method to update the GUI with "+, -" buttons and "scale inputfield"
    private int UpdateGUIWithModelSize(GameObject dropDown, SerializableTrackingmarker marker, int startingIndex)
    {
        Transform parent = dropDown.transform.parent;

        // Create and set up plusButton and minusButton for scaling
        GameObject plusButton = Instantiate(PlusButtonPrefab, parent);
        GameObject minusButton = Instantiate(MinusButtonPrefab, parent);

        // Create ScaleRatioDisplay Instance
        GameObject scaleRatioDisplay = Instantiate(ScaleRatioDisplayPrefab, parent);
        //Find scale_input component of the child in ScaleRatioDisplay
        TMP_InputField scaleRatioInput = scaleRatioDisplay.GetComponentInChildren<TMP_InputField>();


        // Read initial selected dropdown value and show corresponding initial scale from config file
        // Get the TMP_DropDown componet from the GameObject
        TMP_Dropdown dropMenu = dropDown.GetComponent<TMP_Dropdown>();

        // Get initial dropMenu.options[dropMenu.value].text
        string initialDropOptionText = dropMenu.options[dropMenu.value].text;


        // Read and iterate from _config.assets, find correspongding assets.id and input initialScale
        foreach (var asset in _config.assets)
        {
            if (initialDropOptionText == asset.id)
            {
                //tmpScale.text = FormatFloatToFixedPoint(asset.initialScale); // Fixed-point notation to keep significant digits
                scaleRatioInput.text = GetRoundedRatio(asset.initialScale).ToString(); //asset.initialScale.ToString(); // Display the initial scale in the input field
            }
        }

        // Update the model InitialScale to markerModelScales
        foreach (var asset in _config.assets)
        {
            // This dictionary is used to store the display ratio value for each model for each marker
            if (markerModelScalesRatioDisplay.ContainsKey(marker.name))
            {
                markerModelScalesRatioDisplay[marker.name].Add(asset.id, asset.initialScale);
            }
            else
                markerModelScalesRatioDisplay[marker.name] = new Dictionary<string, float> { { asset.id, asset.initialScale } };
        }


        // Add onValueChanged listener to dropMenu and update initialScale with currentDropMenu changing
        dropMenu.onValueChanged.AddListener((int value) =>
        {
            // Get current dropMenu.options[dropMenu.value].text
            string currentDropOptionText = dropMenu.options[value].text;

            //Read modified Scale from markerModelScales and show it in TMP_InputField
            foreach (var asset in _config.assets)
            {
                if (currentDropOptionText == asset.id)
                {
                    //tmpScale.text = FormatFloatToFixedPoint(markerModelScales[marker.name][asset.id]);
                    scaleRatioInput.text = GetRoundedRatio(markerModelScalesRatioDisplay[marker.name][asset.id]).ToString(); // Display the initial ratio in the input field
                }
            }
        });


        // Add event listener to increment and decrement buttonComponent
        Button incrementButtonComponent = plusButton.GetComponent<Button>();
        Button decrementButtonComponent = minusButton.GetComponent<Button>();

        // Add event listener to increment and decrement buttonComponent for scaleRatioInput
        incrementButtonComponent.onClick.AddListener(() =>
        {
            ScaleRatioOnIncrement(scaleRatioInput);
            ChangeModelSizeClicked();
        });

        decrementButtonComponent.onClick.AddListener(() =>
        {
            ScaleRatioOnDecrement(scaleRatioInput);
            ChangeModelSizeClicked();
        });


        // Add event listener to ScaleRatioInput to update the user  physical length input in meters
        scaleRatioInput.onValueChanged.AddListener((string physcialLengthInput) =>
        {
            if (float.TryParse(physcialLengthInput, out float physicalLength))
            {
                if (physicalLength <= 0) // prevent negative scale and zero scale input
                {
                    Debug.LogWarning("Non-Postive number is invalid. Please enter a valid number.");
                    return;
                }

                // Convert the physical input to the backend scale value (0.0000X)
                float backendScale = ConvertRatioScaleViceVersa(physicalLength);  // 1cm:Ym -> 0.0000X

                // Update the markerModelScales with backendScale
                markerModelScalesRatioDisplay[marker.name][dropMenu.options[dropMenu.value].text] = backendScale;
            }
            else
            {
                Debug.LogError("Invalid input. Please enter a valid number.");
            }
        
                
        });

        // Add event listener to ScaleRatioInput when user finish editing the input
        scaleRatioInput.onEndEdit.AddListener((string physcialLengthInput) =>
        {
            if (float.TryParse(physcialLengthInput, out float physicalLength))
            {
                if (physicalLength <= 0) // prevent negative scale and zero scale input
                {
                    Debug.LogWarning("Non-Postive number is invalid. Please enter a valid number.");
                    return;
                }

                // Convert the physical input to the backend scale value (0.0000X)
                float backendScale = ConvertRatioScaleViceVersa(physicalLength); // 1cm:Ym -> 0.0000X

                // Update the markerModelScales with backendScale
                markerModelScalesRatioDisplay[marker.name][dropMenu.options[dropMenu.value].text] = backendScale;
                ChangeModelSizeClicked();
            }
            else
            {
                Debug.LogError("Invalid input. Please enter a valid number.");
            }
        });

        //Set sibling indices for minusButton, TMP_Scale and plusButton in oder
        minusButton.transform.SetSiblingIndex(++startingIndex);
        scaleRatioDisplay.transform.SetSiblingIndex(++startingIndex);
        plusButton.transform.SetSiblingIndex(++startingIndex);
        return startingIndex;
    }

    //Create a decrement event and Modify scaleRatioInput to decrease the physical length
    private void ScaleRatioOnDecrement(TMP_InputField ScaleRatioInput)
    {
        if (float.TryParse(ScaleRatioInput.text, out float currentPhysicalLength))
        {
            if (currentPhysicalLength <= 0) // prevent negative scale and zero scale input
            {
                Debug.LogWarning("Non-Postive number is invalid. Please enter a valid number.");
                return;
            }

            currentPhysicalLength = currentPhysicalLength > fixedRatioStep ? (currentPhysicalLength - fixedRatioStep) : currentPhysicalLength; // prevent negative scale and zero scale input
            ScaleRatioInput.text = currentPhysicalLength.ToString(); // Display the new scale ratio to user interface
        }
        else
        {
            Debug.LogError("Invalid input. Please enter a valid number.");
        }
    }

    //Create an increment event and Modify scaleRatioInput to increase the physical length
    private void ScaleRatioOnIncrement(TMP_InputField ScaleRatioInput)
    {
        if (float.TryParse(ScaleRatioInput.text, out float currentPhysicalLength))
        {
            currentPhysicalLength = currentPhysicalLength > 0 ? (currentPhysicalLength + fixedRatioStep) : currentPhysicalLength;

            if (currentPhysicalLength <= 0) // prevent negative scale and zero scale input
            {
                Debug.LogWarning("Non-Postive number is invalid. Please enter a valid number.");
                return;
            }
            ScaleRatioInput.text = currentPhysicalLength.ToString(); // Display the new scale ratio to user interface
        }
        else
        {
            Debug.LogError("Invalid input. Please enter a valid number.");
        }
    }

    private float MinimumStep(float scale)
    {
        string scaleString = FormatFloatToFixedPoint(scale);
        int decimalIndex = scaleString.IndexOf('.');
        int significantDigits = decimalIndex >= 0 ? scaleString.Length - decimalIndex - 1 : 0;
        return Mathf.Pow(10, -significantDigits);
    }

    // Only used when converting float to string inputField.text
    private string FormatFloatToFixedPoint(float value) // Fixed-point string format to keep significant digits, remove trailing zeros
    {
        string formattedValue = value.ToString("F7", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
        return formattedValue;
    }

    void CheckTouchMapScalingEvent()
    {
        if (dragMap.TouchMapScaling == null)
        {
            Debug.LogError("TouchMapScaling is null");
            return;
        }
        else
        {
            //Debug.Log("TouchMapScaling is not null");
        }

        dragMap.TouchMapScaling.AddListener((pinchScaleRatio) =>
        {
            if (scaleRatioInputPlaceHolder == null)
            {
                scaleRatioInputPlaceHolder = MapAssetScalingControlContainer.GetComponentInChildren<TMP_InputField>();
            }
            foreach (var MapContainer in instantiatedAssets.Values)
            {
                if (MapContainer.activeSelf) // Locate the active instantiated asset on Marker
                {
                    Debug.Log($"[CheckTouchMapScalingEvent] Current activeMapContainer: {MapContainer.name}");
                    float currentModelScale = (instantiatedAssets[MapContainer.name].transform.localScale.x); // Get the current model scale
                    float updatedModelScale = currentModelScale * pinchScaleRatio; // Update the model scale
                    Debug.Log($"[CheckTouchMapScalingEvent] Current model scale: {currentModelScale}, Updated model scale: {updatedModelScale}");
                    if (scaleRatioInputPlaceHolder != null)
                    {
                        scaleRatioInputPlaceHolder.text = (GetRoundedRatio(updatedModelScale).ToString()); // Display the new ratio to scaleRatioInput and trigger the onValueChanged event
                    }
                    markerModelScalesRatioDisplay[_config.markers[0].name][MapContainer.name] = updatedModelScale; //Keep the updated model scale from touchPinchRatio to the markerModelScalesRatioDisplay
                    ChangeModelSizeClicked();
                }
                else
                {
                    Debug.LogError("[CheckTouchMapScalingEvent] No active instantiated asset found on Marker");
                }
            }
        });
    }

    private void ClearAndDestroyGameObjects(List<GameObject> gameObjectList)
    {
        foreach (var obj in gameObjectList)
        {
            Destroy(obj);
        }
        gameObjectList.Clear();
    }


    // Update is called once per frame
    override protected void Update()
    {
        //base.Update();
        if(doInUnityThread != null)
        {
            doInUnityThread();
            doInUnityThread = null;
        }

        GUIUpdateConnectedClients();

        if (assetHandler == null || !assetHandler.HasConfig)
            return;

        CheckLoadingAssets();

        if (FloodPlayer != null && geoDataStepSlider != null)
        {
            if(((int)geoDataStepSlider.value) != FloodPlayer.CurrentTimeStep)
            {
                geoDataStepSlider.value = FloodPlayer.CurrentTimeStep;
            }
        }

    }

    private void CheckLoadingAssets()
    {
        if (_config == null || assetHandler == null)
            return;

        if (LoadingFeedback != null)
            LoadingFeedback.SetActive(assetHandler.IsAnyAssetProcessing());

    }

    private async void LoadAssets()
    {
        foreach (var markerAsset in GetMarkerAssetGUIConfiguration())
        {
            this.ExecuteOnListeners<IAssetListener>(l => l.OnAssetLoading(markerAsset.Value), FindObjectsInactive.Include);

            if (!instantiatedAssets.ContainsKey(markerAsset.Value))
            {
                var go = await assetHandler.GetAsset(markerAsset.Value);

                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                {
                    mr.gameObject.layer = LayerMask.NameToLayer("3D Model");
                }
                
                instantiatedAssets[markerAsset.Value] = new GameObject(markerAsset.Value);
                instantiatedAssets[markerAsset.Value].transform.parent = dragMap.transform;

                var assetConfig = _config.assets.First(a => a.id == markerAsset.Value);
                var scale = assetConfig.initialScale;

                go.transform.SetParent(instantiatedAssets[markerAsset.Value].transform, worldPositionStays: false);

                //geoDataStepSlider
                var addedGeomData = assetHandler.FillGeometryData(markerAsset.Value, FloodPlayer.transform);
                if (addedGeomData)
                {
                    var min = 0;
                    var max = assetConfig.geoData.dataUrls.Count;
                    var startVal = assetConfig.geoData.fileStartIndex;
                    geoDataStepSlider.minValue = min;
                    geoDataStepSlider.maxValue = max-1;
                    geoDataStepSlider.value = startVal;
                    geoDataStepSlider.transform.parent.gameObject.SetActive(true);

                    FloodPlayer.MaxTimeStep = max;
                    FloodPlayer.CurrentTimeStep = startVal;
                    //FloodPlayer.currentDataReader
                }
                else
                {
                    geoDataStepSlider.transform.parent.gameObject.SetActive(false);
                    Debug.LogWarning("No geometry data found, or geoData node is incomplete.");
                }

                var projectors = await assetHandler.LoadGeoImages(markerAsset.Value, MapProjectorPrefab, instantiatedAssets[markerAsset.Value].transform);

                if(projectors.Any())
                {
                    var geoImageOptions = new List<string>(new[] { "No image" });
                    geoImageOptions.AddRange(projectors.Select(mp => mp.ID));

                    void OnGeoImageDropdownChange(int val)
                    {
                        var textVal = GeoImageDropdown.options[val].text;
                        foreach (var p in projectors)
                        {
                            p.projector.ignoreLayers = GeoImageProjectorIgnoreLayers.value;
                            p.ToggleActive(p.ID == textVal);
                        }

                        var cnfg = assetConfig.geoImages.FirstOrDefault(gi => gi.id == textVal);
                        currentGeoImageOpacity = cnfg?.opacity ?? 0f;

                        UpdateGeoImageVisualization(textVal, currentGeoImageOpacity);
                    };

                    GeoImageDropdown.ClearOptions();
                    GeoImageDropdown.AddOptions(geoImageOptions);
                    GeoImageDropdown.onValueChanged.RemoveAllListeners();
                    GeoImageDropdown.onValueChanged.AddListener(OnGeoImageDropdownChange);
                    GeoImageDropdown.transform.parent.gameObject.SetActive(true);

                    OnGeoImageDropdownChange(0);
                }
                else
                {
                    GeoImageDropdown.transform.parent.gameObject.SetActive(false);
                }

                var vrPoints = await assetHandler.GetAssetPanoramas(markerAsset.Value);
                foreach(var vrPoint in vrPoints)
                {
                    var inst = Instantiate(VRPointPrefab, instantiatedAssets[markerAsset.Value].transform);
                    inst.transform.localPosition = vrPoint.position;
                    if(inst.TryGetComponent(out LandmarkBehaviour lm))
                    {
                        lm.SetInfo(string.Empty, 0, new[] { new SerializableLandmarkType() { id = VRpointLandmarkType } }.ToList(), new());
                    }
                }

                LandmarkConfigFromAsset(markerAsset.Value);
                ModifyShaders(go);
                MapLengthWidth(go);
                CameraLookAtCenterSetUp(go, instantiatedAssets[markerAsset.Value]);

                instantiatedAssets[markerAsset.Value].transform.localPosition = Vector3.zero;
                instantiatedAssets[markerAsset.Value].transform.localScale = new Vector3(scale, scale, scale);

                this.ExecuteOnListeners<IAssetListener>(l => l.OnAssetChanged(markerAsset.Value), FindObjectsInactive.Include);

                // Update the length and width display when first instantiated
                LengthWidthConversion(scale, true);

                if (FloodPlayer.currentDataReader != null)
                {
                    FloodPlayer.transform.localScale = new Vector3(scale, scale, scale);
                    if (assetHandler.GetAssetOrigin(markerAsset.Value, out Vector3 geographicOrigin))
                    {
                        FloodPlayer.currentDataReader.transform.localPosition = new Vector3(
                            FloodPlayer.currentDataReader.transform.position.x,
                            geographicOrigin.z * FloodPlayer.currentDataReader.transform.localScale.y,
                            FloodPlayer.currentDataReader.transform.position.z);
                    }
                }
            }
            else
            {
                Debug.Log($"sending changed: {markerAsset.Value}");
                this.ExecuteOnListeners<IAssetListener>(l => l.OnAssetChanged(markerAsset.Value), FindObjectsInactive.Include);
            }
        }
    }

    IEnumerator RecalculateLOD(LODGroup lodGroup, float waitTime=0f, float lodMultiplier=1f)
    {
        yield return new WaitForSeconds(waitTime);

        //var largestWorldSize = Mathf.Max(Displaybox.transform.lossyScale.x, Displaybox.transform.lossyScale.z);
        var camFovFraction = Camera.main.fieldOfView / 180f;
        lodGroup.RecalculateBounds();
        lodGroup.size *= camFovFraction * 100f * lodMultiplier;
    }

    private void ModifyShaders(GameObject asset)
    {
        MeshRenderer[] rendererArray = asset.GetComponentsInChildren<MeshRenderer>();

        foreach (var renderer in rendererArray)
        {
            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null)
                {
                    Debug.LogError("Material is null on " + renderer.gameObject.name);
                    continue;
                }

                if (ShadersToReplace.FirstOrDefault(s => s.Name == materials[i].shader.name) is NamedShader s)
                {
                    Texture cachedMainTex = null;
                    if(materials[i].mainTexture != null)
                        cachedMainTex = materials[i].mainTexture;

                    materials[i].shader = s.Shader;

                    if(cachedMainTex != null)
                        materials[i].mainTexture = cachedMainTex;
                }
            }

            //renderer.materials = materials;
        }

    }

    // Calculate the map physical length and width
    void MapLengthWidth(GameObject go)
    {
        MapSizeUIDisplayElements[0].SetActive(true); //Activate the MapSize Header

        //Calculate the map size
        mapSizeCalculator = go.AddComponent<MapSizeCalculator>();
        mapSizeCalculator.CalculateMapSize();

        //Check if the Length&Width display is null
        if (MapSizeUIDisplayElements[1] == null)
        {
            Debug.LogError("Length&Width diplay element is null!");
            return;
        }

        TMP_Text mapSize = MapSizeUIDisplayElements[1].GetComponent<TextMeshProUGUI>();

        if (mapSize == null)
        {
            Debug.LogError("MapSize TextMeshProUGUI is not found!");
            return;
        }
        mapSize.text = $"{mapSizeCalculator.GetLength()}m x {mapSizeCalculator.GetWidth()}m";
        MapSizeUIDisplayElements[1].SetActive(true); //Activate the Length&Width Display
        Debug.Log($"Map Size: {mapSizeCalculator.GetLength()} of length x {mapSizeCalculator.GetWidth()} of width");
        //Activate the image background for the Length&Width Display
        MapSizeUIDisplayElements[1].transform.parent.gameObject.SetActive(true);

    }


    //Calculate the map average position y and set a invisible plane used for update the map center position y (Used for view mode)
    void CameraLookAtCenterSetUp (GameObject asset, GameObject mapContainer)
    {
        return;
        if (asset.GetComponent<MapSizeCalculator>() == null)
        {
            asset.AddComponent<MapSizeCalculator>();
        }
        mapSizeCalculator =asset.GetComponent<MapSizeCalculator>();
        float CameraLookatCenterPosition = mapSizeCalculator.AveragePositionHeight(); //Get the average position y of the  map asset

        // Find the parent of the Instantiated asset
        if (mapContainer == null)
        {
            Debug.LogError("Instantiate Asset parent is null!");
            return;
        }
        GameObject mapCenterPlane = Instantiate(UpdatedMapCenterHeightPlanePrefab, mapContainer.transform); //Instantiate the map center plane
        mapCenterPlane.transform.localPosition = new Vector3(0, CameraLookatCenterPosition, 0); //Set the position of the map center plane
    }

    //When dragging the mapPlane, the invisible plane will follow the mapPlane, but I am only interested in the updated center height of the mapPlane (0,Y,0)
    void updatedMapCenterposition()
    {
        GameObject activeContainer = null;
        foreach (var MapContainer in instantiatedAssets.Values)
        {
            if (MapContainer.activeSelf) // Locate the active instantiated asset on Marker
            {
                //Debug.Log($"[updatedMapCenterposition] Current activeMapContainer: {MapContainer.name}");
                activeContainer = MapContainer;
                break;
            }
        }
        if (activeContainer != null && activeContainer.transform.Find("UpdatedMapCenterHeightPlanePrefab(Clone)") != null)
        {
            GameObject mapCenterPlane = activeContainer.transform.Find("UpdatedMapCenterHeightPlanePrefab(Clone)").gameObject;
            virtualMapPoint.transform.position = new Vector3(0, mapCenterPlane.transform.position.y, 0); //Update the virtualMapPoint position
        }

        if (lastContainer == activeContainer && lastmapCenterPostion != virtualMapPoint.transform.position) // scaling happend
        {
            mapPointUpdated?.Invoke(true);
        }
        else if (lastContainer == activeContainer && lastmapCenterPostion == virtualMapPoint.transform.position) // no scaling
        {
            mapPointUpdated?.Invoke(false);
        }
        else if (lastContainer != activeContainer) // switch to another asset
        {
            mapPointUpdated?.Invoke(true);
        }
        lastContainer = activeContainer;
        lastmapCenterPostion = virtualMapPoint.transform.position;
    }

    void LengthWidthConversion(float scale, bool initializationFlag)
    {
        TextMeshPro lengthTex = LengthDisplay.GetComponent<TextMeshPro>();
        TextMeshPro widthTex = WidthDisplay.GetComponent<TextMeshPro>();

        float tableLength = Display_size_xyz_elements[0].text == "" ? 0 : float.Parse(Display_size_xyz_elements[0].text);
        float tableWidth = Display_size_xyz_elements[2].text == "" ? 0 : float.Parse(Display_size_xyz_elements[2].text);

        lengthTex.text = (Mathf.Round(tableLength / scale )).ToString() + "m";
        widthTex.text = (Mathf.Round(tableWidth / scale)).ToString() + "m";

        if (initializationFlag)
        {
            LengthDisplay.SetActive(true);
            WidthDisplay.SetActive(true);
        }
    }

    async void LandmarkConfigFromAsset(string assetName)
    {
        if (_config != null)
        {
            GameObject container = instantiatedAssets[assetName];
           
            int index = 0;
            foreach (var landmark in await assetHandler.GetAssetLandmarks(assetName))
            {
                var landmarkBehaviour = Instantiate(LandmarkPrefab, container.transform);
                landmarkBehaviour.transform.localPosition = new Vector3(landmark.position.x, landmark.position.y, landmark.position.z);
                landmarkBehaviour.SetInfo(landmark.label, landmark.level, landmark.iconTypes, landmark.icons);

                pointerList.Add(landmarkBehaviour.gameObject);

                index++;
            }
        }
        else
        {
            Debug.LogError("Landmark Config Failed, Config file is not loaded!");
        }
    }

    void BillboardLandmarkAndLabel(List<GameObject> pointerList)
    {
        foreach (var pointer in pointerList)
        {
            GameObject pointerLabel = pointer.GetComponentInChildren<TMP_Text>().gameObject;
            pointerLabel.transform.forward = Camera.main.transform.forward;
            
            GameObject pointerIcon = pointer.GetComponentInChildren<MeshFilter>().gameObject;
            pointerIcon.transform.rotation = Quaternion.Euler(0,pointerLabel.transform.localEulerAngles.y,0);
        }
    }
    void GUIUpdateConnectedClients()
    {
        // Delete currentCount TMP_Text Objects after ConnectedClientsLabel Object.
        foreach (var textObject in currentTextObjects)
        {
            Destroy(textObject);
        }
        currentTextObjects.Clear();
        clientsOnGUI.Clear();

        int index = 0;
        foreach (var c in room.Sessions.Sessions)
        {
            var client = c as RoomConnectedClient;
            clientsOnGUI.Add($"[{index}]\n{client.deviceInfo}\nimageTarget: {client.imageTargetName}\nBattery: {client.batteryLevel}");
            index++;
        }

        // Create index number TMP_Text objects and add clientsOnGUI as text for the TMP_Text objects.
        // Fill the created TMP_Text objects after ConnectedClientsLabel Object.
        foreach (var clientInfo in clientsOnGUI)
        {
            GameObject tmpTextObject = Instantiate(ClientTextPrefab, ConnectedClientsStartpoint.transform);
            TMP_Text tmpText = tmpTextObject.GetComponentInChildren<TMP_Text>();
            tmpText.text = clientInfo;
            currentTextObjects.Add(tmpTextObject);
        }

        // Reposition the client Info objects in order, get the PosY of the first object and set the rest of the objects in order.
        if (currentTextObjects.Count > 0)
        {
            float postionDiffDelta;
            postionDiffDelta = currentTextObjects[0].GetComponent<RectTransform>().anchoredPosition.y;
            for (int i = 0; i < currentTextObjects.Count; i++)
            {
                RectTransform rt = currentTextObjects[i].GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, postionDiffDelta * (1 + 2 * i));
            }
        }
    }

    new void OnGUI()
    {
    }

    public void OnFloodVisualizationChange(int value)
    {
        FloodPlayer.UpdateFloodVisualizationType((FloodVisualizationType)value);

        currentFloodType = (FloodVisualizationType)value;
        UpdateFlood();
    }

    public void OnFloodTimeChange(float value)
    {
        FloodPlayer.CurrentTimeStep = (int)value;

        UpdateFlood();
    }

    public void OnShowAffectedBuildingsChange(bool value)
    {
        showAffectedBuildings = value;
        UpdateFlood();
    }
    
    public void OnFloodPlayingChange(bool value)
    {
        FloodPlayer.Autoplay = value;
        VRVisibleToggle.isOn = !value;

        UpdateFlood();
    }

    public void OnFloodMaxTimeChange(float maxTime)
    {
        UpdateFlood();
    }

    private void UpdateFlood()
    {
        if (FloodPlayer.currentDataReader == null)
            return;

        var floodDataUpdate = new FloodVisualizationData()
        {
            FloodType = currentFloodType,
            AnimationStep = FloodPlayer.CurrentTimeStep,
            AnimationSpeedSeconds = FloodPlayer.AutoplaySpeedSeconds,
            ShowAffectedBuildings = showAffectedBuildings,
            IsPlaying = FloodPlayer.Autoplay,
            MaxAnimationStep = FloodPlayer.currentDataReader.filenameFormatMaxFilesCount
        };

        var msg = new WebsocketMessage()
        {
            type = "FLOOD_VISUALIZATION_DATA",
            data = JsonConvert.SerializeObject(floodDataUpdate)
        };

        AllSessionsBroadcast(JsonConvert.SerializeObject(msg));
    }

    public void SetFloodInfoPosition(Vector3 position, float depth, float speed, float altitude)
    {
        var msg = new WebsocketMessage()
        {
            ID = "Shared_Flood_Info_Panel",
            type = "POSITION_AND_INFO",
            data = JsonConvert.SerializeObject(new PositionAndFloodInfo
            {
                position = (SerializableVector3)position,
                depth = depth,
                speed = speed,
                altitude = altitude,
                enabled = true
            })
        };

        AllSessionsBroadcast(JsonConvert.SerializeObject(msg));
    }

    private void UpdateGeoImageVisualization(string geoImageID, float opacity)
    {
        var geoImageData = new GeoImageVisualizationData
        {
            ShowGeoImageId = geoImageID,
            GeoImageOpacity = opacity
        };
        var msg = new WebsocketMessage()
        {
            type = "GEO_IMAGE",
            data = JsonConvert.SerializeObject(geoImageData)
        };

        AllSessionsBroadcast(JsonConvert.SerializeObject(msg));
    }

    private void UpdateLandmarkTypesVisible()
    {
        var visibleTypes = loadedLandmarkTypesVisibilty.Where(lt => lt.Value).Select(lt => lt.Key);

        var msg = new WebsocketMessage()
        {
            type = "TOGGLE_LANDMARK_TYPES",
            data = JsonConvert.SerializeObject(visibleTypes)
        };

        AllSessionsBroadcast(JsonConvert.SerializeObject(msg));
    }
}
