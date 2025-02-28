using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleBasedOnInput : MonoBehaviour
{
    public string DisplayNameContainsText;

    //string cachedName;
    bool shouldBeActive => InputSystem.GetDevice(typeof(Joystick))?.displayName.Contains(DisplayNameContainsText) ?? false;

    // Update is called once per frame
    void Update()
    {
        //if(SwitchJoyConInput.Instance != null && SwitchJoyConInput.Instance.device != null)
        {
            //if (cachedName != SwitchJoyConInput.Instance.device.displayName)
            //{
            //    cachedName = SwitchJoyConInput.Instance.device.displayName;
            //    Debug.Log($"Joycon name is: {cachedName}");
            //}

            //transform.GetChild(0).gameObject.SetActive(SwitchJoyConInput.Instance.device.displayName.Contains(DisplayNameContainsText));
            transform.GetChild(0).gameObject.SetActive(shouldBeActive);
        }
        //else
        {
            //transform.GetChild(0).gameObject.SetActive(false);
        }
        
    }
}
