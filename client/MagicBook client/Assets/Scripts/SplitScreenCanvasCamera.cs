using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SplitScreenCanvasCamera : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if(SplitScreenCanvas.instance == null)
        {
            enabled = false;
            return;
        }

        //SplitScreenCanvas.instance.gameObject.SetActive(false);

        var canvasCopy = Instantiate(SplitScreenCanvas.instance.ScreenSpaceCameraCanvas, transform);
        canvasCopy.gameObject.SetActive(true);
        canvasCopy.renderMode = RenderMode.ScreenSpaceCamera;
        canvasCopy.worldCamera = GetComponent<Camera>();
        canvasCopy.planeDistance = Mathf.Max(0.2f, canvasCopy.worldCamera.nearClipPlane+0.01f);

        Destroy(canvasCopy.GetComponent<SplitScreenCanvas>());
    }

}
