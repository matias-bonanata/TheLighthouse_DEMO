using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace UIAnimation
{
    /// <summary>
    /// Base class for creating UI animations.
    /// </summary>
    public class UIAnimation : ScriptableObject
    {
        /// <summary>
        /// Enum to define whether the animation should use scaled or unscaled time.
        /// </summary>
        public enum TimeScaleMod
        {
            Scaled,   // Uses Unity's time scale (affected by Time.timeScale)
            Unscaled  // Ignores Unity's time scale (useful for pause menus or independent animations)
        }

        [Tooltip("Event triggered when the animation starts.")]
        public UnityEvent OnAnimationStart;

        [Tooltip("Event triggered when the animation stops.")]
        public UnityEvent OnAnimationStop;

        [Tooltip("Defines whether the animation uses scaled or unscaled time.")]
        public TimeScaleMod timeScaleMod;

        [Tooltip("If true, uses DOTween for animations. If false, uses custom AnimationCurves.")]
        public bool Tween = true;

        [Tooltip("If true, randomizes animation parameters such as duration and values.")]
        public bool Randomize = false;

        [Tooltip("Easing function for the animation's start phase (used when Tween is true).")]
        public Ease easeIn = Ease.Linear;

        [Tooltip("Easing function for the animation's end phase (used when Tween is true).")]
        public Ease easeOut = Ease.Linear;

        [Tooltip("Custom curve for the animation's start phase (used when Tween is false).")]
        public AnimationCurve curveIn = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("Custom curve for the animation's end phase (used when Tween is false).")]
        public AnimationCurve curveOut = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("The minimum duration for randomization.")]
        public float minDuration = 0f;

        [Tooltip("The maximum duration for randomization.")]
        public float maxDuration = 1f;

        [Tooltip("The duration of the animation.")]
        public float duration;

        protected RectTransform _parent; // Reference to the RectTransform being animated
        protected float lastClickTime = 0f; // Stores the last time the animation was triggered (for cooldown or timing purposes)

        /// <summary>
        /// Initializes the animation by setting the parent RectTransform.
        /// </summary>
        /// <param name="parent">The RectTransform to which the animation will be applied.</param>
        public virtual void Initialize(RectTransform parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// Plays the animation. Applies randomization if enabled and triggers the OnAnimationStart event.
        /// </summary>
        public virtual void Play()
        {
            // Apply randomization if enabled
            if (Randomize)
            {
                duration = UnityEngine.Random.Range(minDuration, maxDuration);
            }

            OnAnimationStart?.Invoke(); // Trigger the start event
        }

        /// <summary>
        /// Stops the animation and triggers the OnAnimationStop event.
        /// </summary>
        public virtual void Stop()
        {
            OnAnimationStop?.Invoke(); // Trigger the stop event
        }
    }
}