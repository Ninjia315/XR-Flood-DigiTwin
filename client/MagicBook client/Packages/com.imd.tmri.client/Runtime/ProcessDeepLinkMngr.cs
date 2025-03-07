using UnityEngine;
using UnityEngine.Events;

namespace TMRI.Client
{
    public class ProcessDeepLinkMngr : MonoBehaviour
    {
        public string deeplinkURL;
        public string DebugURL;
        public UnityEvent<string> OnConfigurationURL;

        public bool DebugForUnityEditor;
        private bool isDLSaved = false;

        private void Awake()
        {
            {
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
            }
        }

        private void OnDeepLinkActivated(string url)
        {
            Debug.Log($"Got a deepLink: {url}");
            if (!isDLSaved)
            {
                //isDLSaved = true;
                deeplinkURL = url;
                HandleDeeplink();
            }
        }

        private void HandleDeeplink()
        {
            string downloadLink = deeplinkURL.Split("?", 3)[2];

            OnConfigurationURL?.Invoke(downloadLink);
        }
        
    }
}//namespace TMRI.Client