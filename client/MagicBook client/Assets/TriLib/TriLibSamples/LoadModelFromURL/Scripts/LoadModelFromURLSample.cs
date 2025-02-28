using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TriLibCore.Samples
{
    /// <summary>
    /// Represents a sample that loads a compressed (Zipped) Model.
    /// </summary>
    public class LoadModelFromURLSample : MonoBehaviour
    {
        /// <summary>
        /// The Model URL.
        /// </summary>
        public string ModelURL = "https://ricardoreis.net/trilib/demos/sample/TriLibSampleModel.zip";

        public bool FromStreamingAssets;

        public Image loadingRadialImage;
        public Transform container;
        public AssetLoaderOptions loaderOptions;

        /// <summary>
        /// Cached Asset Loader Options instance.
        /// </summary>
        //private AssetLoaderOptions _assetLoaderOptions;

        /// <summary>
        /// Creates the AssetLoaderOptions instance, configures the Web Request, and downloads the Model.
        /// </summary>
        /// <remarks>
        /// You can create the AssetLoaderOptions by right clicking on the Assets Explorer and selecting "TriLib->Create->AssetLoaderOptions->Pre-Built AssetLoaderOptions".
        /// </remarks>
        private void Start()
        {
            if (loaderOptions == null)
            {
                loaderOptions = AssetLoader.CreateDefaultLoaderOptions(false, true);
            }

            if (container == null)
                container = transform;

            var path = ModelURL;

            if(FromStreamingAssets)
                path = Path.Combine(Application.streamingAssetsPath, ModelURL);
            //var uri = new Uri(combined);

#if UNITY_ANDROID && !UNITY_EDITOR
            var webRequest = AssetDownloader.CreateWebRequest(path);
            AssetDownloader.LoadModelFromUri(webRequest, OnLoad, OnMaterialsLoad, OnProgress, OnError, null, loaderOptions);
#else
            AssetLoader.LoadModelFromFile(path, OnLoad, OnMaterialsLoad, OnProgress, OnError, null, loaderOptions);
#endif
        }

        /// <summary>
        /// Called when any error occurs.
        /// </summary>
        /// <param name="obj">The contextualized error, containing the original exception and the context passed to the method where the error was thrown.</param>
        private void OnError(IContextualizedError obj)
        {
            Debug.LogError($"An error occurred while loading your Model: {obj.GetInnerException()}");
            if (loadingRadialImage != null)
                loadingRadialImage.color = Color.red;
        }

        /// <summary>
        /// Called when the Model loading progress changes.
        /// </summary>
        /// <param name="assetLoaderContext">The context used to load the Model.</param>
        /// <param name="progress">The loading progress.</param>
        private void OnProgress(AssetLoaderContext assetLoaderContext, float progress)
        {
            Debug.Log($"Loading Model. Progress: {progress:P}");
            if (loadingRadialImage != null)
                loadingRadialImage.fillAmount = progress;
        }

        /// <summary>
        /// Called when the Model (including Textures and Materials) has been fully loaded.
        /// </summary>
        /// <remarks>The loaded GameObject is available on the assetLoaderContext.RootGameObject field.</remarks>
        /// <param name="assetLoaderContext">The context used to load the Model.</param>
        private void OnMaterialsLoad(AssetLoaderContext assetLoaderContext)
        {
            Debug.Log("Materials loaded. Model fully loaded.");
        }

        /// <summary>
        /// Called when the Model Meshes and hierarchy are loaded.
        /// </summary>
        /// <remarks>The loaded GameObject is available on the assetLoaderContext.RootGameObject field.</remarks>
        /// <param name="assetLoaderContext">The context used to load the Model.</param>
        private void OnLoad(AssetLoaderContext assetLoaderContext)
        {
            Debug.Log("Model loaded. Loading materials.");
            assetLoaderContext.RootGameObject.transform.SetParent(container, worldPositionStays: false);
        }
    }
}
