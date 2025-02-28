using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json;
using TMRI.Core;
using UnityEngine.Events;
using TMPro;

namespace TMRI.Client
{
    public class ProcessDeepLinkMngr : MonoBehaviour
    {
        //public TMP_InputField ServerIPInput;
        //public static ProcessDeepLinkMngr Instance { get; private set; }
        //public const string CONFIGURATION = "config";
        //public const string DISPLAY_SIZE_VECTOR = "display_size_vector";
        public string deeplinkURL;
        public string DebugURL;
        public UnityEvent<string> OnConfigurationURL;

        //public static SerializableConfigFile config;

        public bool DebugForUnityEditor;
        private bool isDLSaved = false;

        private void Awake()
        {
            //Remove Setting from last session
            //if (PlayerPrefs.HasKey(CONFIGURATION))
            //{
            //    config = JsonConvert.DeserializeObject<SerializableConfigFile>(PlayerPrefs.GetString(CONFIGURATION));
            //    foreach (var marker in config.markers)
            //    {
            //        PlayerPrefs.DeleteKey(marker.name);
            //        foreach (var asset in config.assets)
            //        {  // remove all saved assets settings
            //            PlayerPrefs.DeleteKey($"{marker.name}_{asset.id}_scale");
            //        }
            //    }
            //}

            //if (Instance == null)
            {
                //Instance = this;

#if UNITY_EDITOR
                if (DebugForUnityEditor)
                {
                    Debug.LogWarning("ProcessDeeplinkMngr: Debugging for editor enabled.");
                    OnDeepLinkActivated(DebugURL);
                }
#endif

                Application.deepLinkActivated += OnDeepLinkActivated;
                if (!string.IsNullOrEmpty(Application.absoluteURL))
                {
                    // Cold start and Application.absoluteURL not null so process Deep Link.
                    OnDeepLinkActivated(Application.absoluteURL);
                }
                // Initialize DeepLink Manager global variable.
                else deeplinkURL = "[none]";
                //DontDestroyOnLoad(gameObject);
            }
            //else
            {
                //Destroy(gameObject);
            }
        }

        private void OnDeepLinkActivated(string url)
        {
            if (!isDLSaved)
            {
                isDLSaved = true;
                deeplinkURL = url;
                HandleDeeplink();
            }
        }

        private void HandleDeeplink()
        {
            string downloadLink = deeplinkURL.Split("?", 3)[2];
            //StartCoroutine(GetRequest(downloadLink));
            //Debug.Log(downloadLink);
            OnConfigurationURL?.Invoke(downloadLink);
        }
        
    }
}//namespace TMRI.Client