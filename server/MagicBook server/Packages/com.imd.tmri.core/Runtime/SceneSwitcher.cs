using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public int SwitchToSceneBuildIndex = -1;
    public LoadSceneMode SwitchToSceneMode = LoadSceneMode.Single;

    public void SwitchScene()
    {
        SceneManager.LoadScene(SwitchToSceneBuildIndex, SwitchToSceneMode);
    }

    public void SwitchScene(int sceneBuildIndex)
    {
        SceneManager.LoadScene(sceneBuildIndex, SwitchToSceneMode);
    }
}
