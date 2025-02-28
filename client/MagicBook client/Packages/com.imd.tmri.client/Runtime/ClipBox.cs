using System;
using TMRI.Core;
using UnityEngine;

namespace TMRI.Client
{
    [ExecuteAlways]
    public class ClipBox : MonoBehaviour
    {
        public static Matrix4x4 WorldToBox = Matrix4x4.identity;
        public bool Enabled = true;
        public BoxCollider boxCollider;

        Matrix4x4 previousPose;

        void Update()
        {
            WorldToBox = Enabled ? transform.worldToLocalMatrix : Matrix4x4.identity;

            Shader.SetGlobalMatrix("_BoxPose", transform.worldToLocalMatrix);
            Shader.SetGlobalMatrix("_WorldToBox", transform.worldToLocalMatrix);
            Shader.SetGlobalFloat("_UseWorldToBox", Enabled ? 1f : 0f);
            Shader.SetGlobalVector("_ClippingBox", transform.lossyScale);

#if UNITY_VISIONOS
            Unity.PolySpatial.PolySpatialShaderGlobals.SetMatrix("_BoxPose", transform.worldToLocalMatrix);
            Unity.PolySpatial.PolySpatialShaderGlobals.SetMatrix("_WorldToBox", transform.worldToLocalMatrix);
#endif

            if (previousPose != transform.worldToLocalMatrix)
            {
                previousPose = transform.worldToLocalMatrix;
                OnSizeChanged();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(transform.position, new Vector3(transform.localScale.x, 0.0001f, transform.localScale.z));
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif

        private void OnSizeChanged()
        {
            //this.ExecuteOnListeners<IClipBoxListener>(l => l.OnClipBoxChanged(transform.worldToLocalMatrix, transform.localToWorldMatrix), FindObjectsInactive.Include);
        }
    }
}