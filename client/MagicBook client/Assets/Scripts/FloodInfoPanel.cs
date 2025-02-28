using TMPro;
using UnityEngine;
using TMRI.Client;
using TMRI.Core;
using Newtonsoft.Json;

public class FloodInfoPanel : TMRIStateCallback, IFloodVisualizationListener
{
    public TMP_Text DepthText;
    public TMP_Text SpeedText;
    public TMP_Text AltitudeText;
    public string NetworkID;
    public bool ListenForNetworkUpdate = true;
    public LayerMask UpdateInfoRaycastLayerMask;

    public static FloodInfoPanel instance { get; private set; }
    public bool isDirty { get; set; }

    protected override string ID { get => NetworkID; set => NetworkID = value; }

    const string UPDATE_KEY = "POSITION_AND_INFO";

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Update()
    {
        if (isDirty)
        {
            UpdateFloodInfo();
            isDirty = false;
        }
    }

    public void SetFloodInfo(float? depth=null, float? speed=null, float? altitude=null)
    {
        if(depth != null)
            DepthText.text = $"{depth.Value.ToString("F3")} m";

        if(speed != null)
            SpeedText.text = $"{speed.Value.ToString("F3")} m/s";

        if (altitude == null)
        {
            altitude = transform.localPosition.y;
        }

        AltitudeText.text = $"{altitude.Value.ToString("F1")} m";
    }

    public void SetFloodInfo(float depth, float speed, bool active, bool withNotify=true)
    {

        SetFloodInfo(depth, speed);

        SetActive(active);

        if (withNotify)
        {
            var msg = new WebsocketMessage
            {
                type = UPDATE_KEY,
                data = JsonConvert.SerializeObject(new PositionAndFloodInfo
                {
                    position = (SerializableVector3)transform.localPosition,
                    depth = depth,
                    speed = speed,
                    enabled = active
                })
            };
            //Debug.Log($"Sending msg:: {msg.data}");
            Send(msg);
        }
    }

    public void SetActive(bool active)
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(active);
    }

    protected override void OnStateMessage(WebsocketMessage msg)
    {
        //Debug.Log($"msg received: {msg.data}");
        if (msg.type == UPDATE_KEY && ListenForNetworkUpdate)
        {
            var d = JsonConvert.DeserializeObject<PositionAndFloodInfo>(msg.data);
            transform.localPosition = d.position;

            SetFloodInfo(d.depth, d.speed, d.enabled, withNotify: false);
        }
    }

    public void SetFloodVisualizationType(FloodVisualizationType type)
    {
        
    }

    public void OnFloodTimeStepUpdated(int timeStep)
    {
        UpdateFloodInfo();
    }

    private void UpdateFloodInfo()
    {
        var hitSomething = Physics.Raycast(new Ray(transform.position + transform.up * 0.1f, -transform.up), out RaycastHit hitInfo, 10f, UpdateInfoRaycastLayerMask.value);

        if (hitSomething && hitInfo.collider.TryGetComponent(out FloodMeshInteractable activeFloodmesh))
        {
            var speed = activeFloodmesh.GetFloodValueAtRaycastHit(hitInfo, FloodVisualizationType.Speed);
            var depth = activeFloodmesh.GetFloodValueAtRaycastHit(hitInfo, FloodVisualizationType.Depth);

            SetFloodInfo(depth, speed);
        }
        else
        {
            SetFloodInfo(depth: 0f, speed: 0f);
        }
    }

}
