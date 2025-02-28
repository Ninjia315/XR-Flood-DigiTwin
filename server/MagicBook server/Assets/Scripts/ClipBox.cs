using System;
using UnityEngine;

[ExecuteAlways]
public class ClipBox : MonoBehaviour
{ 
    void Update()
    {
        Shader.SetGlobalMatrix("_WorldToBox", transform.worldToLocalMatrix);
        //CheckAndCorrectYPosition();
    }

    //private void CheckAndCorrectYPosition()
    //{
    //    float supposedY = transform.localScale.y * 0.5f;


    //    if (supposedY != transform.position.y) 
    //    {
    //        transform.position = new Vector3(transform.position.x,supposedY, transform.position.z);
    //    }
    //}
}
