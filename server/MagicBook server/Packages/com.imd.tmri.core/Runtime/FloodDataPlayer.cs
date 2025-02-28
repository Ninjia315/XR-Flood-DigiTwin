using System;
using System.Linq;
using TMRI.Core;
using UnityEngine;

namespace TMRI.Core
{
    public class FloodDataPlayer : MonoBehaviour, IAssetListener
    {
        public int CurrentTimeStep;
        public bool Autoplay;
        public float AutoplaySpeedSeconds = 1f;
        public int MaxTimeStep;
        public GeometryDataReader currentDataReader;
        [SerializeField]
        FloodVisualizationType FloodVisualization;

        int lastTimeStep;
        float lastRepetitionTime;
        public FloodVisualizationType lastVisualizationType { get; private set; }


        private void Update()
        {
            var p = currentDataReader;

            if (Autoplay && (Time.time - lastRepetitionTime) > AutoplaySpeedSeconds)
            {
                lastRepetitionTime = Time.time;
                var maxTimeStep = p == null ? MaxTimeStep : Math.Min(MaxTimeStep, p.transform.childCount);
                CurrentTimeStep = (CurrentTimeStep + 1) % maxTimeStep;
            }

            if (lastVisualizationType != FloodVisualization)
            {
                UpdateFloodVisualizationType(FloodVisualization);
            }

            if (p != null && CurrentTimeStep >= 0 && CurrentTimeStep < p.transform.childCount)
            {
                foreach (Transform c in p.transform)
                    c.gameObject.SetActive(false);

                var child = p.transform.GetChild(CurrentTimeStep);
                child.gameObject.SetActive(true);

                if (lastTimeStep != CurrentTimeStep)
                {
                    lastTimeStep = CurrentTimeStep;
                    this.ExecuteOnListeners<IFloodVisualizationListener>(listener => listener.OnFloodTimeStepUpdated(CurrentTimeStep));
                }
            }
        }

        public void UpdateFloodVisualizationType(FloodVisualizationType floodType)
        {
            if (lastVisualizationType == floodType && FloodVisualization == floodType)
                return;

            lastVisualizationType = floodType;
            FloodVisualization = floodType;
            this.ExecuteOnListeners<IFloodVisualizationListener>(listener => listener.SetFloodVisualizationType(floodType), FindObjectsInactive.Include);
        }

        public void OnAssetLoading(string assetID)
        {
            //gameObject.SetActive(false);
        }

        public void OnAssetChanged(string assetID)
        {
            //gameObject.SetActive(true);
            Debug.Log($"OnAssetChanged: {assetID}");
            var children = GetComponentsInChildren<GeometryDataReader>(true);
            currentDataReader = children.FirstOrDefault(c => c.gameObject != gameObject && c.assetID == assetID);
        }
    }

    public interface IFloodVisualizationListener
    {
        public void SetFloodVisualizationType(FloodVisualizationType type);
        public void OnFloodTimeStepUpdated(int timeStep);
    }
}