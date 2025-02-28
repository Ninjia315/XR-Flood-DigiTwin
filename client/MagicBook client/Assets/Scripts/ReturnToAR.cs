using System.Collections;
using System.Collections.Generic;
using TMRI.Client;
using UnityEngine;

public class ReturnToAR : MonoBehaviour
{
    public void OnInteraction()
    {
        FindObjectOfType<ToggleXRMode>().TransitionToAR();
    }
}
