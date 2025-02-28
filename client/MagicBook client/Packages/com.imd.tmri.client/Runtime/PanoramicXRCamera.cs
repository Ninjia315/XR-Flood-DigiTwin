using System.Collections;
using System.Collections.Generic;
using TMRI.Client;
using UnityEngine;

namespace TMRI.Client
{
    [RequireComponent(typeof(Camera))]
    public class PanoramicXRCamera : BaseXRCamera
    {
        public Material panoramicMaterial;
        public bool useCameraDepthTexture;
        public LayerMask UICameraCullingMask;
        protected Camera myCamera;

        protected Camera depthOnlyCamera;
        protected Camera uiCamera;

        protected Material cachedSkyboxMat;

        public virtual void Awake()
        {
            myCamera = GetComponent<Camera>();
        }

        public override void Start()
        {
            base.Start();

            cachedSkyboxMat = RenderSettings.skybox;

            var cameraGO = new GameObject("Depth cam");
            cameraGO.transform.parent = transform;
            cameraGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            cameraGO.SetActive(false);

            depthOnlyCamera = cameraGO.AddComponent<Camera>();

            var uiCameraGO = new GameObject("UI cam");
            uiCameraGO.transform.parent = transform;
            uiCameraGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            uiCamera = uiCameraGO.AddComponent<Camera>();
        }

        public override void EnableVR(float fieldOfView)
        {
            base.EnableVR(fieldOfView);

            cachedSkyboxMat = RenderSettings.skybox;
            RenderSettings.skybox = panoramicMaterial;
            myCamera.clearFlags = CameraClearFlags.Skybox;
            myCamera.cullingMask = 0; //-1 Everything, 0 Nothing
            if (depthOnlyCamera != null)
                depthOnlyCamera.gameObject.SetActive(true);
        }

        public override void EnableAR(float fieldOfView)
        {
            base.EnableAR(fieldOfView);

            RenderSettings.skybox = cachedSkyboxMat;
            myCamera.cullingMask = -1; //-1 Everything, 0 Nothing
            if (depthOnlyCamera != null)
                depthOnlyCamera.gameObject.SetActive(false);
        }

        public override void OnXRTransitionStart()
        {
            base.OnXRTransitionStart();
            RenderSettings.skybox = cachedSkyboxMat;
        }

        public virtual void Update()
        {
            if (depthOnlyCamera != null)
            {
                depthOnlyCamera.CopyFrom(myCamera);
                depthOnlyCamera.clearFlags = CameraClearFlags.Depth;
                depthOnlyCamera.cullingMask = -1; //-1 Everything, 0 Nothing
                depthOnlyCamera.depth = myCamera.depth + (useCameraDepthTexture ? -1 : 1);

                if (useCameraDepthTexture)
                    depthOnlyCamera.depthTextureMode = DepthTextureMode.Depth;
            }

            if(uiCamera != null)
            {
                uiCamera.CopyFrom(myCamera);
                uiCamera.clearFlags = CameraClearFlags.Depth;
                uiCamera.cullingMask = UICameraCullingMask.value;
                uiCamera.depth = 999;
            }
        }
    }
}