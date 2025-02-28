using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMRI.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using WebSocketSharp;

namespace TMRI.Client
{
    public class TMRIState : MonoBehaviour, ISettingsListener
    {
        public string ImageTarget;
        public int SharedSceneIndex = 0;
        public int MainSceneIndex_Cardboard = -1;
        public int MainSceneIndex_MagicLeap = -1;
        public int MainSceneIndex_Simple = 0;
        public int MainSceneIndex_Hololens = -1;
        public int MainSceneIndex_VisionPro = -1;

        public SeeThroughMode ActiveSeeThroughMode = SeeThroughMode.Mono;

        public enum SeeThroughMode
        {
            Mono,
            VideoSeeThrough,
            OpticalSeeThrough,
            Cardboard
        }

        public readonly List<Color> PlayerNumberColor = new(new[]
        {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow,
            Color.gray,
            Color.cyan,
            Color.magenta,
            Color.black
        });


        System.Threading.SynchronizationContext main;
        private Coroutine keepAlive;
        bool initialized;
        WebSocketState? lastWebsocketState = null;
        Task websocketConnectionTask;

        public static TMRIState instance;
        public static Action<TMRIState> OnInstance;

        public int Port = 8082;
        public string HostIP = "127.0.0.1";
        public string Endpoint = "/room";
        public bool AutoStartWebsocketWithDefaultSettings = true;
        public string OptionalConfigurationURL;
        public float ReportBatteryLevelRepeatSeconds = 10f;
        public float KeepAliveRepeatSeconds = 5f;

        public string ReadOnlyID;
        public int CurrentSceneIndex;

        public Action<WebSocket> OnWebSocketSet;
        public UnityEvent<ClientInfo, ClientInfo[]> OnClientInfoUpdate;
        public UnityEvent OnDisplaySizeChangeRecieved;
        public List<GameObject> OtherPlayerPrefabs;
        public WebSocket wscl { get; private set; }
        public Dictionary<string, GameObject> otherPlayers { get; private set; } = new Dictionary<string, GameObject>();
        public void SetHostIP(string ip) => HostIP = ip;
        

        public bool isNull(string id) => string.IsNullOrWhiteSpace(id);
        public bool isMe(string id) => ReadOnlyID == id;
        public bool isOther(string id) => !string.IsNullOrEmpty(ReadOnlyID) && !isMe(id);
        public bool isConnectionOpen() => wscl != null && wscl.ReadyState == WebSocketState.Open;
        public int GetPlayerNumber(string id) => isConnectionOpen() ? otherPlayers.Keys.Append(ReadOnlyID).OrderBy(i => i).ToList().IndexOf(id) : -1;
        public string ConnectionState => wscl == null ? "Initializing" : wscl.ReadyState.ToString();

        public int MainSceneIndex
        {
            get
            {
#if UNITY_MAGICLEAP
            return MainSceneIndex_MagicLeap;
#elif UNITY_WSA
                return MainSceneIndex_Hololens;
#elif UNITY_VISIONOS
                return MainSceneIndex_VisionPro;
#else
                if (TMRISetup.HasSettings)
                {
                    switch ((SeeThroughMode)TMRISetup.SeeThrough)
                    {
                        case SeeThroughMode.Mono:
                        case SeeThroughMode.VideoSeeThrough:
                        case SeeThroughMode.OpticalSeeThrough:
                            return MainSceneIndex_Simple;
                        case SeeThroughMode.Cardboard:
                            return MainSceneIndex_Cardboard;
                        default:
                            return -1;
                    }
                }

                return MainSceneIndex_Simple;
#endif
            }
        }


        private void Awake()
        {
            if (instance == null)
            {
                DontDestroyOnLoad(gameObject);
                instance = this;
                OnInstance?.Invoke(this);
            }
            else if (instance != this)
            {
                Destroy(gameObject);

                var loadMain = true;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (SceneManager.GetSceneAt(i).buildIndex == MainSceneIndex)
                        loadMain = false;

                if (loadMain)
                    SceneManager.LoadScene(MainSceneIndex, LoadSceneMode.Additive);
                return;
            }
        }

        private void OnDestroy()
        {
            if (isConnectionOpen())
#if UNITY_EDITOR || UNITY_VISIONOS
                wscl.Close();
#else
                wscl.CloseAsync();
#endif
        }


        private void Start()
        {
            main = System.Threading.SynchronizationContext.Current;
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (AutoStartWebsocketWithDefaultSettings)
                StartWebSocket();

            var sceneToLoad = MainSceneIndex;
            if(sceneToLoad >= 0)
                SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);

            InvokeRepeating(nameof(ReportBatteryLevel), ReportBatteryLevelRepeatSeconds, repeatRate: ReportBatteryLevelRepeatSeconds);
        }

        public void StartWebSocket(TMRISettings settings = null)
        {
            if (initialized && settings != null && isConnectionOpen())
                wscl.CloseAsync();
            else if (initialized)
                return;

            if (settings != null)
            {
                HostIP = settings.ServerIP;
                OptionalConfigurationURL = settings.ConfigurationURI;
                ActiveSeeThroughMode = settings.SeeThroughMode;
            }

            var uriBuilder = new UriBuilder();
            uriBuilder.Host = HostIP;
            uriBuilder.Port = Port;
            uriBuilder.Path = Endpoint;
            uriBuilder.Scheme = "ws";

            wscl = new WebSocket(uriBuilder.Uri.AbsoluteUri);
            wscl.WaitTime = new TimeSpan(0, 0, 2);
            wscl.OnOpen += (a, b) =>
            {
                Debug.Log($"Websocket connected to {wscl.Url.AbsoluteUri}");
                OnWebSocketSet?.Invoke(wscl);
            };
            wscl.OnMessage += OnMessageAsync;
            wscl.OnError += (a, b) => Debug.LogError(b.Message + "\n" + b.Exception.Message);
            wscl.OnClose += async (websocket, closeEventArgs) =>
            {
                CloseEventArgs args = closeEventArgs;
                Debug.Log($"Websocket got CLOSE event: {args.WasClean} {((CloseStatusCode)args.Code)}");

                if (this != null && (!args.WasClean || (CloseStatusCode)args.Code == CloseStatusCode.Away))
                {
                    await Task.Delay((int)(KeepAliveRepeatSeconds * 1000));
                    Debug.Log("Trying to reconnect...");
                    websocketConnectionTask = Task.Run(wscl.Connect, destroyCancellationToken);
                }
            };

            Debug.Log($"Initialized websocket connection: {wscl.Url.AbsoluteUri} with timeout {wscl.WaitTime.TotalSeconds} seconds");

            websocketConnectionTask = Task.Run(wscl.Connect, destroyCancellationToken);

            initialized = true;
        }

        void Update()
        {
            if(websocketConnectionTask != null && websocketConnectionTask.Exception != null)
            {
                Debug.LogError($"Websocket task faulted. Restarting...\n {websocketConnectionTask.Exception.Message}");

                wscl = null;
                initialized = false;
                StartWebSocket();
            }

            if(wscl != null && wscl.ReadyState != lastWebsocketState)
            {
                this.ExecuteOnListeners<IConnectionStateListener>(listener => listener.OnConnectionStateChange(wscl.ReadyState));
                lastWebsocketState = wscl.ReadyState;
            }
        }

        private void OnMessageAsync(object sender, MessageEventArgs e)
        {
            main.Post(_ =>
            {
                var msg = JsonConvert.DeserializeObject<WebsocketMessage>(e.Data);
                
                switch (msg.type)
                {
                    case "START":
                        // Received a notification that another client has just joined.
                        // Add new instance of that client.
                        if (isOther(msg.ID) && !otherPlayers.ContainsKey(msg.ID))
                        {
                            // Instantiate the corresponding prefab
                            otherPlayers[msg.ID] = Instantiate(OtherPlayerPrefabs[otherPlayers.Count % OtherPlayerPrefabs.Count]);

                            // Attach PlayerIdentifier component and set the ID
                            var identifier = otherPlayers[msg.ID].AddComponent<PlayerIdentifier>();
                            identifier.playerID = msg.ID;
                            identifier.imageTarget = msg.imageTarget;
                            identifier.gameObject.SetActive(identifier.imageTarget == ImageTarget);
                        }
                        break;

                    case "DESTROY":
                        // Received a notification that another client has left.
                        // Remove our instance of that client.
                        if (otherPlayers.ContainsKey(msg.ID))
                        {
                            Destroy(otherPlayers[msg.ID]);
                            otherPlayers.Remove(msg.ID);
                        }
                        break;

                    case "INIT":
                        ReadOnlyID = msg.ID;
                        var allClients = JsonConvert.DeserializeObject<List<ClientInfo>>(msg.data);
                        foreach (var client in allClients)
                        {
                            if (isMe(client.ID)) continue;

                            // Instantiate the corresponding prefab
                            otherPlayers[client.ID] = Instantiate(OtherPlayerPrefabs[otherPlayers.Count % OtherPlayerPrefabs.Count]);

                            // Attach PlayerIdentifier component and set the ID
                            var identifier = otherPlayers[client.ID].AddComponent<PlayerIdentifier>();
                            identifier.playerID = client.ID;
                            identifier.imageTarget = client.imageTarget;
                            identifier.gameObject.SetActive(identifier.imageTarget == ImageTarget);
                        }
                        break;

                    case "CLIENTINFO":
                        var clientInfo = JsonConvert.DeserializeObject<ClientInfo>(msg.data);

                        if (isOther(msg.ID) && otherPlayers.ContainsKey(msg.ID))
                        {
                            var pi = otherPlayers[msg.ID].GetComponent<PlayerIdentifier>();
                            pi.imageTarget = clientInfo.imageTarget;
                            //pi.team = clientInfo.team;
                            pi.gameObject.SetActive(pi.imageTarget == ImageTarget);
                        }
                        else if (isMe(msg.ID))
                        {
                            ImageTarget = clientInfo.imageTarget;

                            foreach (var player in otherPlayers.Values)
                            {
                                player.SetActive(player.GetComponent<PlayerIdentifier>().imageTarget == ImageTarget);
                            }

                            if (FindFirstObjectByType<ToggleXRMode>() is ToggleXRMode xrToggle)
                                xrToggle.Reset();
                        }
                        break;

                    case "RESTART":
                        Restart();
                        break;
                    
                }

            }, null);
        }

        public void Restart()
        {
            SceneManager.LoadScene(SharedSceneIndex, LoadSceneMode.Single);
            ImageTarget = null;
        }


        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (wscl != null)
                wscl.OnMessage -= OnMessageAsync;
        }

        public void OnImageTargetChange(string imageTargetName)
        {
            if (!string.IsNullOrEmpty(imageTargetName) && imageTargetName == ImageTarget)
                return;

            ImageTarget = imageTargetName;

            UpdateMyClientInfo();
        }


        public void Enter(int sceneIndex)
        {

        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CurrentSceneIndex = scene.buildIndex;

        }

        private void ToggleGUI(bool active)
        {
            foreach (var splitScreenCamera in FindObjectsByType<SplitScreenCanvasCamera>(FindObjectsSortMode.None))
            {
                if (splitScreenCamera.transform.childCount > 0)
                    splitScreenCamera.transform.GetChild(0).gameObject.SetActive(active);
            }
        }

        private void UpdateMyClientInfo()
        {
            if (!isConnectionOpen())
                return;

            var msg = new WebsocketMessage
            {
                ID = ReadOnlyID,
                type = "CLIENTINFO",
                imageTarget = ImageTarget,
                data = JsonConvert.SerializeObject(new ClientInfo
                {
                    ID = ReadOnlyID,
                    imageTarget = ImageTarget
                })
            };

            Debug.Log($"Sending {JsonConvert.SerializeObject(msg)}");
            wscl.SendAsync(JsonConvert.SerializeObject(msg), _ => { });
        }

        private void ReportBatteryLevel()
        {
            if (!isConnectionOpen())
                return;

            var msg = new WebsocketMessage()
            {
                type = "BATTERY",
                data = $"{SystemInfo.deviceModel} {SystemInfo.deviceName}|{SystemInfo.batteryLevel.ToString()}"
            };

            wscl.SendAsync(JsonConvert.SerializeObject(msg), _ => { });
        }

        public void OnTMRISettings(TMRISettings settings)
        {
            StartWebSocket(settings);
        }
    }

    public class TMRISettings
    {
        public string ConfigurationURI;
        public string ServerIP;
        public TMRIState.SeeThroughMode SeeThroughMode;
    }

    public interface IConnectionStateListener
    {
        public void OnConnectionStateChange(WebSocketState state);
    }

}//namespace TMRI.Client