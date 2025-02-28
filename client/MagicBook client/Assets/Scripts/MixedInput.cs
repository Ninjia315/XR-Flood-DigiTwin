using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class MixedInput : MonoBehaviour
{
    public InputAction action;
    public InputAction secondaryAction;
    public InputAction tertiaryAction;
    public InputAction horizontalAxis;
    public InputAction verticalAxis;

    private static MixedInput instance;

    private void OnEnable()
    {
        instance = this;
        action.Enable();
        secondaryAction.Enable();
        tertiaryAction.Enable();
        horizontalAxis.Enable();
        verticalAxis.Enable();

        DontDestroyOnLoad(this);
    }

    public static bool HasInstance =>
#if UNITY_EDITOR
        true;
#elif UNITY_MAGICLEAP
        ML6DOFController.Instance != null;
#elif UNITY_IOS || UNITY_ANDROID
        SwitchJoyConInput.Instance != null;
#else
        false;
#endif

    public static bool ActionDown => instance.action.WasPressedThisFrame();
//#if UNITY_EDITOR
//    Mouse.current.leftButton.wasPressedThisFrame;
//#elif UNITY_MAGICLEAP
//    ML6DOFController.Instance.triggerDown;
//#elif UNITY_IOS || UNITY_ANDROID
//    SwitchJoyConInput.Instance == null ? Touchscreen.current.press.wasPressedThisFrame :
//    SwitchJoyConInput.Instance.Left.wasPressedThisFrame;
//#endif

    public static bool ActionHeld => instance.action.IsPressed();
//#if UNITY_EDITOR
//    Mouse.current.leftButton.isPressed && !ActionDown;
//#elif UNITY_MAGICLEAP
//    ML6DOFController.Instance.triggerHeld;
//#elif UNITY_IOS || UNITY_ANDROID
//    SwitchJoyConInput.Instance == null ? (Touchscreen.current.press.isPressed && !ActionDown) :
//    SwitchJoyConInput.Instance.Left.isPressed;
//#endif

    public static bool ActionUp => instance.action.WasReleasedThisFrame();
//#if UNITY_EDITOR
//    Mouse.current.leftButton.wasReleasedThisFrame;
//#elif UNITY_MAGICLEAP
//    ML6DOFController.Instance.triggerUp;
//#elif UNITY_IOS || UNITY_ANDROID
//    SwitchJoyConInput.Instance == null ? Touchscreen.current.press.wasReleasedThisFrame :
//    SwitchJoyConInput.Instance.Left.wasReleasedThisFrame;
//#endif

    public static Vector2 AxisDir => new Vector2(instance.horizontalAxis.ReadValue<float>(), -instance.verticalAxis.ReadValue<float>());
//#if UNITY_EDITOR
//    new (Mouse.current.delta.x.ReadValue(), Mouse.current.delta.y.ReadValue());
//#elif UNITY_MAGICLEAP
//    ML6DOFController.Instance.mTouchpadPosition;
//#elif UNITY_IOS || UNITY_ANDROID
//    SwitchJoyConInput.Instance == null ? Touchscreen.current.delta.ReadValue() :
//    SwitchJoyConInput.Instance.AxisDir;
//#endif

    public static bool SecondaryActionDown => instance.secondaryAction.WasPressedThisFrame();
//#if UNITY_EDITOR
//Mouse.current.rightButton.wasPressedThisFrame;
//#elif UNITY_MAGICLEAP
//    ML6DOFController.Instance.bumperDown;
//#elif UNITY_IOS || UNITY_ANDROID
//    SwitchJoyConInput.Instance == null ? Touchscreen.current.press.isPressed :
//    SwitchJoyConInput.Instance.Right.wasPressedThisFrame;
//#endif

    public static bool SecondaryActionHeld => instance.secondaryAction.IsPressed();
//#if UNITY_EDITOR
//Mouse.current.rightButton.isPressed && !SecondaryActionDown;
//#elif UNITY_MAGICLEAP
//    ML6DOFController.Instance.bumperHeld;
//#elif UNITY_IOS || UNITY_ANDROID
//    SwitchJoyConInput.Instance == null ? (Touchscreen.current.press.isPressed && !SecondaryActionDown) :
//    SwitchJoyConInput.Instance.Right.isPressed;
//#endif

    public static bool SecondaryActionUp => instance.secondaryAction.WasReleasedThisFrame();
//#if UNITY_EDITOR
//Mouse.current.rightButton.wasReleasedThisFrame;
//#elif UNITY_MAGICLEAP
//    ML6DOFController.Instance.bumperUp;
//#elif UNITY_IOS || UNITY_ANDROID
//    SwitchJoyConInput.Instance == null ? Touchscreen.current.press.wasReleasedThisFrame :
//    SwitchJoyConInput.Instance.Right.wasReleasedThisFrame;
//#endif

    public static bool TertiaryActionUp => instance.tertiaryAction.WasReleasedThisFrame();
//#if UNITY_EDITOR
//Mouse.current.middleButton.wasReleasedThisFrame;
//#elif UNITY_MAGICLEAP
//    ML6DOFController.Instance.menuUp;
//#elif UNITY_IOS || UNITY_ANDROID
//    SwitchJoyConInput.Instance == null ? Touchscreen.current.press.wasReleasedThisFrame :
//    SwitchJoyConInput.Instance.Up.wasReleasedThisFrame;
//#endif
}
