using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TMRI.Client
{

    [RequireComponent(typeof(Canvas))]
    public class SplitScreenCanvas : MonoBehaviour
    {
        //public Camera FirstScreenSpaceCamera;
        //public Camera SecondScreenSpaceCamera;

        public static SplitScreenCanvas instance;

        public Canvas ScreenSpaceCameraCanvas { get; private set; }

        void Start()
        {

            if (instance == null)
            {
                DontDestroyOnLoad(gameObject);
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            ScreenSpaceCameraCanvas = GetComponent<Canvas>();
            //ScreenSpaceCameraCanvas.renderMode = RenderMode.ScreenSpaceCamera;

            //gameObject.SetActive(false);
        }
    }
}