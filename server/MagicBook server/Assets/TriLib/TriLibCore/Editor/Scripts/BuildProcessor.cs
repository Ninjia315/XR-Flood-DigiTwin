using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TriLibCore.Mappers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TriLibCore.Editor
{
    public class BuildProcessor
    {
        public static void OnPostprocessBuild(Dictionary<string, string> removedFromBuild)
        {
            RestoreAssets(removedFromBuild);
        }

        public static void OnPreprocessBuild(Dictionary<string, string> removedFromBuild)
        {
#if TRILIB_ENABLE_WEBGL_THREADS
            PlayerSettings.WebGL.threadsSupport = true;
#else
            PlayerSettings.WebGL.threadsSupport = false;
#endif
            if (!Application.isBatchMode)
            {
#if UNITY_WSA
            if (!PlayerSettings.WSA.GetCapability(PlayerSettings.WSACapability.RemovableStorage) && EditorUtility.DisplayDialog(
                    "TriLib", "TriLib cache system needs the [RemovableStorage] WSA Capacity enabled. Do you want to enable it now?", "Yes", "No"))
            {
                PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.RemovableStorage, true);
            }
#endif
            }
            CheckMappers.EnableCompatibleMaterialMapperIfNeeded();
        }

        private static bool AssetExists(Object asset)
        {
            if (asset != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset);
                return !string.IsNullOrEmpty(assetPath);
            }
            return false;
        }

        private static Action<BuildPlayerOptions> GetBuildPlayerHandler(out bool success)
        {
            var buildPlayerWindowType = typeof(BuildPlayerWindow);
            var buildPlayerHandlerField = buildPlayerWindowType.GetField("buildPlayerHandler", BindingFlags.NonPublic | BindingFlags.Static);
            if (buildPlayerHandlerField != null)
            {
                success = true;
                return buildPlayerHandlerField.GetValue(null) as Action<BuildPlayerOptions>;
            }
            success = false;
            return null;
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            var buildPlayerHandler = GetBuildPlayerHandler(out var success);
            if (!success || buildPlayerHandler != null)
            {
                Debug.LogWarning("TriLib tried to register a 'build player handler', but there was a' build player handler' registered. Aborting.");
                return;
            }
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
        }

        private static void OnBuildPlayer(BuildPlayerOptions buildOptions)
        {
            var removedFromBuild = new Dictionary<string, string>();
            try
            {
                OnPreprocessBuild(removedFromBuild);
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(buildOptions);
            }
            finally
            {
                OnPostprocessBuild(removedFromBuild);
            }
        }

        private static void RemoveAssetFromBuild(Dictionary<string, string> removedFromBuild, Object asset)
        {
            if (asset != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    assetPath = $"{Application.dataPath}/../{assetPath}";
                    if (File.Exists(assetPath))
                    {
                        var tempPath = $"{assetPath}.tmp";
                        File.Move(assetPath, tempPath);
                        removedFromBuild.Add(assetPath, tempPath);
                    }
                }
            }
        }

        private static void RestoreAssets(Dictionary<string, string> removedFromBuild)
        {
            if (removedFromBuild.Count > 0)
            {
                foreach (var kvp in removedFromBuild)
                {
                    if (File.Exists(kvp.Value))
                    {
                        File.Move(kvp.Value, kvp.Key);
                    }
                }
                AssetDatabase.Refresh();
            }
        }
    }
}