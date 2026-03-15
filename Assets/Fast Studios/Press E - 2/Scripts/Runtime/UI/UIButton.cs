using UnityEngine;
using UnityEngine.EventSystems;

namespace FastStudios
{
    public enum UIButtonType
    {
        Interact = 0,
        GrabDrop = 1,
        GrabPlace = 2,
        GrabTurnLeft = 3,
        GrabTurnRight = 4,
        GrabThrow = 5,
        GrabRotate = 6,
        InspectionRotate = 7,
        InspectionImage = 8,
        InspectionText = 9,
    }
    
    [RequireComponent(typeof(CanvasGroup))]
    public class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public UIButtonType Type = UIButtonType.Interact;
        public bool DontShowWhenDontNeed = true;
        InteractionManager manager;
        CanvasGroup canvasGroup;

        int lastPointerUpFrame = -1;

        int activePointerId = int.MinValue;

        void Awake()
        {
            manager = InteractionManager.singleton;
            canvasGroup = GetComponent<CanvasGroup>();
        }

        void OnEnable()
        {
            if (manager == null) manager = InteractionManager.singleton;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        void OnDisable()
        {
            if (manager != null)
            {
                // if (manager.InteractUIButtonHeld) manager.InteractUIButtonUp = true;
                if (GetHeld()) ResolveUp(true);
                // manager.InteractUIButtonHeld = false;
                ResolveHeld(false);
            }

            activePointerId = int.MinValue;
            lastPointerUpFrame = -1;
        }

        void Update()
        {
            if (manager.CaptureUIButtonsInteraction == false) return;
            
            if (DontShowWhenDontNeed)
            {
                if (CanShow())
                {
                    canvasGroup.alpha = 1;
                    canvasGroup.interactable = true;
                }
                else
                {
                    canvasGroup.alpha = 0;
                    canvasGroup.interactable = false;
                }
            }
        }
        
        [HideInInspector]
        public void OnPointerDown(PointerEventData eventData)
        {
            if (manager == null) manager = InteractionManager.singleton;
            if (manager == null) return;
            if (manager.CaptureUIButtonsInteraction == false) return;

            if (activePointerId != int.MinValue) return;
            activePointerId = eventData.pointerId;

            // manager.InteractUIButtonDown = true;
            // manager.InteractUIButtonHeld = true;
            ResolveDown(true);
            ResolveHeld(true);
        }

        [HideInInspector]
        public void OnPointerUp(PointerEventData eventData)
        {
            if (manager == null) manager = InteractionManager.singleton;
            if (manager == null) return;
            if (manager.CaptureUIButtonsInteraction == false) return;

            if (activePointerId != int.MinValue && eventData.pointerId != activePointerId) return;

            activePointerId = int.MinValue;

            // manager.InteractUIButtonUp = true;
            // manager.InteractUIButtonHeld = false;
            ResolveUp(true);
            ResolveHeld(false);

            lastPointerUpFrame = Time.frameCount;
        }

        public void Interact()
        {
            if (Time.frameCount == lastPointerUpFrame) return;

            if (manager == null) manager = InteractionManager.singleton;
            if (manager == null) return;
            if (manager.CaptureUIButtonsInteraction == false) return;

            // manager.InteractUIButtonDown = true;
            // manager.InteractUIButtonUp = true;
            ResolveDown(true);
            ResolveUp(true);
        }

        public void ResolveDown(bool ToSet)
        {
            if (manager == null) manager = InteractionManager.singleton;

            if (manager.CaptureUIButtonsInteraction == false) return;

            switch (Type)
            {
                case UIButtonType.Interact:
                    manager.Interaction.UIButtonDown = ToSet;
                    break;

                case UIButtonType.GrabDrop:
                    manager.GrabDrop.UIButtonDown = ToSet;
                    break;

                case UIButtonType.GrabPlace:
                    manager.GrabPlace.UIButtonDown = ToSet;
                    break;

                case UIButtonType.GrabTurnLeft:
                    manager.GrabTurnLeft.UIButtonDown = ToSet;
                    break;

                case UIButtonType.GrabTurnRight:
                    manager.GrabTurnRight.UIButtonDown = ToSet;
                    break;

                case UIButtonType.GrabThrow:
                    manager.GrabThrow.UIButtonDown = ToSet;
                    break;

                case UIButtonType.GrabRotate:
                    manager.GrabRotation.UIButtonDown = ToSet;
                    break;

                case UIButtonType.InspectionRotate:
                    manager.InspectionRotation.UIButtonDown = ToSet;
                    break;

                case UIButtonType.InspectionImage:
                    manager.InspectionDetailsImage.UIButtonDown = ToSet;
                    break;

                case UIButtonType.InspectionText:
                    manager.InspectionDetailsText.UIButtonDown = ToSet;
                    break;
            }
        }

        public void ResolveHeld(bool ToSet)
        {
            if (manager == null) manager = InteractionManager.singleton;

            if (manager.CaptureUIButtonsInteraction == false) return;

            switch (Type)
            {
                case UIButtonType.Interact:
                    manager.Interaction.UIButtonHeld = ToSet;
                    break;

                case UIButtonType.GrabDrop:
                    manager.GrabDrop.UIButtonHeld = ToSet;
                    break;

                case UIButtonType.GrabPlace:
                    manager.GrabPlace.UIButtonHeld = ToSet;
                    break;

                case UIButtonType.GrabTurnLeft:
                    manager.GrabTurnLeft.UIButtonHeld = ToSet;
                    break;

                case UIButtonType.GrabTurnRight:
                    manager.GrabTurnRight.UIButtonHeld = ToSet;
                    break;

                case UIButtonType.GrabThrow:
                    manager.GrabThrow.UIButtonHeld = ToSet;
                    break;

                case UIButtonType.GrabRotate:
                    manager.GrabRotation.UIButtonHeld = ToSet;
                    break;

                case UIButtonType.InspectionRotate:
                    manager.InspectionRotation.UIButtonHeld = ToSet;
                    break;

                case UIButtonType.InspectionImage:
                    manager.InspectionDetailsImage.UIButtonHeld = ToSet;
                    break;

                case UIButtonType.InspectionText:
                    manager.InspectionDetailsText.UIButtonHeld = ToSet;
                    break;
            }
        }

        public void ResolveUp(bool ToSet)
        {
            if (manager == null) manager = InteractionManager.singleton;

            if (manager.CaptureUIButtonsInteraction == false) return;

            switch (Type)
            {
                case UIButtonType.Interact:
                    manager.Interaction.UIButtonUp = ToSet;
                    break;

                case UIButtonType.GrabDrop:
                    manager.GrabDrop.UIButtonUp = ToSet;
                    break;

                case UIButtonType.GrabPlace:
                    manager.GrabPlace.UIButtonUp = ToSet;
                    break;

                case UIButtonType.GrabTurnLeft:
                    manager.GrabTurnLeft.UIButtonUp = ToSet;
                    break;

                case UIButtonType.GrabTurnRight:
                    manager.GrabTurnRight.UIButtonUp = ToSet;
                    break;

                case UIButtonType.GrabThrow:
                    manager.GrabThrow.UIButtonUp = ToSet;
                    break;

                case UIButtonType.GrabRotate:
                    manager.GrabRotation.UIButtonUp = ToSet;
                    break;

                case UIButtonType.InspectionRotate:
                    manager.InspectionRotation.UIButtonUp = ToSet;
                    break;

                case UIButtonType.InspectionImage:
                    manager.InspectionDetailsImage.UIButtonUp = ToSet;
                    break;

                case UIButtonType.InspectionText:
                    manager.InspectionDetailsText.UIButtonUp = ToSet;
                    break;
            }
        }

        public bool GetHeld()
        {
            if (manager == null) manager = InteractionManager.singleton;

            if (manager.CaptureUIButtonsInteraction == false) return false;

            switch (Type)
            {
                case UIButtonType.Interact:
                    return manager.Interaction.UIButtonHeld;

                case UIButtonType.GrabDrop:
                    return manager.GrabDrop.UIButtonHeld;

                case UIButtonType.GrabPlace:
                    return manager.GrabPlace.UIButtonHeld;

                case UIButtonType.GrabTurnLeft:
                    return manager.GrabTurnLeft.UIButtonHeld;

                case UIButtonType.GrabTurnRight:
                    return manager.GrabTurnRight.UIButtonHeld;

                case UIButtonType.GrabThrow:
                    return manager.GrabThrow.UIButtonHeld;

                case UIButtonType.GrabRotate:
                    return manager.GrabRotation.UIButtonHeld;

                case UIButtonType.InspectionRotate:
                    return manager.InspectionRotation.UIButtonHeld;

                case UIButtonType.InspectionImage:
                    return manager.InspectionDetailsImage.UIButtonHeld;

                case UIButtonType.InspectionText:
                    return manager.InspectionDetailsText.UIButtonHeld;
            }

            return default;
        }
        
        public bool CanShow()
        {
            if (manager == null) manager = InteractionManager.singleton;

            if (manager.CaptureUIButtonsInteraction == false) return false;

            switch (Type)
            {
                case UIButtonType.Interact:
                    return manager.Interaction.CanShow;

                case UIButtonType.GrabDrop:
                    return manager.GrabDrop.CanShow;

                case UIButtonType.GrabPlace:
                    return manager.GrabPlace.CanShow;

                case UIButtonType.GrabTurnLeft:
                    return manager.GrabTurnLeft.CanShow;

                case UIButtonType.GrabTurnRight:
                    return manager.GrabTurnRight.CanShow;

                case UIButtonType.GrabThrow:
                    return manager.GrabThrow.CanShow;

                case UIButtonType.GrabRotate:
                    return manager.GrabRotation.CanShow;

                case UIButtonType.InspectionRotate:
                    return manager.InspectionRotation.CanShow;

                case UIButtonType.InspectionImage:
                    return manager.InspectionDetailsImage.CanShow;

                case UIButtonType.InspectionText:
                    return manager.InspectionDetailsText.CanShow;
            }

            return default;
        }
    }
}
