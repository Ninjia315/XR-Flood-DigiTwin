using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMRI.Core;
using UnityEngine;
using UnityEngine.Networking;

public class LocalFileDownloader : MonoBehaviour
{
    [Serializable]
    public class PlatformAssetHash
    {
        public List<AssetHandler.BuildPlatform> Platforms;
        public string AssetName;
        public string AssetHash;
    }

    public List<PlatformAssetHash> AssetsToLoad;

    //public Action<string, AssetBundle> OnAssetBundleLoaded;

    private Dictionary<string, List<GameObject>> _loadedAssets = new();
    private PlatformAssetHash asset;

    private void Start()
    {
        //if (string.IsNullOrEmpty(AssetName) || string.IsNullOrEmpty(AssetHash))
        if(AssetsToLoad == null || AssetsToLoad.Count == 0)
            return;

        var platform = AssetHandler.GetRuntimePlatform();
        asset = AssetsToLoad.Find(a => a.Platforms.Any(p => p.ToString() == platform));
        var combined = Path.Combine(Application.streamingAssetsPath, platform, asset.AssetName);
        var uri = new Uri(combined);
        Debug.Log($"platform: {platform}\ncombined: {combined}");

        StartCoroutine(DownloadAndReturnAsset(uri, asset.AssetHash, asset.AssetName));
    }

    private IEnumerator DownloadAndReturnAsset(Uri downloadUrl, string hash, string name)
    {
        //var uri = new Uri(downloadUrl);

        Debug.Log($"{downloadUrl} makes uri: {downloadUrl.AbsoluteUri}");

        //for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            //using (UnityWebRequest downloadRequest = UnityWebRequestAssetBundle.GetAssetBundle(downloadUrl, Hash128.Parse(crc), crc:0))
            using (UnityWebRequest downloadRequest = UnityWebRequestAssetBundle.GetAssetBundle(downloadUrl, new CachedAssetBundle()
            {
                hash = Hash128.Parse(hash),
                name = name
            }, crc: 0))
            {
                yield return downloadRequest.SendWebRequest();

                if (downloadRequest.result == UnityWebRequest.Result.Success)
                {
                    //PlayerPrefs.SetString($"CACHE_{name}_URL", downloadRequest.url);

                    AssetBundle assetBundle = DownloadHandlerAssetBundle.GetContent(downloadRequest);
                    if (assetBundle != null)
                    {
                        //OnAssetBundleLoaded(name, assetBundle);
                        yield return LoadAssetsFromBundle(name, assetBundle);
                    }
                    else
                    {
                        Debug.LogError("Error: AssetBundle is null.");
                    }

                    //inProgressDownloads.Remove(name);
                    yield break;
                }
            }

            // Wait before retrying
            //yield return new WaitForSeconds(RetryDelay);
        }

        // Final cleanup if all retries failed
        Debug.LogError($"Failed to download asset bundle '{name}'");
        //inProgressDownloads.Remove(name);
    }

    private IEnumerator LoadAssetsFromBundle(string name, AssetBundle assetBundle)
    {
        //Debug.Log("AssetHandler: LoadAssetsFromBundle start.");
        List<GameObject> assets = new();
        string[] assetNames = assetBundle.GetAllAssetNames();
        int batchSize = 5; // Number of assets to load per frame
        int assetCount = assetNames.Length;
        //Debug.Log($"AssetHandler: Assetcount: {assetCount}");
        for (int i = 0; i < assetCount; i++)
        {
            var assetRequest = assetBundle.LoadAssetAsync<GameObject>(assetNames[i]);
            yield return assetRequest;

            if (assetRequest.asset is GameObject asset)
            {
                assets.Add(asset);
            }
            else
            {
                Debug.LogWarning($"Asset {assetNames[i]} is not a GameObject.");
            }

            // Yield control back to Unity every batchSize assets
            if ((i + 1) % batchSize == 0)
            {
                yield return null;
            }
        }

        assetBundle.Unload(false);

        _loadedAssets[name] = assets;

        Debug.Log($"{name} assets loaded and ready.");
        //AssetStatus[name] = false;
    }

    bool once = true;
    private void Update()
    {
        if(once && asset != null && _loadedAssets.TryGetValue(asset.AssetName, out List<GameObject> assets))
        {
            once = false;
            foreach (var a in assets)
                Instantiate(a, transform);
        }
    }
}
