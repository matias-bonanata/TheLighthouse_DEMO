namespace FastStudios
{
    public partial class InteractionManager
    {
        void HandleUnityEvent(Interactable interactable)
        {
            PressEInputBind bind = InputHandler.ResolveBind(Interaction, interactable.OverrideInteractionKey, interactable.NewInteraction);
            bool pressed = InputHandler.GeneralInputDown(bind);
            pressed |= bind.UIButtonDown;

            if (pressed)
            {
                UseAndMaybeConsumeKeys(interactable);
                interactable.unityEventToTrigger.Invoke();
                interactable.interactionTimes += 1;
            }
            else if (interactable.HasAutoInteract && !interactable.oneTimeUnityEventBool)
            {
                UseAndMaybeConsumeKeys(interactable);
                interactable.unityEventToTrigger.Invoke();
                interactable.interactionTimes += 1;
                interactable.oneTimeUnityEventBool = true;
            }

            Interaction.CanShow = true;
        }

    }
}