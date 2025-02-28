using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverviewUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text timeText;
    public TMP_Text firesText;
    public Button replayButton;
    public UnityEvent OnReplayAction;

    public static float timeLeft;
    public static int firesExtinguished;
    public static bool show;

    bool canCheckInput;
    GameObject child;

    private void OnEnable()
    {
        child = transform.childCount > 0 ? transform.GetChild(0).gameObject : gameObject;
        SetOverview(timeLeft, firesExtinguished);
        StartCoroutine(DelayedInput());
    }

    public void SetOverview(float secondsLeft, int firesCount)
    {
        titleText.text = secondsLeft <= 0f ? $"Time's up!" : $"Finish!";
        timeText.text = $"Time left: {secondsLeft} seconds";
        firesText.text = $"Fires extinguished: {firesCount}";

        replayButton.onClick.AddListener(ReplayLoadScene);
    }

    private IEnumerator DelayedInput()
    {
        canCheckInput = false;
        yield return new WaitForSeconds(3f);
        canCheckInput = true;
    }

    private void Update()
    {
        if (child.activeSelf != show)
        {
            SetOverview(timeLeft, firesExtinguished);
            child.SetActive(show);
            StartCoroutine(DelayedInput());
        }

        if (!show)
            return;

        if (canCheckInput && (MixedInput.ActionUp || MixedInput.SecondaryActionUp || MixedInput.TertiaryActionUp))
        {
            canCheckInput = false;
            OnReplayAction?.Invoke();
        }
        //ReplayLoadScene();
    }

    private void ReplayLoadScene()
    {
        //SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}
