using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;

namespace MobfishCardboard
{
    public class CardboardMainCamera : MonoBehaviour
    {
        //Only used in dontDestroyAndSingleton
        private static CardboardMainCamera instance;

        [Header("Cameras")]
        [SerializeField]
        private Camera novrCam;
        [SerializeField]
        private Camera leftCam;
        [SerializeField]
        private Camera rightCam;
        [SerializeField]
        private GameObject vrCamGroup;
        [SerializeField]
        private GameObject novrCamGroup;

        [Header("Options")]
        [SerializeField]
        private bool defaultEnableVRView;
        [Tooltip(
            "Set this GameObject DontDestroyOnLoad and Singleton. If it's not needed or any parent GameObject already have DontDestroyOnLoad, disable it")]
        [SerializeField]
        private bool dontDestroyAndSingleton = true;

        private Camera CameraLeft => leftCam;
        private Camera CameraRight => rightCam;
        private Camera myCamera => novrCam;
        private RenderTextureDescriptor eyeRenderTextureDesc;
        private bool overlayIsOpen;
        private Vector3 cachedLeft;
        private Vector3 cachedRight;
        private bool initialized;
        private CardboardHeadTransform cardboardHeadTransformer;
        private TrackedPoseDriver tpd;

        private void Awake()
        {
            Application.targetFrameRate = CardboardUtility.GetTargetFramerate();
            tpd = GetComponent<TrackedPoseDriver>();

            if (dontDestroyAndSingleton)
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
            }

            SetupRenderTexture();

            CardboardManager.InitCardboard();
            CardboardManager.SetVRViewEnable(defaultEnableVRView);
        }

        // Start is called before the first frame update
        void Start()
        {
            cachedLeft = CameraLeft.transform.localPosition;
            cachedRight = CameraRight.transform.localPosition;
            cardboardHeadTransformer = GetComponent<CardboardHeadTransform>();

            RefreshCamera();
            CardboardManager.deviceParamsChangeEvent += RefreshCamera;
            SwitchVRCamera();
            CardboardManager.enableVRViewChangedEvent += SwitchVRCamera;
        }

        private void OnDestroy()
        {
            CardboardManager.deviceParamsChangeEvent -= RefreshCamera;
            CardboardManager.enableVRViewChangedEvent -= SwitchVRCamera;
        }

        private void SetupRenderTexture()
        {
            SetupEyeRenderTextureDescription();

            RenderTexture newLeft = new RenderTexture(eyeRenderTextureDesc);
            RenderTexture newRight = new RenderTexture(eyeRenderTextureDesc);
            leftCam.targetTexture = newLeft;
            rightCam.targetTexture = newRight;

            CardboardManager.SetRenderTexture(newLeft, newRight);
        }

        private void SetupEyeRenderTextureDescription()
        {
            Vector2Int resolution = CardboardUtility.GetAdjustedScreenResolution();
            eyeRenderTextureDesc = new RenderTextureDescriptor()
            {
                dimension = TextureDimension.Tex2D,
                width = resolution.x / 2,
                height = resolution.y,
                depthBufferBits = 16,
                volumeDepth = 1,
                msaaSamples = 1,
                vrUsage = VRTextureUsage.OneEye
            };

            #if UNITY_2019_1_OR_NEWER
            eyeRenderTextureDesc.graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            Debug.LogFormat("CardboardMainCamera.SetupEyeRenderTextureDescription(), graphicsFormat={0}",
                eyeRenderTextureDesc.graphicsFormat);

            #endif
        }

        private void SwitchVRCamera()
        {
            vrCamGroup.SetActive(CardboardManager.enableVRView);
            //novrCamGroup.SetActive(!CardboardManager.enableVRView);
        }

        private void RefreshCamera()
        {
            if (!CardboardManager.profileAvailable)
            {
                return;
            }

            RefreshCamera_Eye(leftCam,
                CardboardManager.projectionMatrixLeft, CardboardManager.eyeFromHeadMatrixLeft);
            RefreshCamera_Eye(rightCam,
                CardboardManager.projectionMatrixRight, CardboardManager.eyeFromHeadMatrixRight);


            // if (CardboardManager.deviceParameter != null)
            // {
            //     leftCam.transform.localPosition =
            //         new Vector3(-CardboardManager.deviceParameter.InterLensDistance / 2, 0, 0);
            //     rightCam.transform.localPosition =
            //         new Vector3(CardboardManager.deviceParameter.InterLensDistance / 2, 0, 0);
            // }
        }

        private static void RefreshCamera_Eye(Camera eyeCam, Matrix4x4 projectionMat, Matrix4x4 eyeFromHeadMat)
        {
            if (!projectionMat.Equals(Matrix4x4.zero))
                eyeCam.projectionMatrix = projectionMat;

            //https://github.com/googlevr/cardboard/blob/master/sdk/lens_distortion.cc
            if (!eyeFromHeadMat.Equals(Matrix4x4.zero))
            {
                Pose eyeFromHeadPoseGL = CardboardUtility.GetPoseFromTRSMatrix(eyeFromHeadMat);
                eyeFromHeadPoseGL.position.x = -eyeFromHeadPoseGL.position.x;
                eyeCam.transform.localPosition = eyeFromHeadPoseGL.position;
                eyeCam.transform.localRotation = eyeFromHeadPoseGL.rotation;
            }
        }

        private void OnPostRender()
        {
            //CameraLeft.fieldOfView = CameraRight.fieldOfView = myCamera.fieldOfView;

            if (initialized)
                return;

            var cachedTargetTextureLeft = CameraLeft.targetTexture;
            var cachedTargetTextureRight = CameraRight.targetTexture;

            CameraLeft.CopyFrom(myCamera);
            CameraRight.CopyFrom(myCamera);

            CameraLeft.targetTexture = cachedTargetTextureLeft;
            CameraRight.targetTexture = cachedTargetTextureRight;

            CameraLeft.usePhysicalProperties = true;
            CameraRight.usePhysicalProperties = true;
            CameraLeft.usePhysicalProperties = false;
            CameraRight.usePhysicalProperties = false;

            //myCamera.cullingMask = 0;

            //ScaleScreenDisparity(PlayerPrefs.GetFloat(SCREEN_DISPARITY_KEY, 0f));

            initialized = true;
        }

    }

    
}