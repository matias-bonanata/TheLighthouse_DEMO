using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UIAnimation
{
    /// <summary>
    /// Base class for UI components that handle interactions and animations.
    /// Implements common pointer events (hover, click, etc.) and manages animations for different interaction states.
    /// </summary>
    public abstract class UIAComponentBase : MonoBehaviour
    {
        [Tooltip("Enable debug logging for animations and interactions.")]
        public bool debug;

        [Tooltip("List of animation settings for different interaction states.")]
        public List<AnimationSettingData> animationSettings = new List<AnimationSettingData>();

        protected Dictionary<InteractiveData, bool> debugFlags = new Dictionary<InteractiveData, bool>(); // Tracks debug states for interactions
        protected Dictionary<InteractiveData, IAnimationStrategy> animationStrategies; // Stores animation strategies for each interaction type

        // UnityEvents for each interaction type
        public UnityEvent OnStartEvent;
        public UnityEvent onScriptPlayEvent;
        public UnityEvent onScriptStopEvent;

        /// <summary>
        /// Initializes the component and plays the Start animation.
        /// </summary>
        protected virtual void Start()
        {
            Initialize();
            PlayAnimation(InteractiveData.Start, "OnPointerEnter called");
            OnStartEvent?.Invoke();
        }

        /// <summary>
        /// Initializes the animation strategies for each interaction type.
        /// </summary>
        public virtual void Initialize()
        {
            animationStrategies = new Dictionary<InteractiveData, IAnimationStrategy>();

            // Create animation strategies for each setting
            foreach (var setting in animationSettings)
            {
                var animations = new List<UIAnimation>();
                foreach (var objData in setting._animationObjectData)
                {
                    // Add Image animations
                    if (objData.objectList.image != null && objData.animationList.uIAImage != null)
                    {
                        var animationInstance = Instantiate(objData.animationList.uIAImage);
                        animationInstance.Initialize(objData.objectList.image.rectTransform);
                        animations.Add(animationInstance);
                        Debug.Log($"Added Image animation for {setting._interactiveData}");
                    }

                    // Add RectTransform animations
                    if (objData.objectList.rectTransform != null && objData.animationList.uIARectTransform != null)
                    {
                        var animationInstance = Instantiate(objData.animationList.uIARectTransform);
                        animationInstance.Initialize(objData.objectList.rectTransform);
                        animations.Add(animationInstance);
                        Debug.Log($"Added RectTransform animation for {setting._interactiveData}");
                    }

                    // Add CanvasGroup animations
                    if (objData.objectList.canvasGroup != null && objData.animationList.uIACanvas != null)
                    {
                        var animationInstance = Instantiate(objData.animationList.uIACanvas);
                        animationInstance.Initialize(objData.objectList.canvasGroup.GetComponent<RectTransform>());
                        animations.Add(animationInstance);
                        Debug.Log($"Added CanvasGroup animation for {setting._interactiveData}");
                    }

                    if (objData.objectList.animator != null && objData.animationList.uIAUnityAnimation != null)
                    {
                        var animationInstance = Instantiate(objData.animationList.uIAUnityAnimation);
                        animationInstance.Initialize(objData.objectList.animator.GetComponent<RectTransform>());
                        animations.Add(animationInstance);
                        Debug.Log($"Added Animator animation for {setting._interactiveData}");
                    }

                    if (objData.objectList.particleSystem != null && objData.animationList.uIAPatricles != null)
                    {
                        var animationInstance = Instantiate(objData.animationList.uIAPatricles);
                        animationInstance.Initialize(objData.objectList.particleSystem.GetComponent<RectTransform>());
                        animations.Add(animationInstance);
                        Debug.Log($"Added Patrical System animation for {setting._interactiveData}");
                    }
                }

                // Create an AnimationController for the animations and add it to the strategies dictionary
                var controller = new AnimationController(animations);
                if (!animationStrategies.ContainsKey(setting._interactiveData))
                {
                    animationStrategies[setting._interactiveData] = controller;
                }
            }
        }

        /// <summary>
        /// Plays the Script animation (triggered programmatically).
        /// </summary>
        public virtual void Play()
        {
            PlayAnimation(InteractiveData.Script, "OnScript called");
            onScriptPlayEvent.Invoke(); // Trigger the UnityEvent
        }

        /// <summary>
        /// Stops the Script animation (triggered programmatically).
        /// </summary>
        public virtual void Stop()
        {
            StopAnimation(InteractiveData.Script, "OnScript called");
            onScriptStopEvent.Invoke(); // Trigger the UnityEvent
        }

        /// <summary>
        /// Plays the animation for a specific interaction type.
        /// </summary>
        /// <param name="interaction">The interaction type (e.g., Hover, Click).</param>
        /// <param name="message">Debug message to log.</param>
        public virtual void PlayAnimation(InteractiveData interaction, string message)
        {
            if (animationStrategies.ContainsKey(interaction))
            {
                animationStrategies[interaction].Play();
                LogDebug(interaction, message);
            }
        }

        /// <summary>
        /// Stops the animation for a specific interaction type.
        /// </summary>
        /// <param name="interaction">The interaction type (e.g., Hover, Click).</param>
        /// <param name="message">Debug message to log.</param>
        public virtual void StopAnimation(InteractiveData interaction, string message)
        {
            if (animationStrategies.ContainsKey(interaction))
            {
                animationStrategies[interaction].Stop();
                LogDebug(interaction, message);
            }
        }

        /// <summary>
        /// Abstract property to define the supported interaction types for the component.
        /// </summary>
        public abstract IEnumerable<InteractiveData> SupportedInteractions { get; }

        /// <summary>
        /// Logs debug messages if debug mode is enabled.
        /// </summary>
        /// <param name="interaction">The interaction type.</param>
        /// <param name="message">The message to log.</param>
        protected void LogDebug(InteractiveData interaction, string message)
        {
            if (debug && animationStrategies.ContainsKey(interaction))
            {
                Debug.Log($"<color=yellow>{message}</color>");
            }
        }
    }
}