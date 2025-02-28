using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SetSliderDynamic : MonoBehaviour
{
    public TMP_InputField textInput;

    Slider s;
    // Start is called before the first frame update
    void Start()
    {
        s = GetComponent<Slider>();
        s.onValueChanged.AddListener(v => textInput.text = v.ToString("F0"));
        textInput.onEndEdit.AddListener(SetSliderValue);
    }

    public void SetSliderMaxValue(float value)
    {
        s.maxValue = value;
    }

    public void SetSliderMaxValue(string value)
    {
        if (float.TryParse(value, out float result))
            SetSliderMaxValue(result);
    }

    public void SetSliderValue(string value)
    {
        if (float.TryParse(value, out float result))
            s.value = result;
    }

    public void SetText(float value)
    {
        if(s.value != value)
            textInput.text = value.ToString("F0");
    }
}
