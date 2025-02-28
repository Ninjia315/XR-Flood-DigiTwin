using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMRI.Core;
using UnityEngine;
using WebSocketSharp;

namespace TMRI.Client
{
    /// <summary>
    /// Base class for all objects wishing to receive network callbacks from the TMRI system.
    /// </summary>
    public abstract class BaseTMRICallback : MonoBehaviour
    {
        WebSocket wscl;
        System.Threading.SynchronizationContext main;

        public bool isConnectionOpen() => wscl != null && wscl.ReadyState == WebSocketState.Open;

        protected virtual void doOnWebsocketConnected(WebSocket w)
        {
            if (wscl != null)
                wscl.OnMessage -= OnMessageAsync;
            wscl = w;
            wscl.OnMessage += OnMessageAsync;
        }

        public virtual void OnEnable()
        {
            void doOnStateInstance(TMRIState inst)
            {
                var websocket = inst;
                if (websocket.wscl == null)
                {
                    websocket.OnWebSocketSet += doOnWebsocketConnected;
                }
                else
                {
                    doOnWebsocketConnected(websocket.wscl);
                }
            }

            if (TMRIState.instance == null)
            {
                TMRIState.OnInstance += doOnStateInstance;
            }
            else
            {
                doOnStateInstance(TMRIState.instance);
            }

            main = System.Threading.SynchronizationContext.Current;
        }

        public virtual void OnDisable()
        {
            if (TMRIState.instance != null && wscl != null)
                wscl.OnMessage -= OnMessageAsync;
        }

        private void OnMessageAsync(object sender, MessageEventArgs e)
        {
            main.Post(_ =>
            {
                var msg = JsonConvert.DeserializeObject<WebsocketMessage>(e.Data);
                OnTMRIMessage(msg);
            }, null);
        }

        protected void Send(WebsocketMessage msg)
        {
            if (isConnectionOpen())
                wscl.SendAsync(JsonConvert.SerializeObject(msg), _ => { });
        }

        protected abstract void OnTMRIMessage(WebsocketMessage msg);
    }

    /// <summary>
    /// A base class for objects who's properties are shared accross the network session.
    /// An object inheriting from this base will use the STATE-type websocket message.
    /// </summary>
    public abstract class TMRIStateCallback : BaseTMRICallback
    {
        /// <summary>
        /// The network ID that will be used to determine if this object is shared or not.
        /// </summary>
        protected abstract string ID { get; set; }
        protected abstract void OnStateMessage(WebsocketMessage msg);

        protected override void doOnWebsocketConnected(WebSocket w)
        {
            base.doOnWebsocketConnected(w);

            if (isConnectionOpen())
            {
                Send(new WebsocketMessage()
                {
                    ID = ID,
                    type = "STATE"
                });
            }
        }

        protected override void OnTMRIMessage(WebsocketMessage msg)
        {
            if (msg.ID == ID)
                OnStateMessage(msg);
        }
    }
}//namespace TMRI.Client