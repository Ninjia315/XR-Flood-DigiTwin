using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using TMRI.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TMRI.Client
{
    public class LandmarkBehaviour : BaseTMRICallback
    {
        public int ViewingLevel;
        [SerializeField]
        TMP_Text LandmarkText;
        [SerializeField]
        GameObject ViewingToggleGO;
        [SerializeField]
        List<ViewingLevelDefinition> ViewingLevels;
        [SerializeField]
        List<IconDefinition> Icons;
        [SerializeField]
        SpriteRenderer IconSpriteRenderer;
        [SerializeField]
        GameObject MapPoint3DIndicator;
        [SerializeField]
        RawImage IconTemplate;

        ViewingLevelDefinition vld;
        bool uiVisible = true;
        Dictionary<int, GameObject> landmarkIcons = new();

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
        class IconDefinition
        {
            [SerializeField]
            internal int Type;
            [SerializeField]
            internal Sprite IconSprite;
        }

        private void Start()
        {
            if(TMRIOnlineScene.UIVisible != null)
                uiVisible = TMRIOnlineScene.UIVisible.Value;

            if(TMRIOnlineScene.LandmarksTypesVisible != null)
                ToggleBasedOnVisibleTypes(TMRIOnlineScene.LandmarksTypesVisible);
        }

        public void SetInfo(string landmarkText, int viewingLevel, List<SerializableLandmarkType> iconTypes, Dictionary<int, Texture2D> icons)
        {
            LandmarkText.text = landmarkText;
            ViewingLevel = viewingLevel;

            if(ViewingLevels != null)
                vld = ViewingLevels.FirstOrDefault(vl => vl.Level == ViewingLevel);

            foreach (var iconType in iconTypes)
            {
                if (icons.ContainsKey(iconType.id))
                {
                    var icon = Instantiate(IconTemplate, IconTemplate.transform.parent);
                    icon.texture = icons[iconType.id];
                    icon.gameObject.SetActive(true);
                    landmarkIcons[iconType.id] = icon.gameObject;
                }
                else
                {
                    var iconGO = Instantiate(IconSpriteRenderer.gameObject, IconSpriteRenderer.transform.parent);
                    var iconDef = Icons.FirstOrDefault(i => i.Type == iconType.id);
                    iconGO.GetComponent<SpriteRenderer>().sprite = iconDef.IconSprite;
                }
            }

            //if (Icons != null && iconTypes.Any() && Icons.FirstOrDefault(i => i.Type == iconTypes[0]) is IconDefinition icon)
            if(iconTypes.Any())
            {
            //    IconSpriteRenderer.sprite = icon.IconSprite;
                MapPoint3DIndicator.SetActive(false);
            }
            //else
            {
                IconTemplate.gameObject.SetActive(false);
                IconSpriteRenderer.gameObject.SetActive(false);
            }
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

            if(ClipBox.WorldToBox != Matrix4x4.identity)
            {
                var center = ClipBox.WorldToBox.MultiplyPoint3x4(transform.position);

                visibleInClipBox = (center.x > -.5f && center.x < .5f && center.z > -.5f && center.z < .5f);
            }

            ViewingToggleGO.SetActive(uiVisible && visibleInDistance && visibleInClipBox);
        }

        protected override void OnTMRIMessage(WebsocketMessage msg)
        {
            switch (msg.type)
            {
                case "TOGGLE_UI":
                    if (bool.TryParse(msg.data, out uiVisible))
                    {
                        //gameObject.SetActive(uiVisible);
                    }
                    break;
                case "TOGGLE_LANDMARK_TYPES":
                    var visibleList = JsonConvert.DeserializeObject<List<SerializableLandmarkType>>(msg.data);

                    ToggleBasedOnVisibleTypes(visibleList);

                    break;
            }
        }

        private void ToggleBasedOnVisibleTypes(List<SerializableLandmarkType> visibleList)
        {
            foreach (var icon in landmarkIcons.Values)
                icon.SetActive(false);

            foreach (var visibleLandmarkType in visibleList)
            {
                if (landmarkIcons.ContainsKey(visibleLandmarkType.id))
                    landmarkIcons[visibleLandmarkType.id].SetActive(true);
            }

            if (landmarkIcons.Any())
                uiVisible = landmarkIcons.Any(lt => lt.Value.activeSelf);
        }
    }
}
