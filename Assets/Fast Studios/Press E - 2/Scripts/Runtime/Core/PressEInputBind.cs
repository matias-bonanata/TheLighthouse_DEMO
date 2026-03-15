using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FastStudios
{
    [System.Serializable]
    public class PressEInputBind
    {
        public InputMethod InputMethod = InputMethod.Keyboard;
        public KeyCode Key = KeyCode.E;
        public MouseMethod MouseButton = MouseMethod.Left;

#if ENABLE_INPUT_SYSTEM
        public InputActionReference Action;
#endif

        [HideInInspector] public bool UIButtonDown;
        [HideInInspector] public bool UIButtonHeld;
        [HideInInspector] public bool UIButtonUp;

        [HideInInspector] public bool CanShow = false;

        

        public void ClearUIButtonsFrameState()
        {
            UIButtonDown = false;
            UIButtonUp = false;
        }
    }
}