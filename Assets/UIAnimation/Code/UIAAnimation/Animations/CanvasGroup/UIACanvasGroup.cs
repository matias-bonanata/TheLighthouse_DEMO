using UnityEngine;
using DG.Tweening;

namespace UIAnimation
{
    /// <summary>
    /// ScriptableObject for managing alpha animations of a CanvasGroup.
    /// Offers four animation modes: FadeIn, FadeOut, Blink, and Pulse.
    /// FadeIn mode gradually increases the alpha to make the object visible,
    /// FadeOut mode gradually decreases the alpha to make the object invisible,
    /// Blink mode creates a blinking effect by alternating between two alpha values,
    /// Pulse mode creates a pulsating effect by smoothly transitioning between two alpha values.
    /// </summary>
    [CreateAssetMenu(fileName = "New Canvas Group Object Animation", menuName = "UI Animations/Canvas/Canvas Group")]
    public class UIACanvasGroup : UIACanvas
    {
        public enum AnimationMode
        {
            FadeIn,     // Smooth appearance
            FadeOut,    // Smooth disappearance
            Blink,      // Blinking effect
            Pulse       // Pulsating effect
        }

        [Tooltip("The current mode of the animation.")]
        public AnimationMode CurrentMode = AnimationMode.FadeIn;
        public bool InitializeStartVelue = false;
        private CanvasGroup _canvasGroup;
        private Tween _currentTween;

        /// <summary>
        /// Initializes the animation by setting up the CanvasGroup component and optionally initializing its alpha value.
        /// </summary>
        /// <param name="parent">The RectTransform to which the animation will be applied.</param>
        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);
            _canvasGroup = parent.GetComponent<CanvasGroup>();
            if (InitializeStartVelue)
            {
                _canvasGroup.alpha = StartValue;
            }
        }

        /// <summary>
        /// Plays the animation based on the current mode.
        /// </summary>
        public override void Play()
        {
            base.Play();

            if (_canvasGroup == null)
                return;

            switch (CurrentMode)
            {
                case AnimationMode.FadeIn:
                    FadeIn();
                    break;
                case AnimationMode.FadeOut:
                    FadeOut();
                    break;
                case AnimationMode.Blink:
                    Blink();
                    break;
                case AnimationMode.Pulse:
                    Pulse();
                    break;
            }
        }

        /// <summary>
        /// Stops the animation and resets the CanvasGroup's alpha to its initial state.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            if (_currentTween != null)
            {
                _currentTween.Kill();
                _currentTween = null;
            }

            // Reset to initial state
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f; // Reset alpha to fully visible
            }
        }

        /// <summary>
        /// Performs a fade-in animation by increasing the alpha of the CanvasGroup.
        /// </summary>
        private void FadeIn()
        {
            _canvasGroup.alpha = StartValue; // Start from the randomized or specified StartValue
            if (Tween)
            {
                _currentTween = _canvasGroup.DOFade(EndValue, duration)
                    .SetEase(easeIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                _currentTween = _canvasGroup.DOFade(EndValue, duration)
                    .SetEase(curveIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }

        /// <summary>
        /// Performs a fade-out animation by decreasing the alpha of the CanvasGroup.
        /// </summary>
        private void FadeOut()
        {
            _canvasGroup.alpha = StartValue; // Start from the randomized or specified StartValue
            if (Tween)
            {
                _currentTween = _canvasGroup.DOFade(EndValue, duration)
                    .SetEase(easeOut)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                _currentTween = _canvasGroup.DOFade(EndValue, duration)
                    .SetEase(curveOut)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }

        /// <summary>
        /// Performs a blinking animation by alternating the alpha of the CanvasGroup between two values.
        /// </summary>
        private void Blink()
        {
            _canvasGroup.alpha = StartValue; // Start from the randomized or specified StartValue
            if (Tween)
            {
                _currentTween = DOTween.Sequence()
                    .Append(_canvasGroup.DOFade(EndValue, duration / 2).SetEase(easeIn))
                    .Append(_canvasGroup.DOFade(StartValue, duration / 2).SetEase(easeOut))
                    .SetLoops(-1, LoopType.Restart) // Infinite loop
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                _currentTween = DOTween.Sequence()
                    .Append(_canvasGroup.DOFade(EndValue, duration / 2).SetEase(curveIn))
                    .Append(_canvasGroup.DOFade(StartValue, duration / 2).SetEase(curveOut))
                    .SetLoops(-1, LoopType.Restart) // Infinite loop
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }

        /// <summary>
        /// Performs a pulsating animation by smoothly transitioning the alpha of the CanvasGroup between two values.
        /// </summary>
        private void Pulse()
        {
            _canvasGroup.alpha = StartValue; // Start from the randomized or specified StartValue
            if (Tween)
            {
                _currentTween = DOTween.Sequence()
                    .Append(_canvasGroup.DOFade(EndValue, duration / 2).SetEase(easeIn))
                    .Append(_canvasGroup.DOFade(StartValue, duration / 2).SetEase(easeOut))
                    .SetLoops(-1, LoopType.Yoyo) // Infinite loop with yoyo effect
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                _currentTween = DOTween.Sequence()
                    .Append(_canvasGroup.DOFade(EndValue, duration / 2).SetEase(curveIn))
                    .Append(_canvasGroup.DOFade(StartValue, duration / 2).SetEase(curveOut))
                    .SetLoops(-1, LoopType.Yoyo) // Infinite loop with yoyo effect
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }
    }
}