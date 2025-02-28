using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using TMRI.Client;
using TMRI.Core;

public class DrawOnMesh : TMRIStateCallback, ReticlePointerInteractable
{
    public LineRenderer drawingPrefab;
    public float ZOffset;
    public string NetworkID;
    public Material _gazeMaterial;

    Vector3? previousHit;
    //Dictionary<int, LineRenderer> networkDrawings = new Dictionary<int, LineRenderer>();

    // Key is hash of player ID and Index: 1234-5678_0
    Dictionary<string, LineRenderer> networkDrawings = new();

    //WebSocket wscl;
    //MagicBookClient mbc;
    //List<DrawOnMesh> localDrawOnMesh = new List<DrawOnMesh>();
    //System.Threading.SynchronizationContext main;

    

    //public string MessageType { get; set; } = "DRAW";
    //public string ID { get => NetworkID; set { } }

    public Material GazeMaterial { get; set; }
    protected override string ID { get => NetworkID; set { } }

    private void Start()
    {
        GazeMaterial = new Material(_gazeMaterial);

        var playerNumber = TMRIState.instance.GetPlayerNumber(TMRIState.instance.ReadOnlyID);
        GazeMaterial.color = TMRIState.instance.PlayerNumberColor[playerNumber];
    }


    public void AddLineDrawing(LineDrawing drawing, int playerNumber)
    {
        var key = $"{drawing.ID}_{drawing.Index}";
        if (!networkDrawings.ContainsKey(key))
        { 
            /*
            networkDrawings[key] = Instantiate(drawingPrefab);
            networkDrawings[key].transform.parent = transform;
            networkDrawings[key].transform.localScale = Vector3.one;
            networkDrawings[key].transform.localPosition = Vector3.zero;
            networkDrawings[key].material.color = playerNumberColor[playerNumber];
            networkDrawings[key].positionCount = drawing.Positions.Count;
            networkDrawings[key].SetPositions(drawing.Positions.Select(p => (Vector3)p).ToArray());*/
            networkDrawings[key] = InstantiateNewLine(playerNumber);
        }

        networkDrawings[key].positionCount = drawing.Positions.Count;
        networkDrawings[key].SetPositions(drawing.Positions.Select(p => (Vector3)p).ToArray());
    }

    public void OnDrag(RaycastHit hitInfo)
    {
        //if (!isActiveAndEnabled || mbc.XRContainer.mode == XRMode.VR)
        //    return;

        var myID = TMRIState.instance.ReadOnlyID;
        var offsetHit = transform.InverseTransformPoint(hitInfo.point + hitInfo.normal * ZOffset * hitInfo.transform.lossyScale.x);
        var index = networkDrawings.Where(nd => nd.Key.StartsWith(myID)).Count() - 1;
        var lineRenderer = networkDrawings[$"{myID}_{index}"];

        if ((lineRenderer.positionCount >= 1 &&
            Vector3.Distance(lineRenderer.GetPosition(lineRenderer.positionCount - 1), offsetHit) > .5f * hitInfo.transform.lossyScale.x) ||
            previousHit.HasValue && lineRenderer.positionCount == 0)
        {
            lineRenderer.positionCount += 1;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, offsetHit);
        }

        previousHit = offsetHit;

        var drawing = GetLastDrawing(myID);
        //if (wscl.ReadyState == WebSocketState.Open)
        {
            var msg = new WebsocketMessage
            {
                ID = this.ID,
                type = "DRAW",
                data = JsonConvert.SerializeObject(drawing)
            };
            Send(msg);
        }

        var playerNumber = TMRIState.instance.GetPlayerNumber(myID);
        GazeMaterial.color = TMRIState.instance.PlayerNumberColor[playerNumber];
    }

    public void OnDown(RaycastHit hitInfo)
    {
        var myID = TMRIState.instance.ReadOnlyID;
        var index = networkDrawings.Where(nd => nd.Key.StartsWith(myID)).Count();
        /*
        networkDrawings[index] = Instantiate(drawingPrefab, transform);
        //networkDrawings[index].useWorldSpace = true;
        networkDrawings[index].startWidth = networkDrawings[index].endWidth = 0.1f * networkDrawings[index].transform.lossyScale.x;
        //networkDrawings[index].transform.parent = transform;
        //networkDrawings[index].transform.localScale = Vector3.one;
        //networkDrawings[index].transform.localPosition = Vector3.zero;
        var playerNumber = MagicBookGameState.instance.GetPlayerNumber(MagicBookGameState.instance.ReadOnlyID);
        networkDrawings[index].material.color = playerNumberColor[playerNumber];

        Debug.Log($"Made new {networkDrawings[index].name} with localPos {networkDrawings[index].transform.localPosition} and localScale {networkDrawings[index].transform.localScale} and globalScale {networkDrawings[index].transform.lossyScale} and width {networkDrawings[index].startWidth}");*/
        networkDrawings[$"{TMRIState.instance.ReadOnlyID}_{index}"] = InstantiateNewLine(TMRIState.instance.GetPlayerNumber(myID));
    }

    public void OnUp(RaycastHit hitInfo)
    {
        var msg = new WebsocketMessage
        {
            ID = this.ID,
            type = "DRAW_FINISH",
        };
        Send(msg);
    }

    public void OnTapped(RaycastHit hitInfo)
    {
        
    }

    private LineRenderer InstantiateNewLine(int playerNumber)
    {
        var line = Instantiate(drawingPrefab, transform);

        line.startWidth = line.endWidth = 0.1f * line.transform.lossyScale.x;
        line.material.color = TMRIState.instance.PlayerNumberColor[playerNumber];

        Debug.Log($"Made new {line.name} with localPos {line.transform.localPosition} and localScale {line.transform.localScale} and globalScale {line.transform.lossyScale} and width {line.startWidth}");

        return line;
    }

    private LineDrawing GetLastDrawing(string id)
    {
        var index = networkDrawings.Where(nd => nd.Key.StartsWith(id)).Count() - 1;
        var lineRenderer = networkDrawings[$"{id}_{index}"];
        var linePositions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(linePositions);

        return new LineDrawing
        {
            Positions = linePositions.Select(lp => (SerializableVector3)lp).ToList(),
            Index = index,
            ID = id
        };
    }

    

    protected override void OnStateMessage(WebsocketMessage msg)
    {
        //if (!isActiveAndEnabled || mbc.XRContainer.mode == XRMode.AR)
        //    return;

        // When someone is actively drawing
        if(msg.type == "DRAW")
        {
            var drawing = JsonConvert.DeserializeObject<LineDrawing>(msg.data);
            var playerNumber = TMRIState.instance.GetPlayerNumber(drawing.ID);
            AddLineDrawing(drawing, playerNumber);
        }
        else // The absolute state of the drawings on this object
        {
            Debug.Log($"WebsocketMessage: id={msg.ID} type={msg.type} data={msg.data}");
            foreach (var nd in networkDrawings.Values)
            {
                if (nd == null)
                    continue;
                Destroy(nd.gameObject);
            }

            networkDrawings.Clear();

            var allDrawings = JsonConvert.DeserializeObject<List<LineDrawing>>(msg.data);
            //foreach (var drawing in allDrawings.Where(d => MagicBookGameState.instance.isPartOfMyTeam(d.ID)))
            //{
            //    var playerNumber = MagicBookGameState.instance.GetPlayerNumber(drawing.ID);
            //    AddLineDrawing(drawing, playerNumber);
            //}
        }
    }

}
