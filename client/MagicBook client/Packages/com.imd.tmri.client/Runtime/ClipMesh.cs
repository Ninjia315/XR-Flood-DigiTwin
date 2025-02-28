using System.Linq;
using TMRI.Core;
using UnityEngine;

namespace TMRI.Client
{
    [RequireComponent(typeof(BoxCollider), typeof(MeshRenderer))]
    public class ClipTiledMesh : MonoBehaviour
    {
        BoxCollider bc;
        MeshRenderer mr;
        public int NumLODs = 3;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            mr = GetComponent<MeshRenderer>();
            bc = GetComponent<BoxCollider>();
            if (bc == null)
                bc = gameObject.AddComponent<BoxCollider>();
        }

        // Update is called once per frame
        void Update()
        {
            if (mr == null || bc == null || bc.size == Vector3.zero || ClipBox.WorldToBox == Matrix4x4.identity)
            {
                mr.enabled = true;
                bc.enabled = true;
                return;
            }

            var center = ClipBox.WorldToBox.MultiplyPoint3x4(transform.position);

            if (center.x > -.5f && center.x < .5f && center.z > -.5f && center.z < .5f)
            {
                mr.enabled = true;
            }
            else
            {
                var worldMin = transform.TransformPoint(bc.center + new Vector3(-bc.size.x, -bc.size.y, -bc.size.z) * 0.5f);
                var worldMax = transform.TransformPoint(bc.center + new Vector3(bc.size.x, bc.size.y, bc.size.z) * 0.5f);
                var a = ClipBox.WorldToBox.MultiplyPoint3x4(worldMin);
                var b = ClipBox.WorldToBox.MultiplyPoint3x4(worldMax);
                var aIsInside = (a.x > -.5f && a.x < .5f) && (a.z > -.5f && a.z < .5f);
                var bIsInside = (b.x > -.5f && b.x < .5f) && (b.z > -.5f && b.z < .5f);
                var cIsInside = (a.x > -.5f && a.x < .5f) && (b.z > -.5f && b.z < .5f);
                var dIsInside = (a.z > -.5f && a.z < .5f) && (b.x > -.5f && b.x < .5f);

                mr.enabled = aIsInside || bIsInside || cIsInside || dIsInside;

                // The following simple overlap checker is better but too heavy:
                //mr.enabled = bc.bounds.Intersects(new Bounds(ClipBox.WorldToBox.inverse * Vector3.zero, ClipBox.WorldToBox.inverse * new Vector3(1f, 99f, 1f)));
            }

            bc.enabled = mr.isVisible;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var worldMin = transform.TransformPoint(bc.center + new Vector3(-bc.size.x, -bc.size.y, -bc.size.z) * 0.5f);
            var worldMax = transform.TransformPoint(bc.center + new Vector3(bc.size.x, bc.size.y, bc.size.z) * 0.5f);

            UnityEditor.Handles.Label(worldMin, ClipBox.WorldToBox.MultiplyPoint3x4(worldMin).ToString());
            UnityEditor.Handles.Label(worldMax, ClipBox.WorldToBox.MultiplyPoint3x4(worldMax).ToString());

            var minDistToCamera = 0.01f;
            var maxDistToCamera = 1.5f;
            var closestPoint = bc.ClosestPoint(Camera.main.transform.position);
            var distToCamera = Vector3.Distance(Camera.main.transform.position, closestPoint);
            var lodLevel = GetComponentInParent<Core.AssetHandler.ModelLODBehaviour>().lodIndex;
            
            var l = Mathf.InverseLerp(minDistToCamera, maxDistToCamera, distToCamera);
            var lodFracStart = lodLevel / (float)NumLODs;
            var lodFracEnd = (lodLevel + 1) / (float)NumLODs;
            UnityEditor.Handles.Label(transform.position, $"lodLevel:{lodLevel} srt:{lodFracStart} end:{lodFracEnd} l:{l}");
        }
#endif
    }
}