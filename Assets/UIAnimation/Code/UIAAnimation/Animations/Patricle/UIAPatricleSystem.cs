using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// ScriptableObject for managing particle system animations in a UI context.
    /// This class provides functionality to initialize, play, and stop particle systems
    /// attached to a RectTransform, allowing for dynamic visual effects in UI elements.
    /// </summary>
    [CreateAssetMenu(fileName = "New Patricl Object Animation", menuName = "UI Animations/Particls/Particle System")]
    public class UIAPatricleSystem : UIAPatricles
    {
        /// <summary>
        /// Initializes the particle system animation by setting up the parent RectTransform.
        /// </summary>
        /// <param name="parent">The RectTransform to which the particle system is attached.</param>
        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);
        }

        /// <summary>
        /// Plays the particle system animation.
        /// </summary>
        public override void Play()
        {
            base.Play();
            particleSystem.Play();
        }

        /// <summary>
        /// Stops the particle system animation.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            particleSystem.Stop();
        }
    }
}