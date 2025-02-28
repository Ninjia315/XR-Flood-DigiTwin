using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MobfishCardboard
{
    public class SimulateHeadTransform: MonoBehaviour
    {
        [SerializeField]
        private Transform targetTransform;
        [SerializeField]
        [Range(0, 5)]
        private float sensitivity = 1f;

        private void Awake()
        {
            if (targetTransform == null)
                targetTransform = GetComponent<Transform>();

            if (!Application.isEditor)
                enabled = false;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            var cursorLock = false;
            if (GetKeyRotate())
            {
                cursorLock = true;
                Vector3 currentEulerAngle = transform.localEulerAngles;
                float targetRotX = currentEulerAngle.x - GetMouseY() * sensitivity;

                if (targetRotX < 90 || targetRotX > -90)
                {
                    currentEulerAngle.x = targetRotX;
                }
                float targetRotY = currentEulerAngle.y + GetMouseX() * sensitivity;

                if (targetRotY > 360)
                    targetRotY -= 360;
                else if (targetRotY < -360)
                    targetRotY += 360;
                currentEulerAngle.y = targetRotY;

                transform.localEulerAngles = currentEulerAngle;
                transform.Translate(0, 0, Mouse.current.scroll.value.y * 0.001f);
            }
            else if (GetKeyTilt())
            {
                cursorLock = true;
                Vector3 currentEulerAngle = transform.localEulerAngles;
                float targetRotZ = currentEulerAngle.z - GetMouseY();

                currentEulerAngle.z = targetRotZ;
                transform.localEulerAngles = currentEulerAngle;
            }

            if(cursorLock)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        #if !UNITY_2019_1_OR_NEWER || ENABLE_LEGACY_INPUT_MANAGER

        private bool GetKeyRotate()
        {
            return Input.GetKey(KeyCode.LeftAlt);
        }

        private bool GetKeyTilt()
        {
            return Input.GetKey(KeyCode.LeftControl);
        }

        private float GetMouseX()
        {
            return Input.GetAxis("Mouse X");
        }

        private float GetMouseY()
        {
            return Input.GetAxis("Mouse Y");
        }

        #else
		private bool GetKeyRotate()
		{
			return Keyboard.current.altKey.isPressed;
		}

		private bool GetKeyTilt()
		{
			return Keyboard.current.ctrlKey.isPressed;
		}

		private float GetMouseX()
		{
			return Mouse.current.delta.x.ReadValue();
		}

		private float GetMouseY()
		{
			return Mouse.current.delta.y.ReadValue();
		}

        #endif
    }
}