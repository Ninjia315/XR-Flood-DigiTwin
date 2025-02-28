using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace TMRI.Client
{
    [RequireComponent(typeof(Camera))]
    public class StereoCamera : PanoramicXRCamera//BaseXRCamera
    {
        public Camera CameraLeft;
        public Camera CameraRight;

        
        //private CardboardHeadTransform cardboardHeadTransformer;
        private Vector3 cachedLeft;
        private Vector3 cachedRight;
        protected TrackedPoseDriver tpd;

        public override void EnableAR(float fieldOfView)
        {
            base.EnableAR(fieldOfView);

            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

#if UNITY_EDITOR
            tpd.enabled = false;
#else
            tpd.enabled = true;
#endif

            CameraLeft.clearFlags = CameraRight.clearFlags = CameraClearFlags.SolidColor;
            if (CameraLeft.orthographic)
                CameraLeft.orthographicSize = CameraRight.orthographicSize = fieldOfView;
            else
                CameraLeft.fieldOfView = CameraRight.fieldOfView = fieldOfView;//75;

            CameraLeft.transform.localPosition = Vector3.zero;
            CameraRight.transform.localPosition = Vector3.zero;

            //if (cardboardHeadTransformer != null)
            //    cardboardHeadTransformer.enabled = false;

            //CardboardManager.SetVRViewEnable(false);
        }
        public override void EnableVR(float fieldOfView)
        {
            base.EnableVR(fieldOfView);

            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
#if UNITY_EDITOR
            tpd.enabled = false;
#else
            tpd.enabled = true;
#endif

            CameraLeft.clearFlags = CameraRight.clearFlags = CameraClearFlags.Skybox;
            CameraLeft.fieldOfView = CameraRight.fieldOfView = fieldOfView;
            CameraLeft.transform.localPosition = cachedLeft;
            CameraRight.transform.localPosition = cachedRight;

            //if (cardboardHeadTransformer != null)
            //    cardboardHeadTransformer.enabled = true;

            //CardboardManager.SetVRViewEnable(true);
        }
        public override void ToggleStereoMono(float fieldOfView, CameraClearFlags monoCameraClearFlags)
        {
            base.ToggleStereoMono(fieldOfView, monoCameraClearFlags);

            CameraLeft.gameObject.SetActive(!CameraLeft.gameObject.activeSelf);
            CameraRight.gameObject.SetActive(!CameraRight.gameObject.activeSelf);
            //var post = vrCamGroup.GetComponentInChildren<CardboardPostCamera>(true);
            //post.gameObject.SetActive(!post.gameObject.activeSelf);

            myCamera.clearFlags = monoCameraClearFlags;
            myCamera.usePhysicalProperties = true;
            myCamera.usePhysicalProperties = false;
            myCamera.fieldOfView = fieldOfView;
            myCamera.cullingMask = monoCameraClearFlags == CameraClearFlags.SolidColor ? 0 : -1; //-1 Everything, 0 Nothing
        }

        public override void SetFoV(float fieldOfView)
        {
            base.SetFoV(fieldOfView);

            if (CameraLeft.orthographic)
                CameraLeft.orthographicSize = CameraRight.orthographicSize = fieldOfView;
            else
                CameraLeft.fieldOfView = CameraRight.fieldOfView = fieldOfView;
        }
        public override void SetStereoDisparity(float value)
        {
            base.SetStereoDisparity(value);

            //CameraLeft.rect = new Rect(-value, 0, 1f, 1f);
            //CameraRight.rect = new Rect(value, 0, 1f, 1f);
            if (value > 0)
            {
                CameraLeft.rect = new Rect(value, 0, .5f - value, 1f);
                CameraRight.rect = new Rect(.5f, 0, .5f - value, 1f);
            }
            else
            {
                CameraLeft.rect = new Rect(0, 0, .5f + value, 1f);
                CameraRight.rect = new Rect(.5f - value, 0, .5f, 1f);
            }
        }

        public override void Awake()
        {
            base.Awake();

            myCamera = GetComponent<Camera>();
            //cardboardHeadTransformer = GetComponent<CardboardHeadTransform>();
            tpd = GetComponent<TrackedPoseDriver>();

            cachedLeft = CameraLeft.transform.localPosition;
            cachedRight = CameraRight.transform.localPosition;
        }

        public override void OnXRTransitionStart()
        {
            base.OnXRTransitionStart();

            tpd.enabled = false;
        }
    }
}//namespace TMRI.Client