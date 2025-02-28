using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TMRI.Client;
using TMRI.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class FloodDataListener : BaseTMRICallback
{
    public List<GameObject> AffectedBuildings;
    public TMRIOnlineScene onlineScene;
    public FloodDataPlayer dataPlayer;

    public UnityEvent<float> OnMaxFilesChanged;

    bool lastAffectedBuildings;

    protected override void OnTMRIMessage(WebsocketMessage msg)
    {
        switch (msg.type)
        {
            case "FLOOD_VISUALIZATION_DATA":
                if (JsonConvert.DeserializeObject<FloodVisualizationData>(msg.data) is FloodVisualizationData floodData)
                {
                    dataPlayer.AutoplaySpeedSeconds = floodData.AnimationSpeedSeconds;
                    dataPlayer.MaxTimeStep = floodData.MaxAnimationStep;
                    dataPlayer.CurrentTimeStep = floodData.AnimationStep;

                    /// Since the operator sends an event every time the timestep changes (even while playing) we do
                    /// not want to fight with those events by playing our own; so always turn auto-play OFF
                    dataPlayer.Autoplay = false;

                    if (AffectedBuildings != null && lastAffectedBuildings != floodData.ShowAffectedBuildings)
                    {
                        var affectedBuildingsGO = AffectedBuildings.FirstOrDefault(af => af != null && af.name == onlineScene.GetCurrentAsset());

                        if (affectedBuildingsGO != null)
                        {
                            foreach (var go in AffectedBuildings)
                                go.SetActive(false);

                            affectedBuildingsGO.SetActive(floodData.ShowAffectedBuildings);
                            lastAffectedBuildings = floodData.ShowAffectedBuildings;
                        }
                    }

                    dataPlayer.UpdateFloodVisualizationType(floodData.FloodType);
                }
                break;

            case "GEO_IMAGE":
                if (JsonConvert.DeserializeObject<GeoImageVisualizationData>(msg.data) is GeoImageVisualizationData geoImageData)
                {
                    foreach(var mapProjector in FindObjectsByType<MapProjector>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        mapProjector.ToggleActive(mapProjector.ID == geoImageData.ShowGeoImageId);
                        mapProjector.SetOpacity(geoImageData.GeoImageOpacity);
                    }
                    onlineScene.currentGeoImageID = geoImageData.ShowGeoImageId;
                }

                break;
        }
    }

}


