using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using TMRI.Core;
using UnityEngine;
using UnityEngine.UI;

public class LandmarkBehaviour : MonoBehaviour, LandmarkBehaviour.ILandmarkTypeListener
{
    public int ViewingLevel;
    [SerializeField]
    TMP_Text LandmarkText;
    [SerializeField]
    GameObject ViewingToggleGO;
    [SerializeField]
    List<ViewingLevelDefinition> ViewingLevels;
    [SerializeField]
    GameObject MapPoint3DIndicator;
    [SerializeField]
    RawImage IconTemplate;

    ViewingLevelDefinition vld;
    bool uiVisible = true;

    [SerializeField]
    List<LandmarkIconEntry> landmarkIcons = new();

    [Serializable]
    class ViewingLevelDefinition
    {
        [SerializeField]
        internal int Level;
        [SerializeField]
        internal float MaxDistance;
        [SerializeField]
        internal float MaxFontSize;
    }

    [Serializable]
    public class LandmarkIconEntry
    {
        public int id;
        public GameObject icon;
    }

    public void SetInfo(string landmarkText, int viewingLevel, List<SerializableLandmarkType> iconTypes, Dictionary<int, Texture2D> icons)
    {
        LandmarkText.text = landmarkText;
        ViewingLevel = viewingLevel;

        if (ViewingLevels != null)
            vld = ViewingLevels.FirstOrDefault(vl => vl.Level == ViewingLevel);

        foreach (var iconType in iconTypes)
        {
            if (icons.ContainsKey(iconType.id))
            {
                var icon = Instantiate(IconTemplate, IconTemplate.transform.parent);
                icon.texture = icons[iconType.id];
                icon.gameObject.SetActive(true);
                if (landmarkIcons.FirstOrDefault(li => li.id == iconType.id) is LandmarkIconEntry lmi)
                    lmi.icon = icon.gameObject;
                else
                    landmarkIcons.Add(new LandmarkIconEntry { id = iconType.id, icon = icon.gameObject });
            }
        }

        if(iconTypes.Any())
        {
            MapPoint3DIndicator.SetActive(false);
        }

        IconTemplate.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        var visibleInDistance = true;
        var visibleInClipBox = true;

        if (vld != null)
        {
            visibleInDistance = (Camera.main.transform.position - transform.position).sqrMagnitude < (vld.MaxDistance*vld.MaxDistance);

            if(vld.MaxFontSize > LandmarkText.fontSizeMin)
                LandmarkText.fontSizeMax = vld.MaxFontSize;
        }

        var cumulativeVisible = uiVisible && visibleInDistance && visibleInClipBox;
        ViewingToggleGO.SetActive(cumulativeVisible);
        MapPoint3DIndicator.SetActive(cumulativeVisible && landmarkIcons.Count == 0);

        ViewingToggleGO.transform.forward = Camera.main.transform.forward;
    }

    public void OnLandmarkTypeVisible(int typeId, bool visible)
    {
        if (landmarkIcons.FirstOrDefault(li => li.id == typeId) is LandmarkIconEntry lmi)
            lmi.icon.SetActive(visible);

        if(landmarkIcons.Any())
            uiVisible = landmarkIcons.Any(li => li.icon.activeSelf);
    }

    public interface ILandmarkTypeListener
    {
        public void OnLandmarkTypeVisible(int typeId, bool visible);
    }
}