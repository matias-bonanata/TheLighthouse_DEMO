using UnityEngine;
using System;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FastStudios
{
    public static class InputHandler
    {

        public static PressEInputBind ResolveBind(PressEInputBind normalBind, bool isToOverride, PressEInputBind overrideBind)
        {
            if (isToOverride) return overrideBind;

            return normalBind;
        }
        
        static void GetRuntimeInputSettings(out InputSystemEnum system, out bool captureUIButtons, out bool onlyUIButtons)
        {
            var m = InteractionManager.singleton;

            if (m != null)
            {
                system = m.inputSystem;
                captureUIButtons = m.CaptureUIButtonsInteraction;
                onlyUIButtons = m.OnlyGetUIButtonsInput;
                return;
            }

            // No manager in scene
            system = InteractionManager.ProjectInputSystem;
            captureUIButtons = InteractionManager.ProjectCaptureUIButtons;
            onlyUIButtons = InteractionManager.ProjectOnlyGetUIButtonsInput;
        }

        static bool LegacyDown(InputMethod method, KeyCode key, MouseMethod mouse)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (method == InputMethod.Keyboard) return Input.GetKeyDown(key);
            if (method == InputMethod.Mouse) return Input.GetMouseButtonDown((int)mouse);
#endif
            return false;
        }

        static bool LegacyHeld(InputMethod method, KeyCode key, MouseMethod mouse)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (method == InputMethod.Keyboard) return Input.GetKey(key);
            if (method == InputMethod.Mouse) return Input.GetMouseButton((int)mouse);
#endif
            return false;
        }

        static bool LegacyUp(InputMethod method, KeyCode key, MouseMethod mouse)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (method == InputMethod.Keyboard) return Input.GetKeyUp(key);
            if (method == InputMethod.Mouse) return Input.GetMouseButtonUp((int)mouse);
#endif
            return false;
        }

        static bool ModernDown(InputMethod method, KeyCode key, MouseMethod mouse)
        {
#if ENABLE_INPUT_SYSTEM
            if (method == InputMethod.Mouse)
            {
                var m = Mouse.current;
                if (m == null) return false;

                return mouse switch
                {
                    MouseMethod.Left => m.leftButton.wasPressedThisFrame,
                    MouseMethod.Right => m.rightButton.wasPressedThisFrame,
                    MouseMethod.Middle => m.middleButton.wasPressedThisFrame,
                    _ => false
                };
            }

            if (method == InputMethod.Keyboard)
            {
                var kb = Keyboard.current;
                if (kb == null) return false;

                if (!TryMapKey(key, out var k)) return false;
                return kb[k].wasPressedThisFrame;
            }
#endif
            return false;
        }

        static bool ModernHeld(InputMethod method, KeyCode key, MouseMethod mouse)
        {
#if ENABLE_INPUT_SYSTEM
            if (method == InputMethod.Mouse)
            {
                var m = Mouse.current;
                if (m == null) return false;

                return mouse switch
                {
                    MouseMethod.Left => m.leftButton.isPressed,
                    MouseMethod.Right => m.rightButton.isPressed,
                    MouseMethod.Middle => m.middleButton.isPressed,
                    _ => false
                };
            }

            if (method == InputMethod.Keyboard)
            {
                var kb = Keyboard.current;
                if (kb == null) return false;

                if (!TryMapKey(key, out var k)) return false;
                return kb[k].isPressed;
            }
#endif
            return false;
        }

        static bool ModernUp(InputMethod method, KeyCode key, MouseMethod mouse)
        {
#if ENABLE_INPUT_SYSTEM
            if (method == InputMethod.Mouse)
            {
                var m = Mouse.current;
                if (m == null) return false;

                return mouse switch
                {
                    MouseMethod.Left => m.leftButton.wasReleasedThisFrame,
                    MouseMethod.Right => m.rightButton.wasReleasedThisFrame,
                    MouseMethod.Middle => m.middleButton.wasReleasedThisFrame,
                    _ => false
                };
            }

            if (method == InputMethod.Keyboard)
            {
                var kb = Keyboard.current;
                if (kb == null) return false;

                if (!TryMapKey(key, out var k)) return false;
                return kb[k].wasReleasedThisFrame;
            }
#endif
            return false;
        }

        public static bool GeneralInputDown(PressEInputBind normalBind) => GeneralInputDown(normalBind, false, null);
        public static bool GeneralInput(PressEInputBind normalBind) => GeneralInput(normalBind, false, null);
        public static bool GeneralInputUp(PressEInputBind normalBind) => GeneralInputUp(normalBind, false, null);

        public static bool GeneralInputDown(PressEInputBind normalBind, bool isToOverride, PressEInputBind overrideBind)
        {
            if (normalBind == null) return false;

            GetRuntimeInputSettings(out var sys, out var captureUI, out var onlyUI);

            bool ui = captureUI && normalBind.UIButtonDown;

            if (onlyUI) return ui;

            var physicalBind = (isToOverride && overrideBind != null) ? overrideBind : normalBind;

            bool legacy = LegacyDown(physicalBind.InputMethod, physicalBind.Key, physicalBind.MouseButton);

            bool modernKM = ModernDown(physicalBind.InputMethod, physicalBind.Key, physicalBind.MouseButton);

            bool action = false;
#if ENABLE_INPUT_SYSTEM
            action = ActionDown(physicalBind.Action);
#endif
            bool physical =
                (sys == InputSystemEnum.Old) ? legacy :
                (sys == InputSystemEnum.New) ? action :
                (legacy || action || modernKM);

            return physical || ui;
        }

        public static bool GeneralInput(PressEInputBind normalBind, bool isToOverride, PressEInputBind overrideBind)
        {
            if (normalBind == null) return false;

            GetRuntimeInputSettings(out var sys, out var captureUI, out var onlyUI);

            bool ui = captureUI && normalBind.UIButtonHeld;

            if (onlyUI) return ui;

            var physicalBind = (isToOverride && overrideBind != null) ? overrideBind : normalBind;

            bool legacy = LegacyHeld(physicalBind.InputMethod, physicalBind.Key, physicalBind.MouseButton);

            bool modern = ModernHeld(physicalBind.InputMethod, physicalBind.Key, physicalBind.MouseButton);
#if ENABLE_INPUT_SYSTEM
            modern |= ActionHeld(physicalBind.Action);
#endif

            bool physical =
                (sys == InputSystemEnum.Old) ? legacy :
                (sys == InputSystemEnum.New) ? modern :
                (legacy || modern);

            return physical || ui;
        }

        public static bool GeneralInputUp(PressEInputBind normalBind, bool isToOverride, PressEInputBind overrideBind)
        {
            if (normalBind == null) return false;

            GetRuntimeInputSettings(out var sys, out var captureUI, out var onlyUI);

            bool ui = captureUI && normalBind.UIButtonUp;

            if (onlyUI) return ui;

            var physicalBind = (isToOverride && overrideBind != null) ? overrideBind : normalBind;

            bool legacy = LegacyUp(physicalBind.InputMethod, physicalBind.Key, physicalBind.MouseButton);

            bool modern = ModernUp(physicalBind.InputMethod, physicalBind.Key, physicalBind.MouseButton);
#if ENABLE_INPUT_SYSTEM
            modern |= ActionUp(physicalBind.Action);
#endif

            bool physical =
                (sys == InputSystemEnum.Old) ? legacy :
                (sys == InputSystemEnum.New) ? modern :
                (legacy || modern);

            return physical || ui;
        }

        public static float GeneralScrollDelta(InputSystemEnum system)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            float legacy = Input.GetAxisRaw("Mouse ScrollWheel");
#else
            float legacy = 0f;
#endif

#if ENABLE_INPUT_SYSTEM
            float modern = 0f;

            if (Mouse.current != null)
            {
                float raw = Mouse.current.scroll.ReadValue().y;
                modern += Mathf.Abs(raw) > 10f ? raw / 120f : raw;
            }

            modern += GetGamepadScrollStep(step: 1f);
#else
            float modern = 0f;
#endif

            if (system == InputSystemEnum.Old) return legacy;
            if (system == InputSystemEnum.New) return modern;
            return modern != 0f ? modern : legacy;
        }

#if ENABLE_INPUT_SYSTEM

        static int _scrollHeldDir = 0;
        static float _scrollNextRepeatTime = 0f;

        static float GetGamepadScrollStep(float step = 1f, float initialDelay = 0.25f, float repeatRate = 0.06f)
        {
            var gp = Gamepad.current;
            if (gp == null) return 0f;

            bool upDownThisFrame = gp.dpad.up.wasPressedThisFrame;
            bool downDownThisFrame = gp.dpad.down.wasPressedThisFrame;

            if (upDownThisFrame || downDownThisFrame)
            {
                _scrollHeldDir = upDownThisFrame ? 1 : -1;
                _scrollNextRepeatTime = Time.unscaledTime + initialDelay;
                return _scrollHeldDir * step;
            }

            bool upHeld = gp.dpad.up.isPressed;
            bool downHeld = gp.dpad.down.isPressed;

            if (upHeld || downHeld)
            {
                int dir = upHeld ? 1 : -1;

                if (dir != _scrollHeldDir)
                {
                    _scrollHeldDir = dir;
                    _scrollNextRepeatTime = Time.unscaledTime + initialDelay;
                }

                if (Time.unscaledTime >= _scrollNextRepeatTime)
                {
                    _scrollNextRepeatTime = Time.unscaledTime + repeatRate;
                    return dir * step;
                }

                return 0f;
            }

            _scrollHeldDir = 0;
            return 0f;
        }

        static int _modernMouseCacheFrame = -1;
        static Vector2 _modernMousePixelsSmoothed;
        static Vector2 _modernMousePixelsVel;
        static bool TryMapKey(KeyCode kc, out UnityEngine.InputSystem.Key k)
        {
            if (kc >= KeyCode.A && kc <= KeyCode.Z)
            {
                k = (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.A + (kc - KeyCode.A));
                return true;
            }

            if (kc >= KeyCode.Alpha0 && kc <= KeyCode.Alpha9)
            {
                k = (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.Digit0 + (kc - KeyCode.Alpha0));
                return true;
            }

            if (kc >= KeyCode.Keypad0 && kc <= KeyCode.Keypad9)
            {
                k = (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.Numpad0 + (kc - KeyCode.Keypad0));
                return true;
            }

            if (kc >= KeyCode.F1 && kc <= KeyCode.F15)
            {
                k = (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.F1 + (kc - KeyCode.F1));
                return true;
            }

            k = kc switch
            {
                KeyCode.Space => UnityEngine.InputSystem.Key.Space,
                KeyCode.Escape => UnityEngine.InputSystem.Key.Escape,
                KeyCode.Return => UnityEngine.InputSystem.Key.Enter,
                KeyCode.Backspace => UnityEngine.InputSystem.Key.Backspace,
                KeyCode.Tab => UnityEngine.InputSystem.Key.Tab,

                KeyCode.LeftShift => UnityEngine.InputSystem.Key.LeftShift,
                KeyCode.RightShift => UnityEngine.InputSystem.Key.RightShift,
                KeyCode.LeftControl => UnityEngine.InputSystem.Key.LeftCtrl,
                KeyCode.RightControl => UnityEngine.InputSystem.Key.RightCtrl,
                KeyCode.LeftAlt => UnityEngine.InputSystem.Key.LeftAlt,
                KeyCode.RightAlt => UnityEngine.InputSystem.Key.RightAlt,

                KeyCode.UpArrow => UnityEngine.InputSystem.Key.UpArrow,
                KeyCode.DownArrow => UnityEngine.InputSystem.Key.DownArrow,
                KeyCode.LeftArrow => UnityEngine.InputSystem.Key.LeftArrow,
                KeyCode.RightArrow => UnityEngine.InputSystem.Key.RightArrow,

                _ => UnityEngine.InputSystem.Key.None
            };

            return k != UnityEngine.InputSystem.Key.None;
        }

        static Vector2 GetModernMousePixelsSmoothed(float smoothTime)
        {
            Vector2 raw = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

            if (smoothTime <= 0f)
                return raw;

            if (_modernMouseCacheFrame != Time.frameCount)
            {
                _modernMouseCacheFrame = Time.frameCount;

                _modernMousePixelsSmoothed = Vector2.SmoothDamp(
                    _modernMousePixelsSmoothed,
                    raw,
                    ref _modernMousePixelsVel,
                    smoothTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime
                );
            }

            return _modernMousePixelsSmoothed;
        }

        static Vector2 GetGamepadRightStick_NoiseFiltered(float deadZone = 0.15f)
        {
            var gp = Gamepad.current;
            if (gp == null) return Vector2.zero;

            Vector2 stick = gp.rightStick.ReadValue();

            float deadSqr = deadZone * deadZone;
            if (stick.sqrMagnitude <= deadSqr) return Vector2.zero;

            float mag = stick.magnitude;
            if (mag <= 0.0001f) return Vector2.zero;

            Vector2 dir = stick / mag;
            float t = (mag - deadZone) / (1f - deadZone);
            return dir * Mathf.Clamp01(t);
        }
#endif

        public static Vector2 GeneralMouseDelta(InputSystemEnum system, float newInputScale = 0.02f, float gamepadStickScale = 0.5f, float modernMouseSmoothTime = 0.025f)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            Vector2 legacyMouse = new(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#else
            Vector2 legacyMouse = Vector2.zero;
#endif

#if ENABLE_INPUT_SYSTEM
            Vector2 modernMouse = GetModernMousePixelsSmoothed(modernMouseSmoothTime) * newInputScale;

            Vector2 stick = GetGamepadRightStick_NoiseFiltered() * gamepadStickScale;

            Vector2 modern = modernMouse + stick;
#else
            Vector2 modern = Vector2.zero;
            Vector2 stick = Vector2.zero;
#endif

            if (system == InputSystemEnum.Old) return legacyMouse;

            if (system == InputSystemEnum.New)
            {
#if ENABLE_INPUT_SYSTEM
                if (legacyMouse != Vector2.zero) return legacyMouse + stick;
#endif
                return modern;
            }

#if ENABLE_INPUT_SYSTEM
            if (legacyMouse != Vector2.zero) return legacyMouse + stick;
#endif
            return modern != Vector2.zero ? modern : legacyMouse;
        }

        public static bool TryGetMouseScreenPosition(out Vector2 mp)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                mp = Mouse.current.position.ReadValue();
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            mp = Input.mousePosition;
            return true;
#else
            mp = default;
            return false;
#endif
        }


#if ENABLE_INPUT_SYSTEM
        static bool ActionDown(InputActionReference a)
        {
            return a != null && a.action != null && a.action.WasPressedThisFrame();
        }

        static bool ActionHeld(InputActionReference a)
        {
            return a != null && a.action != null && a.action.IsPressed();
        }

        static bool ActionUp(InputActionReference a)
        {
            return a != null && a.action != null && a.action.WasReleasedThisFrame();
        }
#endif
        public static string GetKeyOrMouse(PressEInputBind normalBind)
        {
            if (normalBind == null) return default;

            return GetKeyOrMouse(normalBind.InputMethod, normalBind.Key, normalBind.MouseButton);
        }

        public static string GetKeyOrMouse(PressEInputBind normalBind, bool isToOverride, PressEInputBind overrideBind)
        {
            var bind = (isToOverride && overrideBind != null) ? overrideBind : normalBind;
            if (bind == null) return default;

            return GetKeyOrMouse(bind.InputMethod, bind.Key, bind.MouseButton);
        }

        public static string GetKeyOrMouse(InputMethod inputMethod, KeyCode keyCode, MouseMethod mouseButton)
        {
            if (inputMethod == InputMethod.Keyboard) return keyCode.ToString();
            else if (inputMethod == InputMethod.Mouse) return mouseButton.ToString();

            return default;
        }

    }
}