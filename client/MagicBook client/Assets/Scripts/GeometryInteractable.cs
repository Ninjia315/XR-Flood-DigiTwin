using System;
using TMRI.Client;
using TMRI.Core;
using UnityEngine;
using UnityEngine.EventSystems;

[Serializable]
public class GeometryInteractable : MonoBehaviour, ReticlePointerInteractable, IPointerClickHandler
{
    public FloodInfoPanel InstantiateOnTouchPrefab;

    public Material GazeMaterial { get; set; }

    FloodInfoPanel touchGameObjectInstance;
    TMRIOnlineScene scene;

    private void Start()
    {
        scene = FindAnyObjectByType<TMRIOnlineScene>();
    }

    public void OnDown(RaycastHit hitInfo) { }
    public void OnUp(RaycastHit hitInfo) { }
    public void OnDrag(RaycastHit hitInfo) { }

    public void OnPointerClick(PointerEventData eventData)
    {
        var raycastResult = eventData.pointerCurrentRaycast;

        OnTapped(new RaycastHit { point = raycastResult.worldPosition });
    }

    public void OnTapped(RaycastHit hitInfo)
    {
        Debug.Log($"OnTapped geometry {(hitInfo.collider != null ? hitInfo.collider.gameObject.name : string.Empty)}");

        if(InstantiateOnTouchPrefab == null && scene != null)
        {
            var geomComponents = scene.geometryComponents;
            if (geomComponents != null && geomComponents.TryGetComponent(out GeometryInteractable gi))
                InstantiateOnTouchPrefab = gi.InstantiateOnTouchPrefab;
        }

        if (FloodInfoPanel.instance == null)
            touchGameObjectInstance = Instantiate(InstantiateOnTouchPrefab, scene.floodDataPlayer.transform);
        else
            touchGameObjectInstance = FloodInfoPanel.instance;

        touchGameObjectInstance.transform.position = hitInfo.point;
        touchGameObjectInstance.isDirty = true;
        touchGameObjectInstance.SetActive(true);
    }

}