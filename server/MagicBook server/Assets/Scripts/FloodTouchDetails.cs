using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMRI.Core;
using UnityEngine;
using UnityEngine.EventSystems;

public class FloodTouchDetails : MonoBehaviour, IFloodVisualizationListener
{
    public TMP_Text depthText;
    public TMP_Text speedText;
    public TMP_Text elevationText;
    public Transform lookAtCameraTransform;
    public LayerMask raycastLayerMask;

    Vector3 lastLocalPosition;

    private void Start()
    {
        SampleFloodDetails();
    }

    public void SetFloodDetails(float depth, float speed, float? elevation=null)
    {
        depthText.text = $"Depth: {depth.ToString("F2")}m";//浸水深: 
        speedText.text = $"Speed: {speed.ToString("F1")}m/s";//流速:

        if(elevation != null)
            elevationText.text = $"Elevation: {elevation.Value.ToString("F0")}m";//標高: 
    }

    private void Update()
    {
        if (lookAtCameraTransform != null)
            lookAtCameraTransform.forward = Camera.main.transform.forward;

        if (lastLocalPosition != transform.localPosition)
        {
            SampleFloodDetails();
            lastLocalPosition = transform.localPosition;
        }
    }

    void SampleFloodDetails()
    {
        //var server = FindObjectOfType<WebsocketServerWithGUI>();
        if (Physics.Raycast(
            new Ray(transform.position + Vector3.up * 0.1f, Vector3.down),
            out RaycastHit hitInfo, 10f, raycastLayerMask.value))
        {
            var speed = GetFloodValueAtRaycastHit(hitInfo, FloodVisualizationType.Speed);
            var depth = GetFloodValueAtRaycastHit(hitInfo, FloodVisualizationType.Depth);

            //var elevation = (transform.worldToLocalMatrix * hitInfo.point).y - transform.position.y / transform.lossyScale.y;
            var elevation = transform.localPosition.y;
            //server.SetFloodInfoPosition(localPos, depth, speed, elevation);

            SetFloodDetails(depth, speed, elevation);
        }
        else
        {
            SetFloodDetails(0f, 0f);
        }
    }

    float GetFloodValueAtRaycastHit(RaycastHit hitInfo, FloodVisualizationType type)
    {
        var materialsComponent = hitInfo.collider.GetComponent<MeshRendererMaterials>();
        if (materialsComponent == null)
            return 0f;

        var allMaterials = materialsComponent.AvailableMaterials;
        var depthMat = allMaterials.Find(m => m.FloodType == type).FloodMaterial;
        var depthTex = (Texture2D)depthMat.GetTexture("_HeightMap");
        var depth = depthTex.GetPixelBilinear(hitInfo.textureCoord.x, hitInfo.textureCoord.y);
        //var depth = depthTex.GetPixel(Mathf.FloorToInt(hitInfo.textureCoord.x * depthTex.width), Mathf.FloorToInt(hitInfo.textureCoord.y * depthTex.height));
        var min = depthMat.GetFloat("_MinHeight");
        var max = depthMat.GetFloat("_MaxHeight");
        return depth.r * (max - min);
    }

    public void SetFloodVisualizationType(FloodVisualizationType type)
    {
    }

    public void OnFloodTimeStepUpdated(int timeStep)
    {
        SampleFloodDetails();
    }
}
