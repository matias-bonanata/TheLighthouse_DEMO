using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FastStudios.Demo
{
    public class FirstPersonCameraOffline : MonoBehaviour
    {
        public bool allowedToLook = true;

        [Range(0.01f, 100)]
        public float sensitivity;

        public Transform orientation;

        float x;
        float y;

#if ENABLE_INPUT_SYSTEM
        [SerializeField] InputActionReference lookAction;
        [SerializeField] float mouseDeltaScale = 0.02f;
        [SerializeField] float gamepadLookMultiplier = 4f;

        [SerializeField, Range(0f, 40f)] float mouseSmooth = 18f;
        Vector2 _mouseSmoothValue;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        [SerializeField] bool preferLegacyMouseWhenAvailable = true;
#endif

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

#if ENABLE_INPUT_SYSTEM
        private void OnEnable()
        {
            if (lookAction != null && lookAction.action != null)
                lookAction.action.Enable();
        }

        private void OnDisable()
        {
            if (lookAction != null && lookAction.action != null)
                lookAction.action.Disable();
        }
#endif

        private void Update()
        {
            if (allowedToLook == false) return;

            Vector2 lookMouse = Vector2.zero;
            Vector2 lookStick = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (lookAction != null && lookAction.action != null)
            {
                lookStick = lookAction.action.ReadValue<Vector2>() * gamepadLookMultiplier;
            }
            else
            {
                if (Gamepad.current != null) lookStick = Gamepad.current.rightStick.ReadValue() * gamepadLookMultiplier;
            }

            if (Mouse.current != null)
            {
                Vector2 rawMouse = Mouse.current.delta.ReadValue() * mouseDeltaScale;

                if (mouseSmooth > 0f)
                {
                    float t = 1f - Mathf.Exp(-mouseSmooth * Time.unscaledDeltaTime);
                    _mouseSmoothValue = Vector2.Lerp(_mouseSmoothValue, rawMouse, t);
                    lookMouse = _mouseSmoothValue;
                }
                else lookMouse = rawMouse;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (preferLegacyMouseWhenAvailable)
            {
                float mx = Input.GetAxisRaw("Mouse X");
                float my = Input.GetAxisRaw("Mouse Y");
                lookMouse = new Vector2(mx, my);
                if (mx != 0 || my != 0) lookStick = Vector2.zero;
            }
#endif
            Vector2 look = lookMouse + lookStick;

            float mouseX = look.x * Time.deltaTime * sensitivity * 20f;
            float mouseY = look.y * Time.deltaTime * sensitivity * 20f;

            y += mouseX;
            x -= mouseY;

            x = Mathf.Clamp(x, -90f, 90f);

            transform.rotation = Quaternion.Euler(x, y, 0);
            orientation.rotation = Quaternion.Euler(0, y, 0);

            if (Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;
            if (Cursor.visible != false) Cursor.visible = false;
        }
    }
}
