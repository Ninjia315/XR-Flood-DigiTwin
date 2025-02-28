using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MobfishCardboard
{
    public class CardboardPostCamera: MonoBehaviour
    {
        [SerializeField]
        private Material eyeMaterialLeft;
        [SerializeField]
        private Material eyeMaterialRight;

        private bool srpInUse;
        private Camera postCam;

        private void Awake()
        {
            postCam = GetComponent<Camera>();

            var ortho = Matrix4x4.Ortho(-1, 1, -1, 1, -0.1f, 0.5f);
            
            postCam.projectionMatrix = ortho;
            //postCam.orthographicSize = 2f;

            srpInUse = GraphicsSettings.defaultRenderPipeline != null;
        }

        public void SetOrthographicSize(float size)
        {
            var ortho = Matrix4x4.Ortho(-1, 1, -1, 1, -0.1f, 0.5f);
            ortho.m00 *= Mathf.Tan(Mathf.Deg2Rad * size); // Horizontal scale for 120-degree FoV
            ortho.m11 *= Mathf.Tan(Mathf.Deg2Rad * size); // Vertical scale for 120-degree FoV
            postCam.projectionMatrix = ortho;
        }

        private void Start()
        {
            if (srpInUse)
            {
                Debug.Log("CardboardPostCamera, current RenderPipeline = " +
                    GraphicsSettings.defaultRenderPipeline?.GetType().ToString());
            }
        }

        private void OnEnable()
        {
            ApplyRenderTexture();
            CardboardManager.renderTextureResetEvent += ApplyRenderTexture;

            if (srpInUse)
            {
                #if UNITY_2019_1_OR_NEWER
                RenderPipelineManager.endCameraRendering += OnSrpCameraPostRender;
                #endif
            }
            else
            {
                Camera.onPostRender += OnCameraPostRender;
            }
        }

        private void OnDisable()
        {
            CardboardManager.renderTextureResetEvent -= ApplyRenderTexture;

            if (srpInUse)
            {
                #if UNITY_2019_1_OR_NEWER
                RenderPipelineManager.endCameraRendering -= OnSrpCameraPostRender;
                #endif
            }
            else
            {
                Camera.onPostRender -= OnCameraPostRender;
            }
        }

        #if UNITY_2019_1_OR_NEWER
        private void OnSrpCameraPostRender(ScriptableRenderContext context, Camera givenCamera)
        {
            PostEyeRender();
        }
        #endif

        private void OnCameraPostRender(Camera cam)
        {
            PostEyeRender();
        }

        private void PostEyeRender()
        {
            if (!CardboardManager.profileAvailable)
                return;

            eyeMaterialLeft.SetPass(0);
            Graphics.DrawMeshNow(CardboardManager.viewMeshLeft, transform.position, transform.rotation);
            eyeMaterialRight.SetPass(0);
            Graphics.DrawMeshNow(CardboardManager.viewMeshRight, transform.position, transform.rotation);
        }

        private void ApplyRenderTexture()
        {
            eyeMaterialLeft.mainTexture = CardboardManager.viewTextureLeft;
            eyeMaterialRight.mainTexture = CardboardManager.viewTextureRight;
        }
    }
}