using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace TMRI.Client
{
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

        public static bool ActionHeld => instance.action.IsPressed();

        public static bool ActionUp => instance.action.WasReleasedThisFrame();

        public static Vector2 AxisDir => new Vector2(instance.horizontalAxis.ReadValue<float>(), -instance.verticalAxis.ReadValue<float>());

        public static bool SecondaryActionDown => instance.secondaryAction.WasPressedThisFrame();

        public static bool SecondaryActionHeld => instance.secondaryAction.IsPressed();

        public static bool SecondaryActionUp => instance.secondaryAction.WasReleasedThisFrame();

        public static bool TertiaryActionUp => instance.tertiaryAction.WasReleasedThisFrame();

    }
}//namespace TMRI.Client