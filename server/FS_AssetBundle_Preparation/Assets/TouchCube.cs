using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchCube : MonoBehaviour

{
    private Vector3 initialPosition;
    private float width;
    private float height;

    private void Awake()
    {
        width =(float)Screen.width / 2.0f;
        height = (float)Screen.height / 2.0f;
    }


     void Start()
    {
        if (Input.touchSupported)
        {
            Debug.Log("Touch input is supported in this device.");
        }
        else
        {
            Debug.LogWarning("Current device cannot support touch input!");
        }

        //position used for the cube
        initialPosition = transform.position;
    }
    // Update is called once per frame
    void Update()
    {
        var fingerCount = 0;
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
            {
                fingerCount++;
            }
        }

        if (fingerCount >0)
        {
            print($"User has {fingerCount} finger(s) touching the screen");
            if (fingerCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved) 
                {
                    Vector3 touchDelta = new Vector3(touch.deltaPosition.x, 0, touch.deltaPosition.y) * Time.deltaTime;
                    transform.position += touchDelta;
                }
            }
            if (fingerCount == 2)
            {
                Touch touchZero = Input.GetTouch(0);
                Touch touchOne = Input.GetTouch(1);

                if ( touchZero.phase == TouchPhase.Moved && touchOne.phase == TouchPhase.Moved)
                {
                    Vector2 prevTouchZeroPos = touchZero.position - touchZero.deltaPosition;
                    Vector2 prevTouchOnePos = touchOne.position - touchOne.deltaPosition;
                    
                }
            }
        }
        else
        {
            Debug.LogWarning("No Activate Touch input detected!");
            // Non-touch Input code block
        }
        
    }
}
