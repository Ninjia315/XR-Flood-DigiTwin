using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMRI.Client;
using TMRI.Core;
using UnityEngine;
using UnityEngine.Events;

namespace TMRI.Client
{
    public class TMRIScene : BaseTMRICallback
    {
        public Transform Container;
        public GameObject ResizableDisplayPrefab;
        public Vector3 DefaultDisplaySize = Vector3.one;
        public UnityEvent<Vector3> OnClippingBounds;
        public UnityEvent<Vector3> OnPositionUpdate;

        public Vector3 DisplaySizeWorld => DisplaySizeGO.transform.lossyScale;
        public bool IsDefaultSize => DisplaySizeGO.transform.localScale == DefaultDisplaySize;

        public GameObject DisplaySizeGO { get; private set; }

        private void Awake()
        {
            if (Container == null)
                Container = transform;

            DisplaySizeGO = Instantiate(ResizableDisplayPrefab);
            DisplaySizeGO.transform.SetParent(transform, false);

            ChangeDisplaySize();
        }

        protected override void OnTMRIMessage(WebsocketMessage msg)
        {
            switch (msg.type)
            {
                case "ASSETANDSIZEUPDATE":

                    string[] msgInfo = msg.data.Split("|");
                    foreach (var info in msgInfo)
                    {
                        var tmp = info.Split("=");
                        var markerID = tmp[0];
                        var assetID = tmp[1];
                        var scale = float.Parse(tmp[2]);

                        Container.localScale = new Vector3(scale, scale, scale);
                    }
                    break;
                case "DISPLAYSIZEUPDATE":
                    if (JsonConvert.DeserializeObject<SerializableVector3>(msg.data) is SerializableVector3 size)
                    {
                        ChangeDisplaySize((Vector3)size);
                    }
                    break;
                case "MAPPOSITIONUPDATE":
                    Container.localPosition = JsonConvert.DeserializeObject<SerializableVector3>(msg.data);
                    OnPositionUpdate?.Invoke(Container.localPosition);
                    break;

                case "MAPROTATIONUPDATE":
                    Container.localRotation = (Quaternion)(JsonConvert.DeserializeObject<SerializableQuaternion>(msg.data));
                    break;

            }
        }

        public void ChangeDisplaySize(Vector3? size = null)
        {
            DisplaySizeGO.transform.localScale = size.HasValue ? size.Value : DefaultDisplaySize;
            OnClippingBounds?.Invoke(DisplaySizeGO.transform.localScale);
        }

    }

}