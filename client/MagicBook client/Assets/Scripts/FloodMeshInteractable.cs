using TMRI.Client;
using TMRI.Core;
using UnityEngine;
using UnityEngine.EventSystems;

public class FloodMeshInteractable : MonoBehaviour, ReticlePointerInteractable, IPointerClickHandler
{
    public GameObject InstantiateOnTouchPrefab;

    public Material GazeMaterial { get; set; }

    GameObject touchGameObjectInstance;

    public void OnDown(RaycastHit hitInfo)
    {
        //throw new System.NotImplementedException();
    }

    public void OnDrag(RaycastHit hitInfo)
    {
        //throw new System.NotImplementedException();
    }

    public void OnTapped(RaycastHit hitInfo)
    {
        if (FloodInfoPanel.instance == null)
            touchGameObjectInstance = Instantiate(InstantiateOnTouchPrefab, transform.parent.parent.parent);
        else
            touchGameObjectInstance = FloodInfoPanel.instance.gameObject;

        touchGameObjectInstance.transform.position = hitInfo.point;

        if(touchGameObjectInstance.TryGetComponent(out FloodInfoPanel fip))
        {
            var speed = GetFloodValueAtRaycastHit(hitInfo, FloodVisualizationType.Speed);
            var depth = GetFloodValueAtRaycastHit(hitInfo, FloodVisualizationType.Depth);

            //var elevation = (transform.worldToLocalMatrix * hitInfo.point).y - transform.position.y / transform.lossyScale.y;
            fip.SetFloodInfo(depth, speed, active: true);
        }
    }

    public void OnUp(RaycastHit hitInfo)
    {
        //throw new System.NotImplementedException();
    }

    public float GetFloodValueAtRaycastHit(RaycastHit hitInfo, FloodVisualizationType type)
    {
        var allMaterials = GetComponent<MeshRendererMaterials>().AvailableMaterials;
        var depthMat = allMaterials.Find(m => m.FloodType == type).FloodMaterial;
        var depthTex = (Texture2D)depthMat.GetTexture("_HeightMap");
        var depth = depthTex.GetPixelBilinear(hitInfo.textureCoord.x, hitInfo.textureCoord.y);
        //var depth = depthTex.GetPixel(Mathf.FloorToInt(hitInfo.textureCoord.x * depthTex.width), Mathf.FloorToInt(hitInfo.textureCoord.y * depthTex.height));
        var min = depthMat.GetFloat("_MinHeight");
        var max = depthMat.GetFloat("_MaxHeight");
        return depth.r * (max - min);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var raycastResult = eventData.pointerCurrentRaycast;
        Debug.Log($"OnPointerClick: {raycastResult.isValid}");
        if(raycastResult.isValid && TryGetComponent(out Collider col))
        {
            var ray = new Ray(Camera.main.transform.position, (raycastResult.worldPosition - Camera.main.transform.position).normalized);
            if (col.Raycast(ray, out RaycastHit hitinfo, 99f))
                OnTapped(hitinfo);
        }
    }
}
