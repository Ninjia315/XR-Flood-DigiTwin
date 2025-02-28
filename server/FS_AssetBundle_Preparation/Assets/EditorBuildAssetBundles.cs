using System.IO;
using UnityEditor;
using UnityEngine;

public class EditorBuildAssetBundles
{
    //[MenuItem("Assets/Build AssetBundles")]
    //static void BuildAllAssetBundles()
    //{
    //    Debug.Log("Starting all AssetBundle build...");
    //    FolderAndBuild(BuildTarget.StandaloneOSX);
    //    FolderAndBuild(BuildTarget.StandaloneWindows);
    //    FolderAndBuild(BuildTarget.Android);
    //    FolderAndBuild(BuildTarget.iOS);
    //    FolderAndBuild(BuildTarget.WSAPlayer);
    //    FolderAndBuild(BuildTarget.VisionOS);
    //    Debug.Log("Finished all AssetBundle build.");
    //}

    //static void FolderAndBuild(BuildTarget target, BuildAssetBundleOptions options = BuildAssetBundleOptions.None)
    //{
    //    var folderPath = Path.Combine(Application.dataPath, "../AssetBundles/"+target.ToString());
    //    //Debug.Log(folderPath);
    //    Directory.CreateDirectory(folderPath);
    //    BuildPipeline.BuildAssetBundles($"AssetBundles/{target}", options, target);
    //    Debug.Log($"Finished {target}!");
    //}
}