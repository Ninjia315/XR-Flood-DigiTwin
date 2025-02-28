using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandmarkConfig : MonoBehaviour
{
    public GameObject MapPointerPrefab;
    private WebsocketServerWithGUI server; 
    // Start is called before the first frame update
    void Start()
    {
        if (server == null)
        {
            server = GameObject.Find("Server").GetComponent<WebsocketServerWithGUI>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

  
}
