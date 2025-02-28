using System.Collections;
using System.Collections.Generic;
using TMRI.Client;
using UnityEngine;

public class VRSpawnTransform : MonoBehaviour
{
    public GameObject LinkedTransform;
    public string LinkedAssetName;

    public void OnInteraction()
    {
        var goalPosition = Vector3.zero;

        if (!string.IsNullOrEmpty(LinkedAssetName))
        {
            var vrContainer = GameObject.FindGameObjectWithTag("VR container");
            var linkedGO = vrContainer.transform.Find(LinkedAssetName);
            if (linkedGO != null)
            {
                Debug.Log($"Found {linkedGO.name}");
                goalPosition = linkedGO.transform.position;
            }
        }
        else
        {
            goalPosition = LinkedTransform.transform.position;
        }

        FindObjectOfType<ToggleXRMode>().TransitionToVR(goalPosition, transform.position);
    }
}
