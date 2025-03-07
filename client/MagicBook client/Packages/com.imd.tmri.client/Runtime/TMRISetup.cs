using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using TMRI.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using ZXing;

namespace TMRI.Client
{
    public class TMRISetup : MonoBehaviour
    {
        public TMP_InputField ConfigurationInput;
        public TMP_InputField ServerInput;
        public TMP_Dropdown SeeThroughInput;
        public bool DeactivateAfterStart = true;
        public bool HideCanvas = false;
        public RawImage cameraFeedback;
        public GameObject qrFoundFeedback;
        public bool AutoStartIfServerIPDetected = false;
        public TMP_Text CacheFeedback;
        public Button StartButton;
        public List<CanvasRenderer> OptionalDisableOnVisionPro = new();

        const string PlayerPrefsPrefix = "TMRISetup";
        WebCamTexture camTexture;

        BarcodeReader barcodeReader;
        bool loadingConfig;
        bool autoStart;
        bool webcamInitialized;

        public static string ServerKey => $"{PlayerPrefsPrefix}_server";
        public static string ConfigurationKey => $"{PlayerPrefsPrefix}_configuration";
        static string SeeThroughKey => $"{PlayerPrefsPrefix}_seethroughmode";

        public static int SeeThrough => PlayerPrefs.GetInt(SeeThroughKey);
        public static string ServerIP => PlayerPrefs.GetString(ServerKey);
        public static string ConfigurationURL => PlayerPrefs.GetString(ConfigurationKey);
        public static bool HasSettings => PlayerPrefs.HasKey(ServerKey) && PlayerPrefs.HasKey(SeeThroughKey);

        public UnityEvent<TMRISettings> OnSaveSettingsAndStart;
        public UnityEvent<string> OnConfigurationUrlInput;

        ARSession arSession;

        void OnEnable()
        {
            if (camTexture == null)
            {
#if !UNITY_VISIONOS
                arSession = FindAnyObjectByType<ARSession>();
                if (arSession != null)
                    arSession.enabled = false;

                camTexture = new WebCamTexture();
                camTexture.requestedHeight = Screen.height; // 480;
                camTexture.requestedWidth = Screen.width; //640;
                camTexture.Play();

                cameraFeedback.texture = camTexture;
#endif
            }

            qrFoundFeedback.SetActive(false);

        }

        void OnDisable()
        {
            if (camTexture != null)
            {
                camTexture.Pause();
            }

        }

        void OnDestroy()
        {
            if (camTexture != null)
                camTexture.Stop();
        }

        private void Update()
        {
            if(!webcamInitialized && camTexture != null && camTexture.isPlaying && camTexture.width > 16)
            {
                cameraFeedback.rectTransform.localEulerAngles = new Vector3(0, 0, camTexture.videoRotationAngle);
                cameraFeedback.rectTransform.localScale = new Vector3(1, camTexture.videoVerticallyMirrored ? -1 : 1, 1);
                webcamInitialized = true;
            }
        }

        private void DecodeUpdate()
        {
            if (camTexture != null && camTexture.isPlaying)
            {
                if (SplitScreenCanvas.instance != null)
                    SplitScreenCanvas.instance.gameObject.SetActive(false);

                DecodeQR(camTexture.GetPixels32());

//#if POLYSPATIAL_ENABLE_WEBCAM
//                Unity.PolySpatial.PolySpatialObjectUtils.MarkDirty(camTexture);
//#endif
            }

        }

        // Start is called before the first frame update
        void Start()
        {
            StartButton.interactable = false;

#if UNITY_VISIONOS && !UNITY_EDITOR
            foreach (var cr in OptionalDisableOnVisionPro)
            {
                cr.SetAlpha(0f);
                foreach (Transform child in cr.transform)
                    child.gameObject.SetActive(false);
            }
#endif

            if (HideCanvas)
                GetComponent<Canvas>().enabled = false;

            if(string.IsNullOrEmpty(ServerInput.text))
                ServerInput.text = ServerIP;

            if (string.IsNullOrEmpty(ConfigurationInput.text))
            {
#if UNITY_VISIONOS && !UNITY_EDITOR
                //const string localDirectory = "Configuration";
                //var allConfigs = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, localDirectory));
                //if (allConfigs.Length > 0)
                //    ConfigurationInput.text = Path.Combine(localDirectory, Path.GetFileName(allConfigs[0]));

                if(string.IsNullOrEmpty(ConfigurationInput.text))
                    ConfigurationInput.text = ConfigurationURL;
#else
                ConfigurationInput.text = ConfigurationURL;
#endif
            }

            SeeThroughInput.value = (int)SeeThrough;

            barcodeReader = new BarcodeReader { AutoRotate = false, Options = new ZXing.Common.DecodingOptions { TryHarder = false } };

            InvokeRepeating(nameof(DecodeUpdate), 1.0f, 0.1f);

            if(!string.IsNullOrEmpty(ConfigurationInput.text))
            {
                autoStart = AutoStartIfServerIPDetected;
                TryGetServerIPFromConfiguration(ConfigurationInput.text);
            }
        }


        public void SaveSettingsAndStartScene()
        {
            if(loadingConfig)
            {
                autoStart = true;
                return;
            }

            var serverIP = ServerInput.text.Trim();
            var seeThrough = SeeThroughInput.value;
            var configURI = ConfigurationInput.text.Trim();

            PlayerPrefs.SetString(ServerKey, serverIP);
            PlayerPrefs.SetString(ConfigurationKey, configURI);
            PlayerPrefs.SetInt(SeeThroughKey, seeThrough);

            if (DeactivateAfterStart)
                gameObject.SetActive(false);

            if (SplitScreenCanvas.instance != null)
                SplitScreenCanvas.instance.gameObject.SetActive(true);

            if (arSession != null)
                arSession.enabled = true;

            var settings = new TMRISettings
            {
                ServerIP = serverIP,
                ConfigurationURI = configURI,
                SeeThroughMode = (TMRIState.SeeThroughMode)SeeThrough
            };

            OnConfigurationUrlInput?.Invoke(configURI);
            OnSaveSettingsAndStart?.Invoke(settings);

            this.ExecuteOnListeners<ISettingsListener>(listener =>
            {
                listener.OnTMRISettings(settings);
            }, FindObjectsInactive.Include);
        }

        public async void TryGetServerIPFromConfiguration(string configurationURI)
        {
            StartButton.interactable = false;

            Task<string> getConfigTask;

            if (Uri.IsWellFormedUriString(configurationURI, UriKind.Absolute))
            {
                if (Regex.Match(configurationURI, @"(?:drive\.google\.com\/(?:file\/d\/|open\?id=))([^\/&]+)") is Match m && m.Success && m.Groups.Count >= 2)
                {
                    var fileId = m.Groups[1].Value;
                    configurationURI = $"https://drive.google.com/uc?export=download&id={fileId}";
                }
                getConfigTask = GetConfigurationFromWeb(configurationURI);
            }
            else // Assume local streaming assets folder
            {
                var path = Path.Combine(Application.streamingAssetsPath, configurationURI);

#if UNITY_ANDROID && !UNITY_EDITOR
                getConfigTask = GetConfigurationFromWeb(path);
#else
                getConfigTask = File.ReadAllTextAsync(path);
#endif
            }

            SerializableConfigFile config = null;

            try
            {
                await getConfigTask;

                if (getConfigTask.IsCompletedSuccessfully)
                {
                    config = JsonConvert.DeserializeObject<SerializableConfigFile>(getConfigTask.Result);
                }
            }
            catch
            {
                Debug.LogWarning($"TMRI setup: '{configurationURI}' did not return a valid configuration json");
            }

            var assetHandler = FindFirstObjectByType<AssetHandler>();
            var doCacheFeedback = false;
            if (config != null)
            { 
                ServerInput.text = config.ipAddress;
                StartButton.GetComponentInChildren<TMP_Text>().text = "Start";
                StartButton.interactable = true;
                ConfigurationInput.text = configurationURI;

                doCacheFeedback = true;

                if (autoStart)
                    SaveSettingsAndStartScene();
            }
            else if(assetHandler != null && assetHandler.HasCache(out config))
            {
                if(PlayerPrefs.HasKey(ConfigurationKey))
                    ConfigurationInput.text = ConfigurationURL;

                doCacheFeedback = true;

                StartButton.GetComponentInChildren<TMP_Text>().text = "Start with cache";
                StartButton.interactable = true;
            }
            else
            {
                StartButton.interactable = false;
                GetComponent<Canvas>().enabled = true;

                if (CacheFeedback != null)
                {
                    CacheFeedback.text = $"Error getting config!";
                }
            }

            if(assetHandler != null && doCacheFeedback && CacheFeedback != null)
            {
                var hasCache = assetHandler.HasCache(config, out Dictionary<string, bool> markerCache, out Dictionary<string, bool> modelCache);
                CacheFeedback.text = $"<b>Cache</b> Markers:\n";
                CacheFeedback.text += string.Join('\n', markerCache.Select(mc => $"[<color={(mc.Value ? "green" : "orange")}>{mc.Key}</color>]"));
                CacheFeedback.text += $"\n<b>Cache</b> Models:\n";
                CacheFeedback.text += string.Join('\n', modelCache.Select(mc => $"[<color={(mc.Value ? "green" : "orange")}>{mc.Key}</color>]"));
            }
        }

        private async Task<string> GetConfigurationFromWeb(string uri)
        {
            loadingConfig = true;

            using UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            webRequest.timeout = 5;

            await webRequest.SendWebRequest();

            loadingConfig = false;

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + webRequest.error);
            }

            return webRequest.downloadHandler.text;
        }

        void DecodeQR(Color32[] c)
        {
            // create a reader with a custom luminance source
            try
            {
                // decode the current frame
                var result = barcodeReader.Decode(c, camTexture.width, camTexture.height);
                if (result != null && !string.IsNullOrEmpty(result.Text))
                {
                    string downloadLink = result.Text.Split("?", 3)[2];
                    ConfigurationInput.text = downloadLink;
                    TryGetServerIPFromConfiguration(downloadLink);
                    qrFoundFeedback.SetActive(true);
                    enabled = false;
#if UNITY_EDITOR
                    OnDestroy();
#endif
                }

            }
            catch(Exception e)
            {
                Debug.LogError(e.Message);
            }
            
        }
    }

    public interface ISettingsListener
    {
        abstract void OnTMRISettings(TMRISettings settings);
    }

}//namespace TMRI.Client
