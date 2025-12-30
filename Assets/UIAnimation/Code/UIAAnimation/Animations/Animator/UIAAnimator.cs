using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// ScriptableObject for managing Animator-based animations of a UI element.
    /// Supports two animation modes: Trigger and Bool.
    /// In Trigger mode, the animation is activated by setting a trigger parameter in the Animator.
    /// In Bool mode, the animation is controlled by setting a boolean parameter in the Animator.
    /// </summary>
    [CreateAssetMenu(fileName = "New Animator Object Animation", menuName = "UI Animations/Animation/Animator")]
    public class UIAAnimator : UIAUnityAnimation
    {
        public enum AnimationMode
        {
            Trigger,
            Bool
        }

        [Tooltip("The animation mode: Trigger or Bool.")]
        public AnimationMode animationMode = AnimationMode.Trigger;

        [Tooltip("The name of the animation parameter in the Animator.")]
        public string AnimationName;

        [Tooltip("The boolean value used to control the animation in Bool mode.")]
        public bool value; // Boolean parameter for controlling the animation

        /// <summary>
        /// Initializes the animation by setting up the RectTransform and Animator.
        /// </summary>
        /// <param name="parent">The RectTransform to which the animation is applied.</param>
        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);
        }

        /// <summary>
        /// Plays the animation based on the selected animation mode.
        /// In Trigger mode, sets the trigger parameter in the Animator.
        /// In Bool mode, sets the boolean parameter in the Animator.
        /// </summary>
        public override void Play()
        {
            base.Play();

            if (animator == null)
                return;

            switch (animationMode)
            {
                case AnimationMode.Trigger:
                    animator.SetTrigger(AnimationName);
                    break;

                case AnimationMode.Bool:
                    animator.SetBool(AnimationName, value);
                    break;
            }
        }

        /// <summary>
        /// Stops the animation based on the selected animation mode.
        /// In Trigger mode, resets the trigger parameter in the Animator.
        /// In Bool mode, sets the boolean parameter to false in the Animator.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            if (animator == null)
                return;

            switch (animationMode)
            {
                case AnimationMode.Trigger:
                    animator.ResetTrigger(AnimationName);
                    break;

                case AnimationMode.Bool:
                    animator.SetBool(AnimationName, false);
                    break;
            }
        }
    }
}