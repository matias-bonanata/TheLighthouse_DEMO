using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// Base class for RectTransform-based UI animations.
    /// </summary>
    public class UIARectTransform : UIAnimation
    {
        [Tooltip("The minimum end value for randomization of the animation.")]
        public Vector3 minEndValue = Vector3.zero;

        [Tooltip("The maximum end value for randomization of the animation.")]
        public Vector3 maxEndValue = new Vector3(1f, 1f, 1f);

        [Tooltip("The target end value for the animation.")]
        public Vector3 EndValue;

        protected Vector3 initialAnchoredPosition; // Stores the initial position of the RectTransform
        protected Quaternion initialRotation; // Stores the initial rotation of the RectTransform
        protected Vector3 initialScale; // Stores the initial scale of the RectTransform

        /// <summary>
        /// Initializes the animation by storing the initial position, rotation, and scale of the RectTransform.
        /// </summary>
        /// <param name="parent">The RectTransform to which the animation will be applied.</param>
        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);
            initialAnchoredPosition = _parent.anchoredPosition;
            initialRotation = _parent.rotation;
            initialScale = _parent.localScale;
        }

        /// <summary>
        /// Plays the animation and calculates a randomized end value if randomization is enabled.
        /// </summary>
        public override void Play()
        {
            base.Play();
            if (Randomize)
            {
                // Randomize the end value within the specified min and max range
                EndValue = new Vector3(
                   Random.Range(minEndValue.x, maxEndValue.x),
                   Random.Range(minEndValue.y, maxEndValue.y),
                   Random.Range(minEndValue.z, maxEndValue.z)
               );
            }
        }
    }
}