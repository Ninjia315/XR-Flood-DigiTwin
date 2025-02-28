using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DragMap : MonoBehaviour
{
    private bool isDragging = false; // Is the object being dragged?
    private bool isRotating = false; // Is the object being rotated?
    public bool isMapControl = true; // Is the map being controlled (map control mode)? otherwise, the camera is being controlled (view mode)
    //private bool isPinching = false; // Is the Cam being pinched?
    //private bool isLookAround = false; // Is the Cam looking around?
    //public float rotationSpeed = 5f; // The speed at which the object is rotated
    [SerializeField]
    private float lookSpeed = 0.5f; // The speed at which the camera is looking around with mouse input
    [SerializeField]
    private float touchLookSpeed = 0.05f; // The speed at which the camera is looking around with touch input
    [SerializeField]
    private float zoomSpeed = 5f; // The speed at which the camera is zomming in and out
    private Plane dragPlane; // The plane of the object being dragged
    private Vector3 offset; // The offset between the object's position and the mouse's position
    private Camera mainCamera; // The camera used to track the mouse
    private Vector3 lastPosition; // The last position of the Plane
    private Vector3 positionDelta; // The change in position of the object
    private Quaternion lastRotation; // The last rotation of the plane
    public UnityEvent<Vector3> OnDrag; // Event to be called when the object is dragged
    public UnityEvent<Vector4> OnRotation; // Event to be called when the object is rotated
    public UnityEvent<float> TouchMapScaling; // Event to be called when the map is scaled by touch input
    public InputActionAsset CameraZoom; 
    private InputAction lookAroundAction; // Variable to hold the Input action for looking around
    public WebsocketServerWithGUI serverManager; // the class that updates the map center used for camera to look at

    public static float updatedX; // The updated x position of the object
    public static float updatedZ; // The updated z position of the object

    //public float dragDeviationThreshold = 5f; // The threshold for drag when touch input is detected
    public float rotationAngleActivatedThreshold = 0.8f; // The threshold for rotation when touch input is detected
    public float pinchThreshold = 5f; // The threshold for pinch when touch input is detected

    private Coroutine dragCoroutine; // The coroutine used to drag the object
    private float camMapCenterDis;// the distance between the camera and the map center
    private float yawAngle; //the angle between the camera-to-map Center and x axis
    private float pitchAngle; // the angle between the camera-to-map Center and x-z plane
    private float initialYawAngle; // the initial yaw angle of the camera
    private float initialPitchAngle; // the initial pitch angle of the camera
    private float horizontalSensitivity => lookSpeed;
    private float verticalSensitivity => lookSpeed;
    private float touchHorizontalSensitivity => touchLookSpeed;
    private float touchVerticalSensitivity => touchLookSpeed;



    private enum GestureType
    {
         None,
         Drag,
         Pinch
    }

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        //yawAngle = Mathf.Rad2Deg * Mathf.Atan2(mainCamera.transform.position.x, mainCamera.transform.position.z); // the angle between the camera-to-map Center and x axis
        //pitchAngle = Mathf.Rad2Deg * (Mathf.Atan2(mainCamera.transform.position.y, Mathf.Sqrt(Mathf.Pow(mainCamera.transform.position.z, 2) + Mathf.Pow(mainCamera.transform.position.x, 2)))); // the angle between the camera-to-map Center and x-z plane
        initialYawAngle = 0f;// yawAngle;
        initialPitchAngle = 90;// pitchAngle;
        yawAngle = initialYawAngle;
        pitchAngle = initialPitchAngle;
        serverManager.resetYawPitchOnViewMode.AddListener(() =>
        {
            yawAngle = initialYawAngle;
            pitchAngle = initialPitchAngle;
            serverManager.mapPointUpdated?.Invoke(true); // when the camera is reset to the initial position, Reculate the cam-to-map center distance
        }
        );
        //Debug.Log($"Initial Yaw Angle: {yawAngle}, Initial Pitch Angle: {pitchAngle}");
        serverManager.mapPointUpdated.AddListener((isUpdated) =>
        {// Update the distance between the camera and the map center only when the map center is updated or switch to other map
         // No change in the distance when the map center is not updated
         // when resetYawPitchOnViewMode is triggered, camMapCenterDis should be updated when camera is reset to the initial position!
            if (isUpdated)
            {
                camMapCenterDis = Vector3.Distance(mainCamera.transform.position, serverManager.virtualMapPoint.transform.position);
                camMapCenterDis = Mathf.Clamp(camMapCenterDis, 0.1f, 5f); // Clamp the distance to prevent the camera from moving too far away or close to the map center
            }
        }
        );

        lastPosition = transform.position;
        lastRotation = transform.rotation;
        updatedX = transform.position.x;
        updatedZ = transform.position.z;

        //Find the lookAround action from the input system
        lookAroundAction = CameraZoom.FindActionMap("CameraZoom").FindAction("LookAround");

        
        Debug.Log($"Current device supports TouchInput: {Input.touchSupported}");
    }

    // Update is called once per frame
    void Update()
    {
        //Handle input based on whether touch is supported, only activate when touch input is detected
        if (Input.touchSupported && Input.touchCount > 0)
        {
            // Restrict LookAround triggered by rightmouse when touch input is detected, prevent long hold touch(Surface laptop) to trigger LookAround
            lookAroundAction.Disable();
            //Debug.Log("Touch Input Detected, Enter Touch block!");
            HandleTouchInput();
        }
        else
        {   // Handle Mouse-Key input
            //Enable LookAround action when touch input is not detected
            lookAroundAction.Enable();
 
            if (isMapControl)
            {   // control mode to drag the map
                HandleDragging();
            }
            else
            {  // view mode to move the camera with mouse-Key(awlays looking at the map center)
                HandleCameraViewRotation();
            }
        }
        mainCamera.transform.LookAt(serverManager.virtualMapPoint.transform, Vector3.up);
        mainCamera.transform.localPosition = new Vector3(mainCamera.transform.localPosition.x, Mathf.Abs(mainCamera.transform.localPosition.y), mainCamera.transform.localPosition.z);
    }


    private void HandleTouchInput()
    {
        // prevent UI interaction when touch input is detected
        if (EventSystem.current.IsPointerOverGameObject() && !frameDraggingOverride)
        {
            return;
        }

        int touchCount = Input.touchCount;

        if (touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            TouchDragFlow(touch, isMapControl);

            //if (touch.phase == UnityEngine.TouchPhase.Began || (touch.phase ==  UnityEngine.TouchPhase.Stationary))    // How to detect the active touch, transition from multiple fingers to single finger?
            //{
            //    StartTouchDrag(touch.position);
            //    Debug.Log("Single Finger Drag Gesture Detected");
            //}

            //Check touch.radius and radiusVariance to detect the touch area
            //Debug.Log($"Touch Radius: {touch.radius}, Touch Radius Variance: {touch.radiusVariance}");

        }
        else // Detect multiple finger gesture: drag or pinch?
        {
            switch(MultiFingerDetectGesture())
            {   // Map control based on the gesture type
                case GestureType.Drag:
                    Touch mainTouch = Input.GetTouch(0);
                    TouchDragFlow(mainTouch, isMapControl);
                    break;

                case GestureType.Pinch:
                    StopTouchDrag();
                    HandleTouchPinch(isMapControl);
                    break;
            }
        }
    }

    private void TouchDragFlow(Touch mainTouch, bool isMapControl)
    {
        if (isMapControl)
        {   // map control mode  to drag the map
            if (mainTouch.phase == UnityEngine.TouchPhase.Moved)
            {
                StopTouchDrag();
                StartTouchDrag(mainTouch.position - mainTouch.deltaPosition);
                //Debug.Log("Single Finger Drag Gesture Detected");
            }

            else if (mainTouch.phase == UnityEngine.TouchPhase.Ended || mainTouch.phase == UnityEngine.TouchPhase.Canceled)
            {
                //Stop dragging
                StopTouchDrag();
                //Debug.Log($"Last Touch ID:{ touch.fingerId} Ended or Canceled");
            }
        }
        else
        { // view mode to move the camera(awlays looking at the map center)
            HandleTouchViewMode(mainTouch);
        }
    }

    private void StartTouchDrag(Vector2 touchPosition)
    {
        //Create an infinite plane at the object's position
        dragPlane = new Plane(Vector3.up, transform.position);

        // Create a ray from the camera through the touch position
        Ray ray = mainCamera.ScreenPointToRay(touchPosition);

        //Calculate the offest between the infinite plane's position and the touch's position
        float distance;
        if (dragPlane.Raycast(ray, out distance))
        {
            Vector3 touchRayPoint = ray.GetPoint(distance);
            offset = transform.position - touchRayPoint; //Calculate the offset

            isDragging = true; //Set isDragging to true

            //Start the drag coroutine to deal with the drag movement
            if (dragCoroutine == null)
            {
                dragCoroutine = StartCoroutine(TouchDragUpdateCoroutine());
            }
        }
    }

    private void StopTouchDrag()
    {
        //Debug.Log("StopTouchDrag: Stop Touch Drag");
        isDragging = false; //Set isDragging to false
        if (dragCoroutine != null)
        {
            StopCoroutine(dragCoroutine); //Stop the drag coroutine
            dragCoroutine = null;
        }
    }

    private IEnumerator TouchDragUpdateCoroutine()
    {
        while (isDragging)
        {

            if (Input.touchCount == 0)
            {
                Debug.Log("Touch Count is 0, Stop Touch DragUpdateCoroutine");
                StopTouchDrag();
                yield break;
            }

            //Create a updated ray from the current touch position to the plane
            Ray ray = mainCamera.ScreenPointToRay(Input.GetTouch(0).position); 

            float distance;
            if (dragPlane.Raycast(ray, out distance))
            {
                Vector3 point = ray.GetPoint(distance); //New updated point
                transform.position = point + offset; //Update the position of the plane

                positionDelta = transform.position - lastPosition; //Calculate the change in position of the object
                if (positionDelta != Vector3.zero)
                {
                    updatedX = transform.position.x;
                    updatedZ = transform.position.z;
                    //Debug.Log("Drag Triggered by TouchInput, OnDrag event triggered");
                    OnDrag?.Invoke(new Vector3(updatedX, 0, updatedZ)); //Trigger the OnDrag event
                }
                lastPosition = transform.position; //Update the last position of the object
            }

            yield return null;
        }
    }

    // Handle the camera view rotation based on touch input
    void HandleTouchViewMode(Touch mainTouch)
    {
        if (mainTouch.phase == UnityEngine.TouchPhase.Moved)
        {
            yawAngle += mainTouch.deltaPosition.x * touchHorizontalSensitivity;
            pitchAngle += mainTouch.deltaPosition.y * touchVerticalSensitivity;
            //Debug.Log($"Touch Updated Yaw Angle: {yawAngle}, Updated Pitch Angle: {pitchAngle}");
            pitchAngle = Mathf.Clamp(pitchAngle, 90.1f, 180f);  //Clamp the pitch to prevent camera flipping
            mainCamera.transform.position = serverManager.virtualMapPoint.transform.position + new Vector3(camMapCenterDis * Mathf.Cos(pitchAngle * Mathf.Deg2Rad) * Mathf.Sin(yawAngle * Mathf.Deg2Rad),
                                                 camMapCenterDis * Mathf.Sin(pitchAngle * Mathf.Deg2Rad),
                                                 camMapCenterDis * Mathf.Cos(pitchAngle * Mathf.Deg2Rad) * Mathf.Cos(yawAngle * Mathf.Deg2Rad));
        }
    }

    //Handle touch pinch to control map scaling in map control mode, or camera zoom in view mode based on flag: isMapControl
    private void HandleTouchPinch(bool isMapControl)
    {
        if (Input.touchCount < 2 )
        {
            return;
        }

        float numPairs = 0;
        float deltaDistance = 0f;
        float overDistance = 0f;
        for (int i = 0; i< Input.touchCount - 1; i++)
        {
            for (int j = i+1; j < Input.touchCount; j++)
            {
                float currentDistance = Vector2.Distance(Input.GetTouch(i).position, Input.GetTouch(j).position);
                float prevDistance = Vector2.Distance(Input.GetTouch(i).position - Input.GetTouch(i).deltaPosition, Input.GetTouch(j).position - Input.GetTouch(j).deltaPosition);
                deltaDistance += (currentDistance - prevDistance);
                overDistance += (currentDistance / prevDistance); // Calculate the factor of the current distance to the previous distance
                numPairs++;
            }
        }
        float averageDeltaDistance = deltaDistance / numPairs;
        float averageOverDistance = overDistance / numPairs;

        if (isMapControl)
        {   // map control mode
            HandleTouchPinchControlMap(averageOverDistance);
        }
        else
        {   // view mode
            HandleTouchPinchControlView(averageDeltaDistance);
        }
    }

    // Handle touch pinch to control the map scale
    void HandleTouchPinchControlMap(float overDistance)
    {   // only trriger the event when the pinch amount is greater than the pinchThreshold
        Debug.Log($"[HandleTouchPinchControlMap] Pinch ScaleRatio: {overDistance}");
        TouchMapScaling?.Invoke(overDistance);
    }

    void HandleTouchPinchControlView(float deltaDistance)
    {
        //Debug.Log($"[HandleTouchPinchControlView] Pinch DeltaDistance: {deltaDistance}");
        float zoomAmount = deltaDistance * zoomSpeed;
        //Move the camera along the z-axis based on the zoom amount smoothly
        mainCamera.transform.position += mainCamera.transform.forward * zoomAmount;
        serverManager.mapPointUpdated?.Invoke(true); // when the camera zooms in or out, Reculate the cam-to-map center distance
    }

    bool frameDraggingOverride = false;
    public void OverrideDragging(BaseEventData dragEventData)
    {
        frameDraggingOverride = true;
    }

    public void EndOverrideDragging(BaseEventData dragEventData)
    {
        frameDraggingOverride = false;
    }

    private void HandleDragging()
    {
        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftControl) &&
            (!EventSystem.current.IsPointerOverGameObject() || frameDraggingOverride)) //Check if only the left mouse button is clicked
        {
            //Create an infinite plane at the object's position
            dragPlane = new Plane(Vector3.up, transform.position);

            // Create a ray from the camera through the mouse position
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            /*//Check if the ray hits the object
            //RaycastHit hit;
            //if (Physics.Raycast(ray, out hit) && hit.transform == transform)*/

            //Calculate the offest between the infinite plane's position and the mouse's position
            float distance;
            if (dragPlane.Raycast(ray, out distance))
            {
                Vector3 mouseRayPoint = ray.GetPoint(distance);
                offset = transform.position - mouseRayPoint; //Calculate the offset

                isDragging = true; //Set isDragging to true

                //Start the drag coroutine to deal with the drag movement
                if (dragCoroutine == null)
                {
                    dragCoroutine = StartCoroutine(DragUpdateCoroutine());
                }
            }
        }

        //Check if the mouse left button is released
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false; //Set isDragging to false
            if (dragCoroutine != null)
            {
                StopCoroutine(dragCoroutine); //Stop the drag coroutine
                dragCoroutine = null;
            }
        }
    }

    private IEnumerator DragUpdateCoroutine()
    {
        while (isDragging)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); //Create a updated ray from the mouse position to the plane
            float distance;

            //Check if the ray hits the drag plane, and update new position of the plane
            if (dragPlane.Raycast(ray, out distance))
            {
                Vector3 point = ray.GetPoint(distance);  // New updated point
                //Debug.Log($"Point: {point}");
                transform.position = point + offset;    //Update the position of the plane, mechanism: updated postion2 = point2 + offset = point2 + (position1 - point1) = position1 + (point2 - point1)
                //Debug.Log($"Position: {transform.position}");

                //Calculate the change in position of the object
                positionDelta = transform.position - lastPosition;

                // if the oject position has changed and is dragged, update the updatedX and updatedZ
                if (positionDelta != Vector3.zero)
                {
                    updatedX = transform.position.x;
                    updatedZ = transform.position.z;
                    //Debug.Log($"Updated X: {updatedX}, Updated Z: {updatedZ}");

                    //Trigger the OnDrag event, passing the updated position of the plane as a Vector3
                    //Debug.Log(" Plane Position changed, OnDrag event triggered");
                    OnDrag?.Invoke(new Vector3(updatedX, 0, updatedZ));
                }

                //Update the last position of the object
                lastPosition = transform.position;
            }

            // Wait for the next frame ( avoid too many updates in a single frame)
            yield return null;
        }
    }

    private void HandleCameraViewRotation()
    {
        if (Input.GetMouseButtonDown(0) && (!EventSystem.current.IsPointerOverGameObject() || frameDraggingOverride))
        {
            isRotating = true;
        }

        if (isRotating)
        {
            //camMapCenterDis = Vector3.Distance(mainCamera.transform.position, serverManager.virtualMapPoint.transform.position);
            //Debug.Log($"Current Camera Distance to Map Center: {camMapCenterDis}");
            yawAngle += Input.GetAxis("Mouse X") * horizontalSensitivity; //Degree unit!
            pitchAngle += Input.GetAxis("Mouse Y") * verticalSensitivity; // Degree unit!
            pitchAngle = Mathf.Clamp(pitchAngle, 90.1f, 180f);  //Clamp the pitch to prevent camera flipping
            //Debug.Log($"Updated Yaw Angle: {yawAngle}, Pitch Angle: {pitchAngle}");
             mainCamera.transform.position = serverManager.virtualMapPoint.transform.position + new Vector3(camMapCenterDis * Mathf.Cos(pitchAngle * Mathf.Deg2Rad) * Mathf.Sin(yawAngle * Mathf.Deg2Rad),
                                                 camMapCenterDis * Mathf.Sin(pitchAngle * Mathf.Deg2Rad),
                                                 camMapCenterDis * Mathf.Cos(pitchAngle * Mathf.Deg2Rad) * Mathf.Cos(yawAngle * Mathf.Deg2Rad));
            //float rotationInput = Input.GetAxis("Mouse X") * rotationSpeed; //Get the rotation input from the mouse 
            //Debug.Log($"Mouse X input: {Input.GetAxis("Mouse X")}");
            //Debug.Log($"Rotation Input: {rotationInput}");

            //Quaternion rotationDelta = Quaternion.Euler(0f, rotationInput, 0f); //Calculate the rotation delta along the Y-axis
            //Debug.Log($"Rotation Delta: {rotationDelta}");
            //transform.rotation *= rotationDelta; //Apply the rotation delta to the object
            //Debug.Log($"Current Rotation (Euler Angles): {transform.eulerAngles}");
            //Debug.Log($"Current Rotation (Quaternion): {transform.rotation}");



            //Check if the rotation has changed from the last frame
            //if (transform.rotation != lastRotation)
            //{
            //    OnRotation?.Invoke(new Vector4(0, transform.rotation.y, 0, transform.rotation.w)); //Trigger the OnRotation event
            //Debug.Log("Rotation changed, OnRotation event triggered");

            //    lastRotation = transform.rotation; //Update the last rotation 
            //}
        }

        // Check if the mouse left button is released
        if (Input.GetMouseButtonUp(0) && isRotating)
        {
            isRotating = false; //Set isRotating to false
        }
    }

    private GestureType MultiFingerDetectGesture()
    {
        int touchCount = Input.touchCount;
        if (touchCount < 2)
        {
            return GestureType.None;
        }

        //float totalDeltaAngle = 0f;
        float totalDeltaDistance = 0f;
        int numPairs = 0;

        /*Vector2 averageDeltaPosition = Vector2.zero;
        for (int i = 0; i < touchCount; i++)
        {
            averageDeltaPosition += Input.GetTouch(i).deltaPosition;
        }
        averageDeltaPosition /= touchCount;

        float totalDeviation = 0f;

        for (int i = 0; i < touchCount; i++)
        {
            Vector2 deviation = Input.GetTouch(i).deltaPosition - averageDeltaPosition;
            totalDeviation += deviation.magnitude;
        }
        float averageDeviation = totalDeviation / touchCount;*/

        //Compare each pair of touches and calculate the totalDeltaDistance  between them
        for (int i = 0; i < touchCount -1; i++)
        {
            for (int j = i + 1; j < touchCount; j++)
            {
                Touch touchI = Input.GetTouch(i);
                Touch touchJ = Input.GetTouch(j);

                Vector2 currentPosI = touchI.position;
                Vector2 currentPosJ = touchJ.position;

                Vector2 prevPosI = touchI.position - touchI.deltaPosition;
                Vector2 prevPosJ = touchJ.position - touchJ.deltaPosition;

                float currentDistance = Vector2.Distance(currentPosI, currentPosJ);
                float prevDistance = Vector2.Distance(prevPosI, prevPosJ);
                float deltaDistance = currentDistance - prevDistance;

                totalDeltaDistance += deltaDistance;

                //float currentAngle = Mathf.Atan2(currentPosJ.y - currentPosI.y, currentPosJ.x - currentPosI.x) * Mathf.Rad2Deg;
                //float prevAngle = Mathf.Atan2(prevPosJ.y - prevPosI.y, prevPosJ.x - prevPosI.x) * Mathf.Rad2Deg;
                //float deltaAngle = Mathf.DeltaAngle(prevAngle, currentAngle);

                //totalDeltaAngle += deltaAngle;

                numPairs++;
            }
        }

        float averageDeltaDistance = totalDeltaDistance / numPairs;
        //float averageDeltaAngle = totalDeltaAngle / numPairs;

        //Debug.Log($"[DetectGesture] Average Delta Distance: {averageDeltaDistance}");

        //Check for pinch gesture
        if (Mathf.Abs(averageDeltaDistance) > pinchThreshold)
        {
            Debug.Log($"[DetectGesture] Mutiple finger, pinch gesture. {averageDeltaDistance}");
            return GestureType.Pinch;
        }
        else
        {   //Default to drag gesture
            return GestureType.Drag;
        }

        //Prioritize drag gesture if fingers move together
        //if (averageDeviation < dragDeviationThreshold)
        //{
        //    Debug.Log("Mutiple finger, drag gesture");
        //   return GestureType.Drag;
        //}

        //Check for rotation gesture
        //else if (Mathf.Abs(averageDeltaAngle) > rotationThreshold)
        //{
        //    return GestureType.Rotation;
        //}
    }


    //Handle the rotation of the map based on touch input
    //private void HandleTouchRotation()
    //{

    //    if (Input.touchCount != 2)
    //    {
    //        isRotating = false;
    //        return;
    //    }
    //    if (Input.GetTouch(0).phase == UnityEngine.TouchPhase.Moved || Input.GetTouch(1).phase == UnityEngine.TouchPhase.Moved)
    //    {
    //        Vector2 touch0PrevPos = Input.GetTouch(0).position - Input.GetTouch(0).deltaPosition;
    //        Vector2 touch1PrevPos = Input.GetTouch(1).position - Input.GetTouch(1).deltaPosition;

    //        Vector2 prevTouchVector = touch0PrevPos - touch1PrevPos;
    //        Vector2 currentTouchVector = Input.GetTouch(0).position - Input.GetTouch(1).position;

    //        //Calculate the angle between two fingers
    //        float angleDelta = Vector2.SignedAngle(prevTouchVector, currentTouchVector);

    //        if (Mathf.Abs(angleDelta) < rotationAngleActivatedThreshold)
    //        {
    //            return;
    //        }

    //        //Debug.Log($"Triggered Rotation Angle Delta: {angleDelta}");

    //        //Apply the rotation delta to the object
    //        float rotationDelta = angleDelta * rotationSpeed * 0.1f; //0.1f: a scaling factor to adjust the rotation speed for touch input
    //        transform.rotation *= Quaternion.Euler(0f, -rotationDelta, 0f);

    //        //Trigger the OnRotation event
    //        if (transform.rotation != lastRotation)
    //        {
    //            Debug.Log("Rotation changed by TouchInput, OnRotation event triggered");
    //            OnRotation?.Invoke(new Vector4(0, transform.rotation.y, 0, transform.rotation.w));
    //            lastRotation = transform.rotation;
    //        }
    //    }

    //}

    //private float CalculatePinchAmount()
    //{
    //    if (Input.touchCount <2)
    //    {
    //        Debug.LogWarning("CalculatePinchAmount called with less than 2 touches");
    //        return 0f;
    //    }

    //    float currentDistance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
    //    float prevDistance = Vector2.Distance(Input.GetTouch(0).position - Input.GetTouch(0).deltaPosition, Input.GetTouch(1).position - Input.GetTouch(1).deltaPosition);

    //    return currentDistance - prevDistance;
    //}

    //private Vector2 GetAverageTouchPosition()
    //{
    //    Vector2 averageTouchPosition = Vector2.zero;
    //    for (int i = 0; i < Input.touchCount; i++)
    //    {
    //        averageTouchPosition += Input.GetTouch(i).position;
    //    }
    //    averageTouchPosition /= Input.touchCount;
    //    return averageTouchPosition;
    //}
}




//Handle the look around  to rotate the camera  along x and y-axis with whole palm touch
/*private void HandleTouchLookAround()
{
    if (Input.touchCount < 3)
    {
        return;
    }

    // five fingers move together to rotate the camera
    if (Input.GetTouch(0).phase == UnityEngine.TouchPhase.Moved)
    {
        Vector2 touchDelta = Input.GetTouch(0).deltaPosition;

        yaw += touchDelta.x * lookSpeed;
        pitch -= touchDelta.y * lookSpeed;
        pitch = Mathf.Clamp(pitch, -89f, 89f);  //Clamp the pitch to prevent camera flipping

        //Rotate the camera along the x and y-axis
        mainCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}*/

/*if (touchCount >= 2 && touchCount <= 4)
       {
           //Calculate the geometric center of the touches
           Vector2 center = Vector2.zero;
           Vector2 prevCenter = Vector2.zero;
           for (int i = 0; i < touchCount; i++)
           {
               center += Input.GetTouch(i).position;
               prevCenter += Input.GetTouch(i).position - Input.GetTouch(i).deltaPosition;
           }
           center /= touchCount;
           prevCenter /= touchCount;

           //Calculate the angle deltas of the touches
           float totalAngleDelta = 0f;
           int validTouchCount = 0;
           for (int i =0; i < touchCount; i++)
           {
               Vector2 prevPos = Input.GetTouch(i).position - Input.GetTouch(i).deltaPosition;
               Vector2 currentPos = Input.GetTouch(i).position;

               Vector2 prevVector = prevPos - prevCenter;
               Vector2 currentVector = currentPos - center;

               //Calculate each touch's angle delta
               float anglePrev = Mathf.Atan2(prevVector.y, prevVector.x) * Mathf.Rad2Deg;
               float angleCurrent = Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg;

               float angleDelta =Mathf.DeltaAngle(anglePrev, angleCurrent);
               totalAngleDelta += angleDelta;
               validTouchCount++;
           }

           if (validTouchCount > 0)
           {
               float averageAngleDelta = totalAngleDelta / validTouchCount;

               //Set a minimum threshold to activate rotation. Within the threshold, the rotation is not triggered, small angle changes are ignored
               float rotationAngleThreshold = 0.2f; // ! Wait for testing!
               Debug.Log($"Average Angle Delta: {averageAngleDelta}");

               if (Mathf.Abs(averageAngleDelta) > rotationAngleThreshold)
               {
                   //Apply the rotation delta to the object
                   transform.Rotate(0f, -averageAngleDelta *rotationSpeed, 0f);

                   //Trigger the OnRotation event
                   if (transform.rotation != lastRotation)  // Only call the event when the rotation has changed out of the minimum threshold
                   {
                       Debug.Log("Rotation changed by TouchInput, OnRotation event triggered");
                       OnRotation?.Invoke(new Vector4(0, transform.rotation.y, 0, transform.rotation.w));
                       lastRotation = transform.rotation;
                   }
               }
           }
       }
       else
       {
           isRotating = false;
       }*/

// Used for future multiple finger gesture recognition
// Check if the touch input is a rotation gesture, if not, check if it is a pinch gesture and set the pinchFlag as true
/*private bool IsRotationGesture(out bool pinchFlag)
{
    int touchCount = Input.touchCount;

    //Calculate average movement of the touches
    Vector2 averageDelta = Vector2.zero;
    for (int i = 0; i< touchCount; i++)
    {
        averageDelta += Input.GetTouch(i).deltaPosition;
    }
    averageDelta /= touchCount; //Calculate the average delta

    //Calculate the amount of relative movement between the touches
    float totalRelativeMovement = 0f;
    for (int i = 0; i < touchCount; i++)
    {
        Vector2 touchDelta = Input.GetTouch(i).deltaPosition - averageDelta; //Calculate the relative movement of each touch
        totalRelativeMovement += touchDelta.magnitude;
    }

    //Set a threshold for distinguishing between rotation and pinch gestures before using totalRelativeMovement
    pinchThreshold = 10f;  // ! Wait for testing!

    //Calculate the pinch amount
    float pinchAmount = CalculatePinchAmount();

    //If pinchAmount is greater than the pinchThreshold, set the pinchFlag to true and return false
    if (Mathf.Abs(pinchAmount) > pinchThreshold)
    {
        Debug.Log($"Pinch Gesture Detected! Current pinchAmount: {Mathf.Abs(pinchAmount)}");
        pinchFlag = true;
        return false;
    }

    pinchFlag = false; // Not a pinch gesture

    //Set a threshold for distinguishing between rotation and drag gestures using totalRelativeMovement
    rotationThreshold = 5f; // ! Wait for testing!
    float averageRelativeMovement = totalRelativeMovement / touchCount;
    if (averageRelativeMovement > rotationThreshold)
    {
        Debug.Log($"Rotation Gesture Detected!  Current Average Relative Movement: {averageRelativeMovement}");
        return true;
    }
    else
    {
        Debug.Log($"Drag Gesture Detected!");
        return false;
    }
}*/



