using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookatCamera : MonoBehaviour
{

    public Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Create a mirror camera based on mainCam and set the z vector to  look at it
        Vector3 mirrorCamPostion = this.transform.position + (this.transform.position - cam.transform.position);
        this.transform.LookAt(mirrorCamPostion); 
    }
}
