using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class CameraZoom : MonoBehaviour
{
    public float zoomSpeed = 1f;
    public float lookSpeed = 5f;

    private Vector2 zoomInput;
    private Vector2 lookInput;
    private bool rotate = false;
    private float yaw;
    private float pitch;
    private Quaternion initialRot;
    private WebsocketServerWithGUI serverManager;
    public UnityEvent ResetCam;

    // Start is called before the first frame update
    void Start()
    {
        initialRot = transform.rotation; //record the initial rotation of the camera
        yaw = initialRot.eulerAngles.y;
        pitch = initialRot.eulerAngles.x;
        serverManager = FindObjectOfType<WebsocketServerWithGUI>();
        ResetCam.AddListener(ResetToInitialRotation); //Reset the camera to its initial rotation
    }

    //Input action for middle mouse button to zoom in and out
    public void ZoomInOut(InputAction.CallbackContext context)
    {
        zoomInput = context.ReadValue <Vector2>();
    }

    //Input action for mouse delta to look around
    public void LookAround(InputAction.CallbackContext context)
    {
        lookInput =context.ReadValue<Vector2>();
    }

    //Input action for right mouse button to activate look mode
    public void ActivateLookMode(InputAction.CallbackContext context)
    {
        rotate = context.ReadValueAsButton();
    }

    // Update is called once per frame
    private void Update()
    {
        //Look around
        //if (rotate)
        //{
        //    yaw += lookInput.x *lookSpeed * Time.deltaTime;
        //    pitch -= lookInput.y *lookSpeed * Time.deltaTime;
        //    pitch =Mathf.Clamp(pitch, -89f, 89f);  //Clamp the pitch to prevent camera flipping
        //    transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        //}


        //Zoom in and out
        float zoomBalancedSpeed = zoomSpeed * Time.deltaTime;

        if (zoomInput.y != 0)
        {
            //Debug.Log("ZoomY :" + zoomInput.y);
            Vector3 lastPos = transform.position;
            //Debug.Log("LastPosX :" + lastPos.x);
            transform.position += zoomInput.y * transform.forward * zoomBalancedSpeed;
            serverManager.mapPointUpdated?.Invoke(true); // when the camera zooms in or out, Reculate the cam-to-map center distance
            //Debug.Log("CurrentPosX :" + transform.position.x);
        }

    }

    private void ResetToInitialRotation()
    {
        yaw = initialRot.eulerAngles.y;
        pitch = initialRot.eulerAngles.x;
        transform.rotation = initialRot;
    }
}
