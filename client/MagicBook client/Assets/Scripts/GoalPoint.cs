using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TMRI.Core;
using TMRI.Client;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class GoalPoint : TMRIStateCallback
{
    public List<string> PlayersAtGoal = new();
    [UniqueIdentifier]
    public string NetworkID;
    public int Team;

    SphereCollider myCollider;
    Renderer myRenderer;

    protected override string ID { get => NetworkID; set { } }

    // Start is called before the first frame update
    void Start()
    {
        myCollider = GetComponent<SphereCollider>();
        myCollider.isTrigger = true;
        myRenderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if(myRenderer != null)
        {
            myRenderer.material.color = PlayersAtGoal.Count == 0
                ? new Color(1,0,0, myRenderer.material.color.a) : new Color(0, 1, 0, myRenderer.material.color.a);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("GoalPoint OnTriggerEnter");
        if(other.gameObject.GetComponent<TMRIPlayer>() is TMRIPlayer pi)
        {
            if (!PlayersAtGoal.Contains(pi.ReadOnlyID))
            {
                PlayersAtGoal.Add(pi.ReadOnlyID);

                Send(new WebsocketMessage
                {
                    ID = this.ID,
                    type = "GOALPOINT",
                    data = JsonConvert.SerializeObject(PlayersAtGoal),
                });
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("GoalPoint OnTriggerExit");
        if (other.gameObject.GetComponent<TMRIPlayer>() is TMRIPlayer pi)
        {
            if (PlayersAtGoal.Contains(pi.ReadOnlyID))
            {
                PlayersAtGoal.Remove(pi.ReadOnlyID);

                Send(new WebsocketMessage
                {
                    ID = this.ID,
                    type = "GOALPOINT",
                    data = JsonConvert.SerializeObject(PlayersAtGoal),
                });
            }
        }
    }

    protected override void OnStateMessage(WebsocketMessage msg)
    {
        var playerIds = JsonConvert.DeserializeObject<List<string>>(msg.data);
        PlayersAtGoal = playerIds;
    }
}
