using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;

[CustomPropertyDrawer(typeof(UniqueIdentifierAttribute))]
public class UniqueIdentifierDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
    {
        // Generate a unique ID, defaults to an empty string if nothing has been serialized yet
        if (prop.stringValue == "" || GUI.Button(new Rect(position.x+200, position.y, 50, 16), "Gen"))
        {
            Guid guid = Guid.NewGuid();
            prop.stringValue = guid.ToString();
        }

        // Place a label so it can't be edited by accident
        Rect textFieldPosition = position;
        textFieldPosition.height = 16;
        DrawLabelField(textFieldPosition, prop, label);
    }

    void DrawLabelField(Rect position, SerializedProperty prop, GUIContent label)
    {
        EditorGUI.LabelField(position, label, new GUIContent(prop.stringValue));
    }
}
#endif
public class UniqueIdentifierAttribute : PropertyAttribute { }