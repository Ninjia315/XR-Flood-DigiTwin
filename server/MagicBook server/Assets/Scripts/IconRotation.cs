using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconRotation : MonoBehaviour
{
    public float rotationDelta = 50f; //rotation speed
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // if child parent is active
        if (transform.parent != null && transform.parent.gameObject.activeSelf)
        {
            //rotate the icon along Z axis
            transform.Rotate(0, 0, - rotationDelta * Time.deltaTime);
            //Debug.Log("Rotating Icon");
        }
    }
}
