using System.Collections.Generic;
using System.Data;
using TMPro;
using TMRI.Client;
using UnityEngine;
using WebSocketSharp;

public class ConnectionStateListener : MonoBehaviour, IConnectionStateListener, ISettingsListener
{
    public TMP_Text connectionStateText;

    string lastIP;

    public Dictionary<WebSocketState?, string> StateColor = new ()
    {
        [WebSocketState.Closed] = "red",
        [WebSocketState.Open] = "green"
    };

    public void OnConnectionStateChange(WebSocketState state)
    {
        if (connectionStateText == null)
            return;

        if(state == WebSocketState.Connecting && !string.IsNullOrEmpty(lastIP))
            connectionStateText.text = $"Connecting to {lastIP}";
        else
            connectionStateText.text = $"<color={(StateColor.TryGetValue(state, out string col) ? col : "white")}>{state}</color>";
    }

    public void OnTMRISettings(TMRISettings settings)
    {
        lastIP = settings.ServerIP;
    }
}
