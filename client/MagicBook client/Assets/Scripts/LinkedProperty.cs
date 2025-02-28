using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using TMRI.Client;
using UnityEngine;


[RequireComponent(typeof(TMP_Text))]
public class LinkedProperty : MonoBehaviour
{
    public MonoBehaviour PropertyContainer;
    public string PropertyName;
    public float UpdateIntervalSeconds = 0.5f;
    public LinkedType PropertyOrField;

    private TMP_Text text;

    public enum LinkedType
    {
        Property,
        Field
    }

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TMP_Text>();
        InvokeRepeating(nameof(UpdateTextFromProperty), 1f, UpdateIntervalSeconds);
    }

    void UpdateTextFromProperty()
    {
        if (PropertyContainer == null)
            PropertyContainer = FindObjectOfType<TMRIState>();

        if (PropertyOrField == LinkedType.Field)
        {
            var field = PropertyContainer.GetType().GetField(PropertyName);
            if (field != null)
            {
                text.text = field.GetValue(PropertyContainer).ToString();
            }
        }
        else if(PropertyOrField == LinkedType.Property)
        {
            var prop = PropertyContainer.GetType().GetProperty(PropertyName);
            if (prop != null)
            {
                text.text = prop.GetValue(PropertyContainer).ToString();
            }
        }
    }
}
