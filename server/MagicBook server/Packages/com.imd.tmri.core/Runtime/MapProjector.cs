using System.Linq;
using TMRI.Core;
using UnityEditor;
using UnityEngine;

namespace TMRI.Core
{
    [RequireComponent(typeof(Projector))]
    public class MapProjector : MonoBehaviour
    {
        public string ID;
        public int borderSize = 2;
        public Texture2D imageTexture;
        public Vector2 minLatLon;
        public Vector2 maxLatLon;
        public Vector3 referenceLatLonAlt;
        public Projector projector;
        public bool configureOnStart;
        public string assetID;
        [Range(0,1)]
        public float opacity = 1f;
        public Texture2D legendTexture;

        float orthoSize;

        private void Start()
        {
            if(projector == null)
                projector = GetComponent<Projector>();

            if(projector.material != null)
                projector.material = new Material(projector.material);

            if (configureOnStart)
            {
                ConfigureProjector();
                //ToggleActive(true);
            }
        }

        public void ToggleActive(bool active)
        {
            projector.enabled = active;

            if (legendTexture != null)
                this.ExecuteOnListeners<ILegendListener>(listener =>
                {
                    if (active)
                        listener.SetLegendImage(ID, legendTexture);
                    else
                        listener.RemoveLegendImage(ID);
                });
        }

        // Function to set the projector properties
        public void ConfigureProjector()//Vector2 minLatLon, Vector2 maxLatLon)
        {
            if (projector == null || imageTexture == null)
            {
                Debug.LogError("Projector or image texture is missing.");
                return;
            }

            Texture2D borderedTexture = AddAlphaBorder(imageTexture, borderSize); // Adjust border size as needed

            var worldMin = GeoCoordinateConverter.GeoToUnity(minLatLon.x, minLatLon.y, 0.0, referenceLatLonAlt.x, referenceLatLonAlt.y, referenceLatLonAlt.z, 1.0f);
            var worldMax = GeoCoordinateConverter.GeoToUnity(maxLatLon.x, maxLatLon.y, 0.0, referenceLatLonAlt.x, referenceLatLonAlt.y, referenceLatLonAlt.z, 1.0f);
            worldMin = new Vector3(-worldMin.x, worldMin.y, -worldMin.z);
            worldMax = new Vector3(-worldMax.x, worldMax.y, -worldMax.z);

            // Compute center position in world coordinates
            Vector3 worldCenter = (worldMin + worldMax) * 0.5f;

            // Set projector position (assuming it projects downward from above)
            projector.transform.localPosition = new Vector3(worldCenter.x, 9999, worldCenter.z);
            projector.transform.localRotation = Quaternion.Euler(90, 180, 0); // Ensure it's projecting downwards
            projector.transform.localScale = Vector3.one;

            // Set aspect ratio based on image dimensions
            float aspectRatio = (float)borderedTexture.width / borderedTexture.height;
            projector.aspectRatio = aspectRatio;

            // Compute orthographic size (half the height in world units)
            orthoSize = Mathf.Abs(worldMax.z - worldMin.z) * 0.5f;
            projector.orthographicSize = orthoSize * transform.lossyScale.x;

            // Apply the texture to the projector material
            projector.material.SetTexture("_ShadowTex", borderedTexture);

            SetOpacity(opacity);
        }

        public void SetOpacity(float opacity)
        {
            if (projector.material == null)
                return;

            projector.material.SetFloat("_Opacity", opacity);
        }

        private void Update()
        {
            projector.orthographicSize = orthoSize * transform.lossyScale.x;
        }

        private Texture2D AddAlphaBorder(Texture2D sourceTexture, int borderSize)
        {
            var clipColor = new Color(1, 1, 1, 0);
            int width = sourceTexture.width;
            int height = sourceTexture.height;
            Texture2D newTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            newTexture.SetPixels(sourceTexture.GetPixels());

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < borderSize; y++)
                {
                    newTexture.SetPixel(x, y, clipColor); // Bottom border
                    newTexture.SetPixel(x, height - 1 - y, clipColor); // Top border
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < borderSize; x++)
                {
                    newTexture.SetPixel(x, y, clipColor); // Left border
                    newTexture.SetPixel(width - 1 - x, y, clipColor); // Right border
                }
            }

            newTexture.Apply();
            newTexture.wrapMode = TextureWrapMode.Clamp;
            return newTexture;
        }

    }

    public interface ILegendListener
    {
        public void SetLegendImage(string id, Texture2D img);
        public void RemoveLegendImage(string id);
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(MapProjector))]
    public class MapProjectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Configure with texture"))
            {
                var script = (MapProjector)target;

                script.ConfigureProjector();
            }
        }
    }
#endif
} //TMRI.Core