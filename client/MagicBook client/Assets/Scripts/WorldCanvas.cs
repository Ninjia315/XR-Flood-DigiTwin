using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMRI.Client;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Canvas))]
public class WorldCanvas : MonoBehaviour, IBaseXRCameraListener
{
    public float InitialDist = 1f;
    public Vector2 MinMaxScale = Vector2.one;
    [Range(0f, 1f)]
    public float DistOutput;
    public bool FlipZ;
    public bool CameraScaling = true;
    public bool CutRoll;

    Canvas canvas;

    public void OnBaseXRCamera(BaseXRCamera cam)
    {
        if(canvas != null && cam != null)
            canvas.worldCamera = cam.GetComponent<Camera>();
    }

    // Start is called before the first frame update
    void Start()
    {
        canvas = GetComponent<Canvas>();

        if (canvas.renderMode != RenderMode.WorldSpace)
            canvas.renderMode = RenderMode.WorldSpace;

    }

    // Update is called once per frame
    void Update()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        var lookAtCam = Application.isPlaying ? canvas.worldCamera : Camera.current;
        if (lookAtCam != null)
        {
#if UNITY_VISIONOS
            var lookatDir = (lookAtCam.transform.position - transform.position).normalized;
            transform.forward = FlipZ ? lookatDir : -lookatDir;
#else
            transform.forward = FlipZ ? -lookAtCam.transform.forward : lookAtCam.transform.forward;
#endif

            if (CutRoll)
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z - lookAtCam.transform.localEulerAngles.z);

            if (CameraScaling)
            {
                var camPos = lookAtCam.transform.position;
                var dist = Vector3.Distance(canvas.transform.position, camPos);

                if (MinMaxScale.sqrMagnitude > 0f)
                {
                    dist = Mathf.Clamp(dist / InitialDist, MinMaxScale.x, MinMaxScale.y);
                    DistOutput = Mathf.InverseLerp(MinMaxScale.x, MinMaxScale.y, dist);
                }
                else
                {
                    dist /= InitialDist;
                }

                canvas.transform.localScale = Vector3.one * dist;//(dist / InitialDist);
            }
        }
        else if (Application.isPlaying)
        {
            canvas.worldCamera = Camera.main;
        }
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        // Ensure continuous Update calls.
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }

    public void OnEnableAR()
    {
    }

    public void OnEnableVR()
    {
    }

    public void OnXRTransitionStart()
    {
    }
}
