using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
using MobfishCardboard;

namespace TMRI.Client
{
    public class CardboardMainCamera : PanoramicXRCamera//BaseXRCamera
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
        [SerializeField]
        private bool singleCam;

        private RenderTextureDescriptor eyeRenderTextureDesc;
        private bool overlayIsOpen;
        //private Vector3 cachedLeft;
        //private Vector3 cachedRight;
        private bool initialized;
        private CardboardHeadTransform cardboardHeadTransformer;
        private TrackedPoseDriver tpd;
        private Camera leftCamDepth;
        private Camera rightCamDepth;


        public override void EnableAR(float fieldOfView)
        {
            base.EnableAR(fieldOfView);

            tpd.enabled = true;

            leftCam.clearFlags = rightCam.clearFlags = CameraClearFlags.SolidColor;
            leftCam.fieldOfView = rightCam.fieldOfView = fieldOfView;//75;
            leftCam.transform.localPosition = rightCam.transform.localPosition = Vector3.zero;

            if (singleCam)
            {
                leftCam.gameObject.SetActive(false);
                rightCam.gameObject.SetActive(false);

                CardboardUtility.ForceSquareResolution = true;
                SetupRenderTexture();

                CardboardManager.RefreshParameters();
                CardboardManager.SetRenderTexture(novrCam.targetTexture, novrCam.targetTexture);
            }

            if (leftCamDepth != null && rightCamDepth != null)
            {
                leftCamDepth.gameObject.SetActive(false);
                rightCamDepth.gameObject.SetActive(false);
            }

            if (cardboardHeadTransformer != null)
                cardboardHeadTransformer.enabled = false;
        }

        public override void EnableVR(float fieldOfView)
        {
            base.EnableVR(fieldOfView);

            tpd.enabled = false;

            leftCam.clearFlags = rightCam.clearFlags = CameraClearFlags.Skybox;
            leftCam.fieldOfView = rightCam.fieldOfView = fieldOfView;

            if (leftCam.targetTexture == null || rightCam.targetTexture == null)
            {
                RenderTexture newLeft = new RenderTexture(eyeRenderTextureDesc);
                RenderTexture newRight = new RenderTexture(eyeRenderTextureDesc);
                leftCam.targetTexture = newLeft;
                rightCam.targetTexture = newRight;

                var l = new GameObject("Left depth cam");
                l.transform.parent = leftCam.transform;
                l.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                leftCamDepth = l.AddComponent<Camera>();

                leftCam.cullingMask = 0; //Nothing

                var r = new GameObject("Right depth cam");
                r.transform.parent = rightCam.transform;
                r.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                rightCamDepth = r.AddComponent<Camera>();

                rightCam.cullingMask = 0; //Nothing
            }

            if (singleCam)
            {
                leftCam.gameObject.SetActive(true);
                rightCam.gameObject.SetActive(true);
            
                CardboardUtility.ForceSquareResolution = false;
                SetupRenderTexture();

                CardboardManager.RefreshParameters();
                CardboardManager.SetRenderTexture(leftCam.targetTexture, rightCam.targetTexture);
            }

            RefreshCamera(stereo: true);

            if (leftCamDepth != null && rightCamDepth != null)
            {
                leftCamDepth.gameObject.SetActive(true);
                rightCamDepth.gameObject.SetActive(true);
            }

            if (cardboardHeadTransformer != null)
                cardboardHeadTransformer.enabled = true;
        }

        public override void ToggleStereoMono(float fieldOfView, CameraClearFlags monoCameraClearFlags)
        {
            base.ToggleStereoMono(fieldOfView, monoCameraClearFlags);

            leftCam.gameObject.SetActive(!leftCam.gameObject.activeSelf);
            rightCam.gameObject.SetActive(!rightCam.gameObject.activeSelf);
            var post = vrCamGroup.GetComponentInChildren<CardboardPostCamera>(true);
            post.gameObject.SetActive(!post.gameObject.activeSelf);

            novrCam.clearFlags = monoCameraClearFlags;
            novrCam.usePhysicalProperties = true;
            novrCam.usePhysicalProperties = false;
            novrCam.fieldOfView = fieldOfView;
        }

        public override void SetFoV(float fieldOfView)
        {
            base.SetFoV(fieldOfView);

            leftCam.fieldOfView = rightCam.fieldOfView = fieldOfView;
        }

        public override void SetStereoDisparity(float value)
        {
            base.SetStereoDisparity(value);

            leftCam.transform.localPosition = Vector3.left * value;
            rightCam.transform.localPosition = Vector3.right * value;
        }

        public override void Awake()
        {
            base.Awake();

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
        override public void Start()
        {
            base.Start();

            //cachedLeft = CameraLeft.transform.localPosition;
            //cachedRight = CameraRight.transform.localPosition;
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

            if (singleCam)
            {
                RenderTexture rt = new RenderTexture(eyeRenderTextureDesc);
                novrCam.targetTexture = rt;
                CardboardManager.SetRenderTexture(rt, rt);
            }
            else
            {
                RenderTexture newLeft = new RenderTexture(eyeRenderTextureDesc);
                RenderTexture newRight = new RenderTexture(eyeRenderTextureDesc);
                leftCam.targetTexture = newLeft;
                rightCam.targetTexture = newRight;
                CardboardManager.SetRenderTexture(newLeft, newRight);
            }
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

        private void RefreshCamera(bool stereo)
        {
            RefreshCamera();

            if (stereo && CardboardManager.profileAvailable && CardboardManager.deviceParameter != null)
            {
                leftCam.transform.localPosition =
                    new Vector3(-CardboardManager.deviceParameter.InterLensDistance / 2, 0, 0);
                rightCam.transform.localPosition =
                    new Vector3(CardboardManager.deviceParameter.InterLensDistance / 2, 0, 0);
            }
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

            var cachedTargetTextureLeft = leftCam.targetTexture;
            var cachedTargetTextureRight = rightCam.targetTexture;

            leftCam.CopyFrom(novrCam);
            rightCam.CopyFrom(novrCam);

            leftCam.targetTexture = cachedTargetTextureLeft;
            rightCam.targetTexture = cachedTargetTextureRight;

            leftCam.usePhysicalProperties = true;
            rightCam.usePhysicalProperties = true;
            leftCam.usePhysicalProperties = false;
            rightCam.usePhysicalProperties = false;

            //myCamera.cullingMask = 0;

            //ScaleScreenDisparity(PlayerPrefs.GetFloat(SCREEN_DISPARITY_KEY, 0f));

            initialized = true;
        }

        public override void OnXRTransitionStart()
        {
            base.OnXRTransitionStart();

            tpd.enabled = false;
        }

        private void Update()
        {
            if (depthOnlyCamera != null)
                depthOnlyCamera.gameObject.SetActive(false);

            UpdateDepthCam(leftCam, leftCamDepth);
            UpdateDepthCam(rightCam, rightCamDepth);
        }

        private void UpdateDepthCam(Camera cam, Camera depthCam)
        {
            if (depthCam == null || cam == null || !depthCam.isActiveAndEnabled)
                return;

            depthCam.CopyFrom(cam);
            depthCam.depth = cam.depth + 1;
            depthCam.clearFlags = CameraClearFlags.Depth;
            depthCam.cullingMask = -1; //Everything
            depthCam.targetTexture = cam.targetTexture;
        }
    }

}
#endif