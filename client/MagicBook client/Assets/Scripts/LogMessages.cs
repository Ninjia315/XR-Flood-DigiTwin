using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LogMessages : MonoBehaviour
{
    public UnityEvent<Vector3> OnPosition;

    public void OnMessage(object obj){
        Debug.Log($"Log hands: Got message from {obj}");
    }

    public void OnTrackingAquired(){
        Debug.Log("Log hands: On tracking aquired");
    }
    public void OnTrackingLost(){
        Debug.Log("Log hands: On tracking lost");
    }
    public void OnPose(Pose obj){
        Debug.Log($"Log hands: Got message with pose {obj} with {obj.position} and {obj.rotation}");
        OnPosition?.Invoke(obj.position);
    }

    public void OnTrackingChanged(bool obj){
        Debug.Log($"Log hands: Got tracking changed: {obj}");
    }
}
