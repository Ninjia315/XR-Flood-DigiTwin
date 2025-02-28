using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderMenuAnim : MonoBehaviour
{
    public GameObject PanelMenu;
    public RectTransform ToggleButton;

    public void ShowMenu()
    {
        if (PanelMenu != null)
        {
            Animator animator = PanelMenu.GetComponent<Animator>();
            if (animator != null)
            {
                bool isOpen = animator.GetBool("Show");
                animator.SetBool("Show", !isOpen);
                ToggleButton.localEulerAngles = new Vector3(0, 0, isOpen ? 0 : 180);
            }
            else
            {
                Debug.Log("No Animator found");
            }
        }
        else
        {
            Debug.Log("No PanelMenu found");
        }
    }
}
