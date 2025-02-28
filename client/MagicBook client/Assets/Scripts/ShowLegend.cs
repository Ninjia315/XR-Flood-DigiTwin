using UnityEngine;
using TMRI.Core;
using TMRI.Client;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;


public class ShowLegend : MonoBehaviour, IFloodVisualizationListener, ILegendListener, IAssetListener
{
    public GameObject AbsoluteDepthContainer;
    public GameObject RelativeDepthContainer;
    public GameObject OpenLegendButton;
    public GameObject ToggleContainer;
    public TMP_Text MaxDepthText;
    public bool OnlyHMD;
    public GameObject LegendTemplatePrefab;
    public float PlacementOffset = 0.1f;

    float cachedMaxDepth;
    Dictionary<string, GameObject> instantiatedLegendImages = new();

    private void Start()
    {
        if (OnlyHMD)
        {
#if !UNITY_VISIONOS
            gameObject.SetActive(false);
#endif
            var tmriScene = FindAnyObjectByType<TMRIScene>();
            if(tmriScene != null)
                tmriScene.OnClippingBounds.AddListener(localCornerPos =>
                {
                    if(localCornerPos.x > localCornerPos.z)
                        transform.localPosition = new Vector3(0f, 0f, localCornerPos.z * .5f + PlacementOffset);
                    else
                        transform.localPosition = new Vector3(localCornerPos.x * .5f + PlacementOffset, 0f, 0f);
                });
        }
    }

    public void SetFloodVisualizationType(FloodVisualizationType type)
    {
        AbsoluteDepthContainer.SetActive(false);
        RelativeDepthContainer.SetActive(false);
        OpenLegendButton.SetActive(true);

        foreach (var gmd in FindObjectsByType<GenerateMeshFromData>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            cachedMaxDepth = Mathf.Max(cachedMaxDepth, gmd.MaxHeightOverallChunks);
            MaxDepthText.text = $"{cachedMaxDepth.ToString("F2")}m";
        }

        switch (type)
        {
            case FloodVisualizationType.Depth:
                RelativeDepthContainer.SetActive(true);
                if (ToggleContainer != null)
                    ToggleContainer.SetActive(true);
                break;
            case FloodVisualizationType.Depth_Alt:
                AbsoluteDepthContainer.SetActive(true);
                if (ToggleContainer != null)
                    ToggleContainer.SetActive(true);
                break;
            default:
                DisableWhenNoLegend();
                break;
        }
    }

    public void SetLegendImage(string id, Texture2D img)
    {
        OpenLegendButton.SetActive(true);
        ToggleContainer.SetActive(true);

        if (instantiatedLegendImages.ContainsKey(id))
        {
            instantiatedLegendImages[id].SetActive(true);
            return;
        }

        var inst = Instantiate(LegendTemplatePrefab, ToggleContainer.transform);

        if (inst.GetComponentInChildren<RawImage>() is RawImage uiImage)
        {
            uiImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, img.width);
            uiImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, img.height);
            uiImage.texture = img;
        }
        else if(inst.GetComponentInChildren<Renderer>() is Renderer r)
        {
            r.material.mainTexture = img;
        }

        instantiatedLegendImages[id] = inst;
    }

    public void RemoveLegendImage(string id)
    {
        if (instantiatedLegendImages.ContainsKey(id))
        {
            instantiatedLegendImages[id].SetActive(false);
            DisableWhenNoLegend();
        }
    }

    private void DisableWhenNoLegend()
    {
        var aChildWasActive = false;
        foreach (Transform child in ToggleContainer.transform)
            if (child.gameObject.activeSelf)
                aChildWasActive = true;

        if (!aChildWasActive)
        {
            OpenLegendButton.SetActive(false);
            ToggleContainer.SetActive(false);
        }
    }

    public void OnAssetLoading(string assetID)
    {
        OpenLegendButton.SetActive(false);
        ToggleContainer.SetActive(false);
    }

    public void OnAssetChanged(string assetID)
    {

    }

    public void OnFloodTimeStepUpdated(int timeStep)
    {
        // If you want to display the max depth for this time step only (in stead of max overall), uncomment:

        //if(FindFirstObjectByType<GenerateMeshFromData>() is GenerateMeshFromData gmd)
        //{
        //    MaxDepthText.text = $"{gmd.MaxHeightOverallChunks.ToString("F2")}m";
        //}
    }
}

