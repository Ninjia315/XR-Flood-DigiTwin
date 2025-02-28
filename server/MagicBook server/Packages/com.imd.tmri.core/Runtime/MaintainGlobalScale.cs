using NUnit.Framework.Interfaces;
using UnityEngine;

public class MaintainGlobalScale : MonoBehaviour
{
    public bool UseTransformScaleOnStart = true;
    public Vector3 GlobalScale;
    public bool ScaleToMainCamera;
    public Vector2 MinMaxCamDistance = Vector2.one;
    public float InitialDist = 1f;
    [Range(0f, 1f)]
    public float DistOutput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(UseTransformScaleOnStart)
            GlobalScale = transform.lossyScale;
    }


    private void LateUpdate()
    {
        transform.localScale = Vector3.one;
        transform.localScale = new Vector3(GlobalScale.x / transform.lossyScale.x, GlobalScale.y / transform.lossyScale.y, GlobalScale.z / transform.lossyScale.z);

        if (ScaleToMainCamera)
        {
            //var camPos = Camera.main.transform.position;
            //var dist = Vector3.Distance(transform.position, camPos);
            var dist = Mathf.Abs(Camera.main.transform.InverseTransformPoint(transform.position).z);
            var frac = 1f;

            if (MinMaxCamDistance.sqrMagnitude > 0f)
            {
                dist = Mathf.Clamp(dist, MinMaxCamDistance.x, MinMaxCamDistance.y);
                frac = Mathf.InverseLerp(MinMaxCamDistance.x, MinMaxCamDistance.y, dist);
            }

            DistOutput = frac;
            transform.localScale *= dist / InitialDist;
        }
    }
}
