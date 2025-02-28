using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using TMRI.Core;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;

namespace TMRI.Client
{
    public class TMRIPlayer : BaseTMRICallback
    {

        // User State
        public AnimationState UserState = 0;

        // 0:"idle", 1:"Walking", 2:"Running", 3:"Waving", 4:"IceBall", 5:"damage"
        public enum AnimationState
        {
            Idle,
            Walking,
            Running,
            Waving,
            IceBall,
            Damage
        }

        public string ReadOnlyID => TMRIState.instance.ReadOnlyID;
        public ToggleXRMode XRContainer;
        public float ARModeScale = 1f;
        public float VRModeScale = 1f;
        public float ARModeOffset = 0.3f;
        public float VRModeOffset = 0f;

        Dictionary<string, GameObject> otherPlayers => TMRIState.instance.otherPlayers;


        public bool isMe(string id) => ReadOnlyID == id;
        public bool isOther(string id) => !string.IsNullOrEmpty(ReadOnlyID) && !isMe(id);
     
        //public int GetPlayerNumber(string id) => isConnectionOpen() ? otherPlayers.Keys.Append(ReadOnlyID).OrderBy(i => i).ToList().IndexOf(id) : -1;


        private void Start()
        {
            if (XRContainer == null)
                XRContainer = GetComponent<ToggleXRMode>();
        }


        protected override void OnTMRIMessage(WebsocketMessage msg)
        {

            switch (msg.type)
            {

                case "UPDATE":
                    // Received a notification that a client's pose is updated.
                    // Update our instance of that client.

                    if (isOther(msg.ID) && otherPlayers.ContainsKey(msg.ID) && XRContainer.IsXRReady)
                    {
                        var pose = JsonConvert.DeserializeObject<SerializablePose>(msg.data);

                        //TODO
                        //if (otherPlayers[msg.ID].GetComponentInChildren<FaceController>() is FaceController fc)
                        //{
                        //    fc.OnNewPose(pose);
                        //}
                        //else
                        if (otherPlayers[msg.ID].GetComponentInChildren<Animator>() is Animator a)
                        {
                            var s = (AnimationState)pose.State;
                            a.SetBool("isWalking", s == AnimationState.Walking);
                            a.SetBool("isExtinguishing", s == AnimationState.IceBall);
                            var avatarChildRoot = otherPlayers[msg.ID].transform.GetChild(0);
                            avatarChildRoot.localPosition = new Vector3(0, -1.5f, 0);

                            if (avatarChildRoot.GetComponentInChildren<ParticleSystem>() is ParticleSystem ps)
                            {
                                if (s == AnimationState.IceBall)
                                {
                                    ps.Play(withChildren: true);
                                }
                                else
                                {
                                    ps.Stop(withChildren: true);
                                }
                            }
                            //if(otherPlayers[msg.ID].GetComponentInChildren<PlayerHighlighter>() is PlayerHighlighter ph)
                            //{
                            //    if (s == AnimationState.Waving)
                            //        ph.StartHighlight();
                            //    else
                            //        ph.StopHighlight();
                            //}
                        }

                        if (otherPlayers[msg.ID].GetComponent<PlayerIdentifier>() is PlayerIdentifier pi)
                            pi.mode = pose.Mode;

                        var activeXRTransform = XRContainer.GetActiveXRModeTransform();
                        var worldPos = activeXRTransform.TransformPoint(pose.Position);
                        var worldRot = activeXRTransform.rotation * pose.Rotation;

                        otherPlayers[msg.ID].transform.position = worldPos;
                        otherPlayers[msg.ID].transform.rotation = worldRot;

                        if (XRContainer.mode == XRMode.VR)
                        {
                            var offset = pose.Mode == XRMode.AR ? ARModeOffset : VRModeOffset;
                            otherPlayers[msg.ID].transform.Translate(-Vector3.up * offset, Space.World);
                        }

                        //Deactivate other avatar when we are both in AR mode
                        otherPlayers[msg.ID].SetActive(XRContainer.mode == XRMode.VR || pose.Mode == XRMode.VR);
                        otherPlayers[msg.ID].transform.eulerAngles = new Vector3(0, otherPlayers[msg.ID].transform.eulerAngles.y, 0);
                        otherPlayers[msg.ID].transform.localScale = activeXRTransform.localScale * (pose.Mode == XRMode.AR ? ARModeScale : VRModeScale);

                    }
                    break;

                case "HIT":
                    // Received a notification that a client's hit.
                    // Update our instance of that client.

                    if (isOther(msg.ID) && otherPlayers.ContainsKey(msg.ID))
                    {
                        Animator animator = otherPlayers[msg.ID].GetComponentInChildren<Animator>();
                        if (animator != null)
                        {
                            animator.SetInteger("State", 5);
                        }
                    }
                    break;

                case "CLIENTINFO":
                    /*UnityEngine.Debug.Log("Got client info: " + msg.data);
                    var clientInfo = JsonConvert.DeserializeObject<ClientInfo>(msg.data);

                    if (isOther(msg.ID) && otherPlayers.ContainsKey(msg.ID))
                    {
                        UnityEngine.Debug.Log("It is other");
                        var pi = otherPlayers[msg.ID].GetComponent<PlayerIdentifier>();
                        pi.imageTarget = clientInfo.imageTarget;
                        pi.team = clientInfo.team;
                        pi.gameObject.SetActive(pi.imageTarget == activeImageTarget);
                    }
                    else if (isMe(msg.ID))
                    {
                        UnityEngine.Debug.Log("It is me");
                        activeImageTarget = clientInfo.imageTarget;
                        activeTeam = clientInfo.team;

                        foreach(var player in otherPlayers.Values)
                        {
                            player.SetActive(player.GetComponent<PlayerIdentifier>().imageTarget == activeImageTarget);
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Do not know " + msg.ID);
                        // Add the new client ID to the map and set its index
                        clientIndexMap[msg.ID] = otherPlayers.Count;
                        // Instantiate the corresponding prefab
                        otherPlayers[msg.ID] = Instantiate(OtherPlayerPrefabs[clientIndexMap[msg.ID] % OtherPlayerPrefabs.Count]);

                        // Attach PlayerIdentifier component and set the ID
                        var identifier = otherPlayers[msg.ID].AddComponent<PlayerIdentifier>();
                        identifier.playerID = msg.ID;
                        identifier.imageTarget = msg.imageTarget;
                        identifier.team = clientInfo.team;
                        identifier.gameObject.SetActive(identifier.imageTarget == activeImageTarget);
                    }*/

                    break;

            }

        }

        private void Update()
        {
            if (isConnectionOpen() && XRContainer.IsXRReady)
            {
                var pos = XRContainer.GetActiveXRModeTransform().InverseTransformPoint(transform.position);
                var rot = Quaternion.Inverse(XRContainer.GetActiveXRModeTransform().rotation) * transform.rotation;
                var msg = new WebsocketMessage()
                {
                    ID = ReadOnlyID,
                    type = "POSE",
                    data = JsonConvert.SerializeObject(new SerializablePose()
                    {
                        Position = (SerializableVector3)pos,
                        Rotation = (SerializableQuaternion)rot,
                        Mode = XRContainer.mode,
                        State = (int)UserState // 0:"idle", 1:"Walking", 2:"Running", 3:"Waving", 4:"IceBall", 5:"damage"
                    })
                };

                Send(msg);
            }

        }

    }

}//namespace TMRI.Client
