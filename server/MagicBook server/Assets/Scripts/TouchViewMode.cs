using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchViewMode : MonoBehaviour
{
    private float camMapCenterDis;// the distance between the camera and the map center
    private float yawAngle; //the angle between the camera-to-map Center and x axis
    private float pitchAngle; // the angle between the camera-to-map Center and x-z plane
    private float horizontalSensitivity = 0.5f;
    private float verticalSensitivity = 0.5f;

    private Camera cam;
    private WebsocketServerWithGUI serverManager; // the class that updates the map center used for camera to look at
    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        yawAngle = Mathf.Atan2(cam.transform.position.x, cam.transform.position.z); // the angle between the camera-to-map Center and x axis
        pitchAngle = Mathf.Atan2(cam.transform.position.y, Mathf.Sqrt(Mathf.Pow(cam.transform.position.z, 2) + Mathf.Pow(cam.transform.position.x, 2))); // the angle between the camera-to-map Center and x-z plane
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchSupported && Input.touchCount > 0)
        {   // touch screen
            HandleTouchViewMode();
        }
        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButton(0))
        {   // mouse-keyboard
            HandleMouseViewMode();
        }
        cam.transform.LookAt(serverManager.virtualMapPoint.transform);
    }

    void HandleTouchViewMode()
    {
        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Moved)
        {
            camMapCenterDis = Vector3.Distance(cam.transform.position, serverManager.virtualMapPoint.transform.position);
            yawAngle += touch.deltaPosition.x * horizontalSensitivity;
            pitchAngle += touch.deltaPosition.y * verticalSensitivity;
            cam.transform.position = new Vector3(camMapCenterDis * Mathf.Cos(pitchAngle) * Mathf.Sin(yawAngle),
                                                 camMapCenterDis * Mathf.Sin(pitchAngle),
                                                 camMapCenterDis * Mathf.Cos(pitchAngle) * Mathf.Cos(yawAngle));
        }
    }

    void HandleMouseViewMode()
    {
        camMapCenterDis = Vector3.Distance(cam.transform.position, serverManager.virtualMapPoint.transform.position);
        yawAngle += Input.GetAxis("Mouse X") * horizontalSensitivity;
        pitchAngle += Input.GetAxis("Mouse Y") * verticalSensitivity;
        cam.transform.position = new Vector3(camMapCenterDis * Mathf.Cos(pitchAngle) * Mathf.Sin(yawAngle),
                                             camMapCenterDis * Mathf.Sin(pitchAngle),                                      
                                             camMapCenterDis * Mathf.Cos(pitchAngle) * Mathf.Cos(yawAngle));
    }
}
