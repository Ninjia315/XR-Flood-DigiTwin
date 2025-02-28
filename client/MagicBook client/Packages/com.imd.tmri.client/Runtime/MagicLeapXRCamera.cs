using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace TMRI.Client
{
    public class MagicLeapXRCamera : BaseXRCamera
    {
        public TrackedPoseDriver tpd;
        public Camera myCamera;

        public override void EnableAR(float fieldOfView)
        {
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            myCamera.fieldOfView = fieldOfView;
            myCamera.clearFlags = CameraClearFlags.SolidColor;

            tpd.enabled = true;
        }

        public override void EnableVR(float fieldOfView)
        {
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            myCamera.fieldOfView = fieldOfView;
            myCamera.clearFlags = CameraClearFlags.Skybox;

#if UNITY_EDITOR
            tpd.enabled = false;
#else
        tpd.enabled = true;
#endif
        }

        public override void OnXRTransitionStart()
        {
            base.OnXRTransitionStart();
            tpd.enabled = false;
        }

        public override void SetFoV(float fieldOfView)
        {
            myCamera.fieldOfView = fieldOfView;
        }

        public override void ToggleStereoMono(float fieldOfView, CameraClearFlags monoCameraClearFlags)
        {

        }
    }
}//namespace TMRI.Client