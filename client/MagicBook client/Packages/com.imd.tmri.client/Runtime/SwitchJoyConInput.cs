
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#if UNITY_IOS
using UnityEngine.InputSystem.iOS;
#elif UNITY_ANDROID
using UnityEngine.InputSystem.Android;
#endif

namespace TMRI.Client
{
    public class SwitchJoyConInput : MonoBehaviour
    {
        public static SwitchJoyConInput Instance;

        public UnityEngine.Vector2 AxisDir { get; private set; }
        public ButtonControl Left { get; private set; }
        public ButtonControl Right { get; private set; }
        public ButtonControl Up { get; private set; }
        public ButtonControl Down { get; private set; }

        public InputDevice device { get; private set; }
        AxisControl x;
        AxisControl y;

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (device == null)
            {
#if UNITY_EDITOR
                device = InputSystem.devices.FirstOrDefault(d => d.valueType == typeof(Joystick));

                if (device == null)
                {
                    enabled = false;
                    return;
                }

                x = device.GetChildControl<AxisControl>("hat/x");
                y = device.GetChildControl<AxisControl>("hat/y");
                Left = device.GetChildControl<ButtonControl>("trigger");
                Right = device.GetChildControl<ButtonControl>("button4");
                Up = device.GetChildControl<ButtonControl>("button3");
                Down = device.GetChildControl<ButtonControl>("button2");

#elif UNITY_IOS
            device = InputSystem.GetDevice(typeof(iOSGameController));
            x = device.GetChildControl<AxisControl>("dpad/x");
            y = device.GetChildControl<AxisControl>("dpad/y");
            Left = device.GetChildControl<ButtonControl>("buttonSouth");
            Right = device.GetChildControl<ButtonControl>("buttonNorth");
            Up = device.GetChildControl<ButtonControl>("buttonEast");
            Down = device.GetChildControl<ButtonControl>("buttonWest");
#elif UNITY_ANDROID
            device = InputSystem.GetDevice(typeof(AndroidGamepadWithDpadAxes));
            x = device.GetChildControl<AxisControl>("dpad/x");
            y = device.GetChildControl<AxisControl>("dpad/y");
            Left = device.GetChildControl<ButtonControl>("buttonSouth");
            Right = device.GetChildControl<ButtonControl>("buttonWest");
            Up = device.GetChildControl<ButtonControl>("buttonNorth");
            Down = device.GetChildControl<ButtonControl>("buttonEast");
            //var shoulder = device.GetChildControl<ButtonControl>("rightStickPress");
#endif
            }

            AxisDir = new UnityEngine.Vector2(y.ReadValue(), -x.ReadValue());

#if UNITY_ANDROID || UNITY_IOS
            if (device.displayName.Contains("(R)"))
                AxisDir = UnityEngine.Vector2.Scale(AxisDir, new UnityEngine.Vector2(-1f, -1f));
#endif
        }
    }
}