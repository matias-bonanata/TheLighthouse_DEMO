using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UIAnimation
{
    public class UIAButton : UIAComponentBase, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public float InteractiveDelay; // Delay between clicks
        private float lastInteractiveTime = 0f; // Time of the last click

        public UnityEvent onPointerEnterEvent;
        public UnityEvent onPointerExitEvent;
        public UnityEvent onPointerDownEvent;
        public UnityEvent onPointerUpEvent;
        public UnityEvent onPointerClickEvent;


        /// <summary>
        /// Handles the pointer enter event and plays the Hover animation.
        /// </summary>
        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            PlayAnimation(InteractiveData.Hover, "OnPointerEnter called");
            onPointerEnterEvent.Invoke(); // Trigger the UnityEvent
        }

        /// <summary>
        /// Handles the pointer exit event and stops the Hover animation.
        /// </summary>
        public virtual void OnPointerExit(PointerEventData eventData)
        {
            StopAnimation(InteractiveData.Hover, "OnPointerExit called");
            onPointerExitEvent.Invoke(); // Trigger the UnityEvent
        }

        /// <summary>
        /// Handles the pointer down event and plays the PressDown animation.
        /// </summary>
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            PlayAnimation(InteractiveData.PressDown, "OnPointerDown called");
            onPointerDownEvent.Invoke(); // Trigger the UnityEvent
        }

        /// <summary>
        /// Handles the pointer up event and stops the PressDown animation.
        /// </summary>
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            StopAnimation(InteractiveData.PressDown, "OnPointerUp called");
            onPointerUpEvent.Invoke(); // Trigger the UnityEvent
        }

        /// <summary>
        /// Handles the pointer click event and plays the Click animation.
        /// </summary>
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            // Check if enough time has passed since the last click
            if (Time.time - lastInteractiveTime < InteractiveDelay)
            {
                return; // If the delay has not passed, exit the method
            }
            lastInteractiveTime = Time.time;
            PlayAnimation(InteractiveData.Click, "OnPointerClick called");
            onPointerClickEvent?.Invoke();
        }

        public override void PlayAnimation(InteractiveData interaction, string message)
        {
            base.PlayAnimation(interaction, message);
        }

        public override void StopAnimation(InteractiveData interaction, string message)
        {
            base.StopAnimation(interaction, message);
        }

        public override IEnumerable<InteractiveData> SupportedInteractions
        {
            get
            {
                yield return InteractiveData.Hover;
                yield return InteractiveData.Click;
                yield return InteractiveData.PressDown;
                yield return InteractiveData.Start;
                yield return InteractiveData.Script;
            }
        }
    }
}