using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UIAnimation
{
    public class UIAPanel : UIAComponentBase
    {
        public float InteractiveDelay; // Delay between clicks
        private float lastInteractiveTime = 0f; // Time of the last click

        public UnityEvent onOpenEvent;
        public UnityEvent OnCloseEvent;

        public virtual void Open()
        {

            // Check if enough time has passed since the last click
            if (Time.time - lastInteractiveTime < InteractiveDelay)
            {
                return; // If the delay has not passed, exit the method
            }
            lastInteractiveTime = Time.time;
            PlayAnimation(InteractiveData.Open, "OnOpen called");
            onOpenEvent?.Invoke();
        }

        public virtual void Close()
        {
            PlayAnimation(InteractiveData.Close, "OnClose called");
            OnCloseEvent?.Invoke();
        }
        public override IEnumerable<InteractiveData> SupportedInteractions
        {
            get
            {
                yield return InteractiveData.Start;
                yield return InteractiveData.Open;
                yield return InteractiveData.Close;
                yield return InteractiveData.Script;
            }
        }
    }
}