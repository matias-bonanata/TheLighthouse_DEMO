using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace UIAnimation
{
    public class UIAController : MonoBehaviour
    {
        public enum ExecutionMode { Script, Start, OnEnable }
        
        [Header("Component")]
        [Tooltip("Array of UI panels that will be animated")]
        [SerializeField] private UIAPanel[] uiComponents;

        [Header("Setting")]
        [Tooltip("Time interval between consecutive animations")]
        [SerializeField] private float delayBetweenAnimations = 0.5f;
        
        [Tooltip("Determines when animations should be triggered")]
        [SerializeField] private ExecutionMode executionMode = ExecutionMode.Script;
        
        [Tooltip("If enabled, animations will close when object is disabled")]
        [SerializeField] private bool IsOnDisable = false;
        
        [Tooltip("If enabled, animations will play in a continuous loop")]
        [SerializeField] private bool loop = false;
        
        [Tooltip("Enable to see debug logs in the console")]
        [SerializeField] private bool debugMode = false;

        [Header("Events")]
        [Tooltip("Event triggered when animations start opening")]
        public UnityEvent OnOpenEvent;
        
        [Tooltip("Event triggered when animations start closing")]
        public UnityEvent OnCloseEvent;
        
        [Tooltip("Event triggered after all animations have finished playing")]
        public UnityEvent OnAllAnimationsPlayedEvent;

        /// <summary>
        /// Initializes and starts animations if execution mode is set to Start
        /// </summary>
        private void Start()
        {
            if (executionMode == ExecutionMode.Start)
                OpenAll();
        }

        /// <summary>
        /// Initializes and starts animations if execution mode is set to OnEnable
        /// </summary>
        private void OnEnable()
        {
            if (executionMode == ExecutionMode.OnEnable)
                OpenAll();
        }

        /// <summary>
        /// Handles animation closing when object is disabled if IsOnDisable is true
        /// </summary>
        private void OnDisable()
        {
            if (IsOnDisable)
                CloseAll();
        }

        /// <summary>
        /// Starts all UI animations in sequence and triggers the OnOpenEvent
        /// </summary>
        public void OpenAll()
        {
            if (debugMode) Debug.Log("Open all animations...");

            StopAllCoroutines();
            StartCoroutine(PlayAnimationsWithDelay());
            OnOpenEvent?.Invoke();
        }

        /// <summary>
        /// Closes all UI animations and triggers the OnCloseEvent
        /// </summary>
        public void CloseAll()
        {
            if (debugMode) Debug.Log("Close all animations...");

            foreach (var component in uiComponents)
            {
                component.Initialize();
                component?.Close();
            }
            OnCloseEvent?.Invoke();
        }

        /// <summary>
        /// Coroutine that handles the sequential playing of animations with specified delays
        /// </summary>
        /// <returns>IEnumerator for coroutine execution</returns>
        private IEnumerator PlayAnimationsWithDelay()
        {
            do
            {
                foreach (var component in uiComponents)
                {
                    if (component != null)
                    {
                        component.Initialize();
                        component.Open();
                        yield return new WaitForSeconds(delayBetweenAnimations);
                    }
                }
                OnAllAnimationsPlayedEvent?.Invoke(); // Invoke the event after all animations are played
            } while (loop);
        }
    }
}
