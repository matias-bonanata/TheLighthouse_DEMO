using UnityEngine;
using System;

namespace FastStudios
{
    public partial class InteractionManager
    {
        #region Hold Privates
        private GameObject holdingObject;
        private Interactable holdingInteractable;
        private bool isHolding;
        private bool hasMaxHoldInteractions;
        private bool oneTimeHold;
        private float holdStartTime;
        private float holdTimer;
        [Range(0, 1)] private float holdPercentage;
        #endregion
        void HandleHold(Interactable interactable)
        {
            PressEInputBind bind = InputHandler.ResolveBind(Interaction, interactable.OverrideInteractionKey, interactable.NewInteraction);
            bool InteractionDown = InputHandler.GeneralInputDown(bind);
            InteractionDown |= bind.UIButtonDown;

            if (hasMaxHoldInteractions && interactable.maxHoldInteractions <= 0 && holdStartTime == 0)
            {
                if (InteractionDown)
                {
                    UseAndMaybeConsumeKeys(interactable);
                    interactable.unityEventToTrigger.Invoke();
                    interactable.interactionTimes += 1;
                }
                else if (interactable.HasAutoInteract && !oneTimeInteraction)
                {
                    UseAndMaybeConsumeKeys(interactable);
                    interactable.unityEventToTrigger.Invoke();
                    interactable.interactionTimes += 1;
                    oneTimeInteraction = true;
                }
            }
            else
            {
                if (InteractionDown)
                {
                    holdingInteractable = interactable;
                    holdingObject = interactable.gameObject;
                    isHolding = true;
                    oneTimeHold = true;
                    holdStartTime = Time.time;
                    holdTimer = interactable.holdTime + holdStartTime - Time.time;
                }
                else if (interactable.HasAutoInteract && holdStartTime == 0 && !oneTimeInteraction)
                {
                    holdingInteractable = interactable;
                    holdingObject = interactable.gameObject;
                    isHolding = true;
                    oneTimeHold = true;
                    oneTimeInteraction = true;
                    holdStartTime = Time.time;
                    holdTimer = interactable.holdTime + holdStartTime - Time.time;
                }

                PressEInputBind interactionBind = InputHandler.ResolveBind(Interaction, interactable.OverrideInteractionKey, interactable.NewInteraction);
                bool interactionUp = InputHandler.GeneralInputUp(interactionBind);
                interactionUp |= interactionBind.UIButtonUp;

                if (interactable.releaseToCancel && holdTimer > 0 &&
                interactionUp)
                {
                    holdingObject = null;
                    isHolding = false;
                    holdingInteractable = null;
                    holdStartTime = 0;
                    holdTimer = 0;
                    oneTimeHold = false;
                    oneTimeInteraction = false;
                    if (holdUI != null) Destroy(holdUI);
                    if (holdUIprefab != null) Destroy(holdUIprefab);
                }

                if (holdStartTime != 0)
                {
                    holdTimer = interactable.holdTime + holdStartTime - Time.time;
                    ResetUI(interactable);
                    holdingObject = interactable.gameObject;
                    isHolding = true;
                    holdingInteractable = interactable;

                    if (holdTimer <= 0 && oneTimeHold)
                    {
                        UseAndMaybeConsumeKeys(interactable);
                        interactable.unityEventToTrigger.Invoke();
                        interactable.interactionTimes += 1;
                        if (hasMaxHoldInteractions) interactable.maxHoldInteractions--;
                        holdStartTime = 0;
                        holdTimer = 0;
                        oneTimeHold = false;
                        oneTimeInteraction = false;
                        holdingObject = null;
                        isHolding = false;
                        holdingInteractable = null;
                    }
                }
            }

            Interaction.CanShow = true;
        }

        void UpdateIsHolding()
        {
            if (holdingInteractable.HoldUIEnabled)
            {
                holdPercentage = 1 - Mathf.Clamp01(holdTimer / holdingInteractable.holdTime);

                if (holdUI == null)
                {
                    holdUI = Instantiate(holdingInteractable.overrideHoldPrefab ? holdingInteractable.HoldUIPrefab : InteractionUIPrefab, transform);
                    if (holdUI.TryGetComponent<UIPrefab>(out var uIPrefab))
                    {
                        holdUIprefab = uIPrefab;
                    }

                    DoSomething(holdUIprefab);
                }
                else
                {
                    DoSomething(holdUIprefab);
                }

                void DoSomething(UIPrefab uiprefab)
                {
                    if (uiprefab.hasImage)
                    {
                        uiprefab.interactedInteractable = holdingInteractable;

                        if (uiprefab.hasSlider)
                        {
                            uiprefab.Slider.gameObject.SetActive(holdingInteractable.DragHasSlider);

                            if (holdingInteractable.DragHasSlider && holdingInteractable.applySliderValue)
                            {
                                uiprefab.Slider.value = holdPercentage;
                            }
                        }
                        ;

                        if (holdingInteractable.ActAsWorldPrompt)
                        {
                            Vector3 toMove;

                            if (holdingInteractable.HoldUIOverrideAnchor) toMove = holdingInteractable.transform.TransformPoint(holdingInteractable.LocalPositionHoldUIAnchor);
                            else if (holdingInteractable.WillOverrideAnchor == true) toMove = holdingInteractable.transform.TransformPoint(holdingInteractable.LocalPositionNewAnchor);
                            else toMove = holdingInteractable.transform.position;

                            uiprefab.rectTransform.position = Cam.WorldToScreenPoint(toMove);
                        }
                    }
                }
            }
        }

        void UpdateNotHolding()
        {
            if (holdUI != null) Destroy(holdUI);
            if (holdUIprefab != null) Destroy(holdUIprefab);
        }
    }
}