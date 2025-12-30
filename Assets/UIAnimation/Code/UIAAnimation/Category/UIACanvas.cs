using UnityEngine;
namespace UIAnimation
{
    /// <summary>
    /// Base class for UI animations targeting Canvas-related properties.
    /// Inherits from UIAnimation and provides functionality for manipulating alpha or other CanvasGroup properties.
    /// </summary>
    public class UIACanvas : UIAnimation
    {
        [Tooltip("The starting value for the animation (e.g., alpha for CanvasGroup).")]
        [Range(0, 1)]
        public float StartValue;

        [Tooltip("The target end value for the animation (e.g., alpha for CanvasGroup).")]
        [Range(0, 1)]
        public float EndValue;

        [Tooltip("The minimum starting value for randomization.")]
        [Range(0, 1)]
        public float minStartVelue;

        [Tooltip("The maximum starting value for randomization.")]
        [Range(0, 1)]
        public float maxStartVelue;

        [Tooltip("The minimum end value for randomization.")]
        [Range(0, 1)]
        public float minEndValue;

        [Tooltip("The maximum end value for randomization.")]
        [Range(0, 1)]
        public float maxEndValue;

        /// <summary>
        /// Initializes the animation and randomizes the start and end values if randomization is enabled.
        /// </summary>
        /// <param name="parent">The RectTransform to which the animation will be applied.</param>
        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);
            if (Randomize)
            {
                // Randomize the start and end values within the specified min and max ranges
                StartValue = Random.Range(minStartVelue, maxStartVelue);
                EndValue = Random.Range(minEndValue, maxEndValue);
            }
        }
    }
}