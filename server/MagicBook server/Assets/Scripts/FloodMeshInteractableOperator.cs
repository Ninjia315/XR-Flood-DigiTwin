using System.Collections;
using System.Collections.Generic;
using TMRI.Core;
using UnityEngine;
using UnityEngine.EventSystems;

public class FloodMeshInteractableOperator : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        var localPos =  eventData.pointerCurrentRaycast.worldPosition;

        Debug.Log($"1: {eventData.pointerCurrentRaycast.worldPosition} 2: {transform.position.y} 3: {localPos}");
        var depth = GeoCoordinateConverter.Unity4ToGeo(localPos).y - transform.position.y / transform.lossyScale.y;
        var elevation = (localPos).y - transform.position.y / transform.lossyScale.y;

        //FindAnyObjectByType<WebsocketServerWithGUI>().SetFloodInfoPosition(new Vector3(localPos.x, localPos.y, localPos.z), depth - 37.94f, 0f, elevation - 37.94f);
    }

}
