using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleVisibleButton : MonoBehaviour
{
    public WebsocketServerWithGUI serverGUI;
    public Sprite invisibleIcon;
    private Sprite visibleIcon;
    private Image buttonImage;

    private bool isVisible = true;

    // Start is called before the first frame update
    void Start()
    {
        buttonImage = GetComponent<Image>();
        visibleIcon = buttonImage.sprite;
    }

    public void ToggleIcon()
    {
        if (serverGUI.dragMap.transform.childCount == 0)
        {
            //Only change the visible icon if there is a map on the dragMap
            return;
        }
        isVisible = !isVisible;
        buttonImage.sprite = isVisible ? visibleIcon : invisibleIcon;
    }
}
