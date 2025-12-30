using UnityEngine;
using UnityEngine.UI;

namespace UIAnimation
{
    /// <summary>
    /// Base class for UI animations targeting an Image component.
    /// Inherits from UIAnimation and provides functionality for manipulating Image properties.
    /// </summary>
    public class UIAImage : UIAnimation
    {
        protected Image image;

        /// <summary>
        /// Initializes the animation by retrieving the Image component from the parent RectTransform.
        /// </summary>
        /// <param name="parent">The RectTransform to which the animation will be applied.</param>
        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);
            image = _parent.GetComponent<Image>(); // Get the Image component from the RectTransform
        }
    }
}