#if UNITY_VISIONOS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMRI.Core;
using Unity.PolySpatial;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace TMRI.Client
{
    public class PolySpatialXRCamera : PanoramicXRCamera
    {
        public VolumeCamera PolySpatialVolumeCamera;
        public VolumeCameraWindowConfiguration VRModeConfiguration;
        public Transform XROriginTransform;
        public List<XRModeMaterialMapping> MaterialMappings;

        [Serializable]
        public class XRModeMaterialMapping
        {
            public Renderer TargetRenderer;
            public Material ARModeMaterial;
            public Material VRModeMaterial;
        }

        VolumeCameraWindowConfiguration cachedPolySpatialConfiguration;
        TrackedPoseDriver tpd;
        XRMode? xrMode;
        Vector3 lockPosition;
        IEnumerable<ImageTargetContent> trackedImageListeners;

        public override void Awake()
        {
            base.Awake();

            tpd = GetComponent<TrackedPoseDriver>();
        }

        public override void EnableAR(float fieldOfView)
        {
            base.EnableAR(fieldOfView);

            xrMode = XRMode.AR;

            if (trackedImageListeners?.Any() ?? false)
                foreach (var i in trackedImageListeners)
                    i.SubscribeTrackedImages(true);

            //tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

#if UNITY_EDITOR
            tpd.enabled = false;
#else
            //tpd.enabled = true;
#endif
            myCamera.backgroundColor = Color.clear;
            if (cachedPolySpatialConfiguration != null)
                PolySpatialVolumeCamera.WindowConfiguration = cachedPolySpatialConfiguration;

            XROriginTransform.position = transform.position;

#if UNITY_EDITOR
            //PolySpatialVolumeCamera.CullingMask = -1;//-1 Everything, 0 Nothing
#endif
            //PolySpatialVolumeCamera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (var map in MaterialMappings)
            {
                map.TargetRenderer.material = map.ARModeMaterial;
            }
        }

        public override void EnableVR(float fieldOfView)
        {
            base.EnableVR(fieldOfView);

            xrMode = XRMode.VR;
            lockPosition = transform.position;

            //tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            //transform.position = XROriginTransform.position;
            //XROriginTransform.position = -transform.localPosition;

#if UNITY_EDITOR
            tpd.enabled = false;
#else
            //tpd.enabled = true;
#endif
            myCamera.backgroundColor = Color.black;
            //myCamera.cullingMask = -1;

            //cachedPolySpatialConfiguration = PolySpatialVolumeCamera.WindowConfiguration;
            //PolySpatialVolumeCamera.WindowConfiguration = VRModeConfiguration;

#if UNITY_EDITOR
            //PolySpatialVolumeCamera.CullingMask = 0;//-1 Everything, 0 Nothing
#endif
            foreach (var map in MaterialMappings)
            {
                map.TargetRenderer.material = map.VRModeMaterial;
            }
        }

        public override void OnXRTransitionStart()
        {
            xrMode = null;
            if(XROriginTransform.TryGetComponent(out ARTrackedImageManager imageManager))
            {
                trackedImageListeners = FindObjectsByType<ImageTargetContent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(it => it.TrackedImageManager == imageManager);
                foreach (var i in trackedImageListeners)
                    i.SubscribeTrackedImages(false);
            }
            //tpd.enabled = false;

            //tpd.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            cachedPolySpatialConfiguration = PolySpatialVolumeCamera.WindowConfiguration;
            PolySpatialVolumeCamera.WindowConfiguration = VRModeConfiguration;

            //yield return new WaitForSeconds(2f);
        }

        public override Transform GetAnimateableTransform()
        {
            //return base.GetAnimateableTransform();
            return XROriginTransform;
        }

        public override void Update()
        {
            base.Update();

            if (xrMode == XRMode.VR)
            {
                //XROriginTransform.position = -transform.localPosition;
                var offset = transform.position - XROriginTransform.position;
                XROriginTransform.position = lockPosition - offset;
            }
        }
    }
}
#endif