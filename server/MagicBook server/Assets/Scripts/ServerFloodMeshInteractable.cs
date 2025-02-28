using System.Collections;
using System.Collections.Generic;
using TMRI.Core;
using UnityEngine;
using UnityEngine.EventSystems;

public class ServerFloodMeshInteractable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject TouchPointPrefab;

    float pointerDownTime;

    static GameObject highlightInstance;

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownTime = Time.time;
        if (GetComponentInParent<DragMap>() is DragMap dm)
        {
            dm.OverrideDragging(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (GetComponentInParent<DragMap>() is DragMap dm)
        {
            dm.EndOverrideDragging(eventData);
        }

        if (Time.time - pointerDownTime < 0.2f)
            HighlightFloodInfo(eventData.pointerPressRaycast);
    }

    void HighlightFloodInfo(RaycastResult raycastResult)
    {
        Debug.Log($"Flood point: {raycastResult.worldPosition}");
        var localPos = raycastResult.gameObject.transform.InverseTransformPoint(raycastResult.worldPosition);

        if (highlightInstance == null && TouchPointPrefab != null)
        {
            highlightInstance = Instantiate(TouchPointPrefab);
            highlightInstance.transform.parent = FindFirstObjectByType<FloodDataPlayer>().transform;
        }

        if (highlightInstance != null)
        {
            highlightInstance.transform.position = raycastResult.worldPosition;
        }
    }

    
}
