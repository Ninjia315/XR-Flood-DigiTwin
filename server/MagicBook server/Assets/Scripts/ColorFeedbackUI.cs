using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static WebsocketServerWithGUI;

public class ColorFeedbackUI : MonoBehaviour
{
    public WebsocketServerWithGUI serverGUI;
    public enum ImageUIColor {LightGreen, Blue, Muddy}
    public ImageUIColor selectedImageColor = ImageUIColor.LightGreen;
    private Image targetImage;
  
    // Start is called before the first frame update
    void Start()
    {
        targetImage = transform.GetComponent<Image>();
    }

    public void ImageColorChangedClicked()
    {
        if (serverGUI.dragMap.transform.childCount == 0)
        {
            //Only change the Image color if there is a map on the dragMap
            return; 
        }

        selectedImageColor++; //once button clicked, change to the next color
        if ((int)selectedImageColor == (Enum.GetValues(typeof(ImageUIColor)).Length))
        {
            selectedImageColor = ImageUIColor.LightGreen;
        }
        ChangeImageColor(selectedImageColor);
        //Debug.Log("Button color changed to: " + selectedImageColor);
    }

    public void ChangeImageColor(ImageUIColor color)
    {
        switch (color)
        {
            case ImageUIColor.LightGreen:
                targetImage.color = Color.HSVToRGB(38f / 360f, 13f / 100f, 65f / 100f);
                break;
            case ImageUIColor.Blue:
                targetImage.color = Color.HSVToRGB(191f / 360f, 33f / 100f, 89f / 100f);
                break;
            case ImageUIColor.Muddy:
                targetImage.color = Color.HSVToRGB(38f / 360f, 69f / 100f, 88f / 100f);
                break;
        }
    }
}
