using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMRI.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using TMRI.Client;

public class GameStateUI : MonoBehaviour
{
    public string imageTargetName;
    public int sceneIndex;
    public TMP_Text teamA;
    public TMP_Text teamB;
    public TMP_Text time;
    public UnityEvent<string, int> OnJoinTeam;
    public UnityEvent<int> OnEnter;

    private TMRIState mbgs;

    private void Start()
    {
        if (GetComponent<LookAtConstraint>() is LookAtConstraint lac && Camera.main != null)
            lac.AddSource(new ConstraintSource { sourceTransform = Camera.main.transform, weight = 0.5f });

        mbgs = FindObjectOfType<TMRIState>();
    }

    public void UpdateUI(ClientInfo myInfo, ClientInfo[] players)
    {
        var playersBase = players.Where(p => p.imageTarget == imageTargetName && myInfo.ID != p.ID);
        var playersText = playersBase.Where(p => p.team == 1).Select(p => p.ID.Substring(0,4)).ToList();

        if (myInfo.team == 1 && myInfo.imageTarget == imageTargetName)
            playersText.Add("<b>You</b>");

        teamA.text = $"Team 1: {string.Join(", ", playersText)}";

        playersText = playersBase.Where(p => p.team == 2).Select(p => p.ID.Substring(0, 4)).ToList();

        if (myInfo.team == 2 && myInfo.imageTarget == imageTargetName)
            playersText.Add("<b>You</b>");

        teamB.text = $"Team 2: {string.Join(", ", playersText)}";
    }

    private void Update()
    {
        //if (mbgs != null)
        //    time.text = mbgs.TimeInfo;

        if (MixedInput.ActionUp)
            JoinTeam(1);
        else if (MixedInput.SecondaryActionUp)
            JoinTeam(2);
        else if (MixedInput.TertiaryActionUp)
            Enter();
    }

    public void JoinTeam(int team)
    {
        OnJoinTeam?.Invoke(imageTargetName, team);
    }

    public void Enter()
    {
        OnEnter?.Invoke(sceneIndex);
    }
}
