using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Linq;

namespace TMRI.Core
{
    public class WebsocketServerRoom : MonoBehaviour
    {
        public int Port = 8082;
        public string Endpoint = "/room";
        public string LobbyEndpoint = "/lobby";
        public GameObject OptionalClientPrefab;

        protected WebSocketServer wssv;
        protected WebSocketServiceHost room;
        //WebSocketServiceHost lobby;

        protected IEnumerable<WebSocketSessionManager> sessionManagers
        {
            get
            {
                if (wssv == null)
                    return new List<WebSocketSessionManager>();
                else
                    return wssv.WebSocketServices.Hosts.Select(h => h.Sessions);
            }
        }

        protected void AllSessionsBroadcast(string data)
        {
            foreach (var sm in sessionManagers)
                sm.Broadcast(data);
        }
        protected void ImageTargetSessionsBroadcast(string imageTarget, string data)
        {
            foreach (var sm in sessionManagers.SelectMany(sm => sm.Sessions))
            {
                if (sm is DeviceWebSocketBehaviour dwb && dwb.imageTargetName == imageTarget)
                {
                    //if (sm is RoomConnectedClient)
                    //    room.Sessions.SendTo(data, sm.ID);
                    //else if (sm is LobbyConnectedClient)
                    //    lobby.Sessions.SendTo(data, sm.ID);
                    dwb.SendToMe(data);
                }
            }

        }

        protected class State
        {
            //string imageTarget;
            public float time;
            public bool timePause;
            Dictionary<string, string> keyValuePairs = new();

            public bool ContainsKey(string key) => keyValuePairs.ContainsKey(key);
            public string this[string key]
            {
                get => keyValuePairs[key];
                set => keyValuePairs[key] = value;
            }

            public Dictionary<string, string>.Enumerator GetEnumerator()
            {
                return keyValuePairs.GetEnumerator();
            }

            public void Clear()
            {
                keyValuePairs.Clear();
            }
        }

        protected Action doInUnityThread;
        protected bool initialized;
        protected Dictionary<string, RoomConnectedClient> clients = new Dictionary<string, RoomConnectedClient>();
        //static Dictionary<string, string> state = new();
        protected static Dictionary<string, State> state = new();
        //static Dictionary<string, float> imageTargetTime = new();
        //static Dictionary<string, bool> imageTargetTimePause = new();

        Dictionary<string, string> timeInputs = new();
        const float defaultTime = 240f;

        protected string FullAddress => $"ws{(wssv.IsSecure ? "s" : "")}://{(wssv.Address.IsLocal() ? "127.0.0.1" : wssv.Address)}:{Port}{Endpoint}";

        public class DeviceWebSocketBehaviour : WebSocketBehavior
        {
            public float batteryLevel = -1f;
            public string deviceInfo;
            public string imageTargetName;
            public int team;

            public void SendToMyRoom(string data)
            {
                foreach (DeviceWebSocketBehaviour c in Sessions.Sessions.Cast<DeviceWebSocketBehaviour>())
                {
                    if (c.imageTargetName == this.imageTargetName && c.ConnectionState == WebSocketState.Open)
                    {
                        c.SendAsync(data, _ => { });
                    }
                }
            }

            public void SendToMe(string data)
            {
                SendAsync(data, _ => { });
            }
        }

        public class RoomConnectedClient : DeviceWebSocketBehaviour
        {
            public Pose pose;
            public int animationState;
            public GameObject gameObject;
            public XRMode mode;
            public int playerNumber;
            public Dictionary<int, List<SerializableVector3>> lines = new();

            public override string ToString()
            {
                return $"PN:{playerNumber} IT:{imageTargetName} TM:{team} Mode:{mode} AS:{animationState}";
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                if (e != null && e.IsText && e.Data != null)
                {
                    var msg = JsonConvert.DeserializeObject<WebsocketMessage>(e.Data);

                    if (msg.type == "POSE")
                    {
                        var p = JsonConvert.DeserializeObject<SerializablePose>(msg.data);
                        pose = new Pose(p.Position, p.Rotation);
                        mode = p.Mode;
                        animationState = p.State;
                    }
                    else if (msg.type == "ID")
                    {
                        // Echo ID
                        msg.data = this.ID;
                        msg.ID = this.ID;
                        msg.imageTarget = this.imageTargetName;
                        Send(JsonConvert.SerializeObject(msg));
                    }

                    else if (msg.type == "HIT")
                    {
                        //var hitData = JsonConvert.DeserializeObject<HitData>(msg.data);

                        SendToMyRoom(JsonConvert.SerializeObject(msg));
                    }
                    else if (msg.type == "DRAW")
                    {
                        // Happens when someone actively is drawing
                        var drawing = JsonConvert.DeserializeObject<LineDrawing>(msg.data);
                        lines[drawing.Index] = drawing.Positions;

                        SendToMyTeam(e.Data);
                    }
                    else if (msg.type == "DRAW_FINISH")
                    {
                        // Construct list of all player drawings and store in the given state ID
                        var allDrawings = GetAllClientDrawings();
                        state[imageTargetName][$"{msg.ID}"] = JsonConvert.SerializeObject(allDrawings);

                        msg.data = state[imageTargetName][$"{msg.ID}"];

                        SendToMyRoom(JsonConvert.SerializeObject(msg));
                    }
                    else if (msg.type == "CLIENTINFO")
                    {
                        var clientInfo = JsonConvert.DeserializeObject<ClientInfo>(msg.data);
                        imageTargetName = clientInfo.imageTarget;
                        team = clientInfo.team;

                        msg.ID = ID;//Just in case

                        Sessions.Broadcast(JsonConvert.SerializeObject(msg));

                        // Check if there's a timer running, if not set it, if below zero reset it
                        //if (!imageTargetTime.ContainsKey(imageTargetName) || imageTargetTime[imageTargetName] <= 0f)
                        if (!state.ContainsKey(imageTargetName))
                        {
                            state[imageTargetName] = new State
                            {
                                time = defaultTime,
                            };
                        }
                    }
                    else if (msg.type == "BATTERY")
                    {
                        var parsed = msg.data.Split('|');
                        deviceInfo = parsed[0];
                        if (float.TryParse(parsed[1], out float bl))
                            batteryLevel = bl;
                    }
                    else if (msg.type == "STATE")
                    {
                        if (!string.IsNullOrEmpty(msg.ID) && !string.IsNullOrEmpty(imageTargetName) && state[imageTargetName].ContainsKey(msg.ID))
                        {
                            msg.data = state[imageTargetName][msg.ID];
                            Send(JsonConvert.SerializeObject(msg));
                        }
                    }
                    else if(!string.IsNullOrEmpty(msg.ID) && !string.IsNullOrEmpty(imageTargetName))
                    {
                        // Save it in state dictionary
                        state[imageTargetName][msg.ID] = msg.data;
                        SendToMyRoom(JsonConvert.SerializeObject(msg));
                    }
                }
            }

            private List<LineDrawing> GetAllClientDrawings()
            {
                var allPlayers = Sessions.Sessions.Cast<RoomConnectedClient>()
                    .Where(c => c.imageTargetName == imageTargetName)
                    .Select(c => new { c.ID, c.lines });
                var allDrawings = allPlayers.SelectMany(p => p.lines.Select(l => new LineDrawing
                {
                    ID = p.ID,
                    Index = l.Key,
                    Positions = l.Value
                })).ToList();

                return allDrawings;
            }

            public void SendToMyTeam(string data)
            {
                foreach (RoomConnectedClient c in Sessions.Sessions.Cast<RoomConnectedClient>())
                {
                    if (c.team == this.team && c.imageTargetName == this.imageTargetName)
                    {
                        c.SendAsync(data, _ => { });
                    }
                }
            }

            protected override void OnOpen()
            {
                base.OnOpen();
                Debug.Log($"Client {this.ID} connected");

                var clients = this.Sessions.Sessions.Cast<RoomConnectedClient>().Select(s => new ClientInfo
                {
                    ID = s.ID,
                    imageTarget = s.imageTargetName,
                    team = s.team,
                    lines = s.lines.Select(l => new LineDrawing { ID = s.ID, Index = l.Key, Positions = l.Value }).ToList()
                });

                var msg = new WebsocketMessage
                {
                    ID = this.ID,
                    type = "INIT",
                    data = JsonConvert.SerializeObject(clients.ToArray())
                };
                Debug.Log(JsonConvert.SerializeObject(msg));
                Send(JsonConvert.SerializeObject(msg));

            }

            protected override void OnClose(CloseEventArgs e)
            {
                base.OnClose(e);

                Debug.Log($"Client {this.ID} disconnected");
            }

            protected override void OnError(ErrorEventArgs e)
            {
                base.OnError(e);
                Debug.LogError(e.Message);
                Debug.LogError(e.Exception);
            }

            public void Start(GameObject fromPrefab = null)
            {
                if (fromPrefab != null)
                {
                    gameObject = Instantiate(fromPrefab);
                    gameObject.name = this.ID;
                }
                else
                {
                    gameObject = new GameObject(this.ID);
                }
            }

            public void Update()
            {
                gameObject.transform.SetPositionAndRotation(pose.position * 0.1f, pose.rotation);
            }

            public void OnDestroy()
            {
                Destroy(gameObject);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            DontDestroyOnLoad(gameObject);
            wssv = new WebSocketServer(Port, secure: false);
            wssv.AddWebSocketService<RoomConnectedClient>(Endpoint, (ts) =>
            {
                Debug.Log($"RoomConnectedClient {ts.ID} initialization...");
            });
            /*wssv.AddWebSocketService<LobbyConnectedClient>(LobbyEndpoint, (ts) =>
            {
                Debug.Log($"LobbyConnectedClient {ts.ID} initialization...");
            });*/

            wssv.Start();
            if (!wssv.WebSocketServices.TryGetServiceHost(Endpoint, out room))
            {
                Debug.LogError("couldn't get websocket server host");
            }
            /*if (!wssv.WebSocketServices.TryGetServiceHost(LobbyEndpoint, out lobby))
            {
                Debug.LogError("couldn't get websocket server host");
            }*/

            InvokeRepeating(nameof(ReportTime), 1f, 1f);
        }

        private void OnDestroy()
        {
            if (wssv != null)
            {
                wssv.Stop();
            }
        }

        private void ReportTime()
        {
            //if (state.ContainsKey("time"))
            foreach (var itt in state)
            {
                var msg = new WebsocketMessage()
                {
                    type = "TIME",
                    imageTarget = itt.Key,
                    data = itt.Value.time.ToString()//state["time"]
                };
                ImageTargetSessionsBroadcast(itt.Key, JsonConvert.SerializeObject(msg));
            }

        }

        // Update is called once per frame
        protected virtual void Update()
        {
            if (!initialized && wssv != null && wssv.IsListening)
            {
                Debug.Log($"Websocket server listening on {FullAddress}");
                initialized = true;
            }

            //if(state.TryGetValue("time", out string t) && float.TryParse(t, out float time))
            foreach (var itt in state)
            {
                if (!room.Sessions.Sessions.Any(s => ((DeviceWebSocketBehaviour)s).imageTargetName == itt.Key)
                    || itt.Value.timePause)
                    continue;

                if (itt.Value.time > 0f)
                    itt.Value.time -= Time.deltaTime;
                //state["time"] = time.ToString();
            }

            //foreach(var clientID in wssh.Sessions.IDs)
            foreach (var session in sessionManagers.SelectMany(sm => sm.Sessions))
            {
                //if (wssh.Sessions.TryGetSession(clientID, out IWebSocketSession s) && s is RoomConnectedClient)
                if (session is RoomConnectedClient client)
                {
                    //var client = s as RoomConnectedClient;
                    var clientID = client.ID;

                    if (!clients.ContainsKey(clientID))
                    {
                        client.Start(OptionalClientPrefab);

                        var startMsg = new WebsocketMessage()
                        {
                            type = "START",
                            ID = clientID,
                            imageTarget = client.imageTargetName,
                        };
                        room.Sessions.Broadcast(JsonConvert.SerializeObject(startMsg));
                        //client.SendToMyRoom(JsonConvert.SerializeObject(startMsg));
                    }

                    clients[clientID] = client;

                    client.Update();

                    var msg = new WebsocketMessage()
                    {
                        type = "UPDATE",
                        ID = clientID,
                        imageTarget = client.imageTargetName,
                        data = JsonConvert.SerializeObject(new SerializablePose
                        {
                            Position = (SerializableVector3)client.pose.position,
                            Rotation = (SerializableQuaternion)client.pose.rotation,
                            Mode = client.mode,
                            State = client.animationState
                        })
                    };
                    //wssh.Sessions.Broadcast(JsonConvert.SerializeObject(msg));
                    client.SendToMyRoom(JsonConvert.SerializeObject(msg));
                }

            }

            var disconnectedClients = clients.Where(c => !room.Sessions.IDs.Contains(c.Key)).Select(c => c.Value).ToArray();
            //var disconnectedClients = clients.Where(c => !sessionManagers.SelectMany(sm => sm.IDs).Contains(c.Key)).Select(c => c.Value).ToArray();
            foreach (var client in disconnectedClients)
            {
                client.OnDestroy();

                var msg = new WebsocketMessage()
                {
                    type = "DESTROY",
                    ID = client.ID,
                    imageTarget = client.imageTargetName,
                };

                room.Sessions.Broadcast(JsonConvert.SerializeObject(msg));
                //client.SendToMyRoom(JsonConvert.SerializeObject(msg));

                clients.Remove(client.ID);
            }

            doInUnityThread?.Invoke();
            doInUnityThread = null;
        }

        Dictionary<string, string> teamInputs = new();
        protected void OnGUI()
        {
            lineY = 0f;
            var normalStyle = new GUIStyle();
            normalStyle.normal.textColor = Color.white;
            normalStyle.fontSize = 32;
            var errorStyle = new GUIStyle();
            errorStyle.normal.textColor = Color.red;
            errorStyle.fontSize = 32;

            if (wssv == null)
            {
                AddLabel("MagicBook server NOT RUNNING", errorStyle);

                return;
            }

            AddLabel("MagicBook server", normalStyle);
            AddLabel($"Status:\t\t{(wssv.IsListening ? "listening on" : "NOT LISTENING")} {FullAddress}", wssv.IsListening ? normalStyle : errorStyle);

            AddLabel("State controls", normalStyle);
            if (GUI.Button(new Rect(900, lineY + 25, 150, 30), "Restart"))
            {
                state.Clear();
                var msg = new WebsocketMessage()
                {
                    type = "RESTART",
                };
                //wssh.Sessions.Broadcast(JsonConvert.SerializeObject(msg));
                AllSessionsBroadcast(JsonConvert.SerializeObject(msg));
            }

            foreach (var itt in state)
            {
                AddLabel($"{itt.Key}: {itt.Value.time}", normalStyle);
                timeInputs[itt.Key] = GUI.TextField(new Rect(850, lineY + 25, 150, 30),
                    timeInputs.TryGetValue(itt.Key, out string tv) ? tv : defaultTime.ToString());

                if (GUI.Button(new Rect(1000, lineY + 25, 150, 30), "Reset time"))
                {
                    itt.Value.time = float.TryParse(timeInputs[itt.Key], out float t) ? t : defaultTime;
                }
                if (GUI.Button(new Rect(1150, lineY + 25, 150, 30), "Pause/resume"))
                {
                    //if (imageTargetTimePause.TryGetValue(itt, out bool p))
                    //    imageTargetTimePause[itt] = !p;
                    //else imageTargetTimePause[itt] = true;
                    itt.Value.timePause = !itt.Value.timePause;
                }
            }


            int i = 0;
            /*
            AddLabel($"# devices:\t{lobby.Sessions.Count} ({lobby.Sessions.InactiveIDs.Count()} inactive)", lobby.Sessions.ActiveIDs.Count() < 3 ? normalStyle : normalStyle);
            AddLabel("-----------", normalStyle);
            foreach(var c in lobby.Sessions.Sessions)
            {
                var client = c as DeviceWebSocketBehaviour;
                AddLabel($"[{i}] {client.deviceInfo} \timageTarget: {client.imageTargetName} ({client.team}) \tBattery: {client.batteryLevel}", client.batteryLevel < 0.2f ? errorStyle : normalStyle);
            }*/

            AddLabel($"# in room:\t{room.Sessions.Count} ({room.Sessions.InactiveIDs.Count()} inactive)", normalStyle);
            AddLabel("-----------", normalStyle);
            i = 0;
            foreach (var c in room.Sessions.Sessions)
            {
                var client = c as RoomConnectedClient;
                AddLabel($"[{i}] {client.deviceInfo} \timageTarget: {client.imageTargetName} ({client.team}) \tBattery: {client.batteryLevel}", client.batteryLevel < 0.2f ? errorStyle : normalStyle);

                teamInputs[client.ID] = GUI.TextField(new Rect(1000, lineY + 25, 150, 30),
                    teamInputs.TryGetValue(client.ID, out string tv) ? tv : client.team.ToString());

                if (GUI.Button(new Rect(1150, lineY + 25, 150, 30), "Team"))
                {
                    client.team = int.Parse(teamInputs[client.ID]);
                    var msg = new WebsocketMessage
                    {
                        ID = client.ID,
                        imageTarget = client.imageTargetName,
                        type = "CLIENTINFO",
                        data = JsonConvert.SerializeObject(new ClientInfo
                        {
                            ID = client.ID,
                            imageTarget = client.imageTargetName,
                            team = client.team
                        })
                    };

                    client.SendToMyRoom(JsonConvert.SerializeObject(msg));
                }
            }

            AddLabel("State:", normalStyle);
            foreach (var s in state)
            {
                AddLabel(s.Key, normalStyle);
                if (GUI.Button(new Rect(500, lineY + 25, 150, 30), "Clear"))
                {
                    s.Value.Clear();
                }
                foreach (var stateValue in s.Value)
                    AddLabel($"{stateValue.Key}: {stateValue.Value}", normalStyle);
                AddLabel("-----------", normalStyle);
            }
        }

        float lineY = 0f;
        private void AddLabel(string text, GUIStyle style)
        {
            const float xOffset = 50, yOffset = 50;
            float lineOffset = 30 + Screen.height / 500;
            GUI.Label(new Rect(xOffset, yOffset + lineY, 500, 500), text, style);
            lineY += lineOffset;
        }
    }

} //namespace TMRI.Core