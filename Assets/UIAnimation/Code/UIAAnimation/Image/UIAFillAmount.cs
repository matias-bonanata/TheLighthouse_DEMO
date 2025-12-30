using UnityEngine;
using DG.Tweening;

namespace UIAnimation
{
    [CreateAssetMenu(fileName = "New Fill Amount Animation", menuName = "UI Animations/Image/FillAmount")]
    public class UIAFillAmount : UIAImage
    {
        public enum AnimationMode
        {
            Normal,    // Changes fillAmount from StartValue to EndValue
            PingPong,  // Loops fillAmount between StartValue and EndValue
            Step,      // Changes fillAmount in discrete steps
        }

        [Tooltip("The current mode of the animation.")]
        public AnimationMode CurrentMode = AnimationMode.Normal;

        [Tooltip("The starting value for the fill amount (0 to 1).")]
        public float StartValue = 0f;

        [Tooltip("The target end value for the fill amount (0 to 1).")]
        public float EndValue = 1f;

        [Tooltip("Number of steps for the step animation.")]
        public int Steps = 4; // Only used in Step mode

        [Tooltip("Time to pause at each step before moving to the next step.")]
        public float StepPauseTime = 0.5f; // Time to wait at each step

        [Tooltip("The minimum starting value for randomization.")]
        public float minStartValue = 0f;

        [Tooltip("The maximum starting value for randomization.")]
        public float maxStartValue = 1f;

        [Tooltip("The minimum end value for randomization.")]
        public float minEndValue = 0f;

        [Tooltip("The maximum end value for randomization.")]
        public float maxEndValue = 1f;

        private Sequence _fillSequence;
        private Tween _fillTween;

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
                StartValue = Random.Range(minStartValue, maxStartValue);
                EndValue = Random.Range(minEndValue, maxEndValue);
            }
            // Set the initial fill amount
            if (image != null)
            {
                image.fillAmount = StartValue;
            }
        }

        /// <summary>
        /// Plays the fill amount animation based on the selected mode.
        /// </summary>
        public override void Play()
        {
            base.Play();

            if (image == null)
                return;

            // Stop the current animation if it exists
            if (_fillTween != null)
            {
                _fillTween.Kill();
                _fillTween = null;
            }

            if (_fillSequence != null)
            {
                _fillSequence.Kill();
                _fillSequence = null;
            }

            // Start the fill amount animation based on the selected mode
            switch (CurrentMode)
            {
                case AnimationMode.Normal:
                    PlayNormalAnimation();
                    break;
                case AnimationMode.PingPong:
                    PlayPingPongAnimation();
                    break;
                case AnimationMode.Step:
                    PlayStepAnimation();
                    break;
            }
        }

        /// <summary>
        /// Stops the animation and resets the fill amount to its initial value.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            // Stop the animation and reset the fill amount to the initial value
            if (_fillTween != null)
            {
                _fillTween.Kill();
                _fillTween = null;
            }

            if (_fillSequence != null)
            {
                _fillSequence.Kill();
                _fillSequence = null;
            }

            if (image != null)
            {
                image.fillAmount = StartValue; // Reset the fill amount to the initial value
            }
        }

        /// <summary>
        /// Plays a normal animation, changing fillAmount from StartValue to EndValue.
        /// </summary>
        private void PlayNormalAnimation()
        {
            if (Tween)
            {
                _fillTween = image.DOFillAmount(EndValue, duration)
                    .SetEase(easeIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled)
                    .OnComplete(() => Stop());
            }
            else
            {
                _fillTween = image.DOFillAmount(EndValue, duration)
                    .SetEase(curveIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled)
                    .OnComplete(() => Stop());
            }
        }

        /// <summary>
        /// Plays a ping-pong animation, looping fillAmount between StartValue and EndValue.
        /// </summary>
        private void PlayPingPongAnimation()
        {
            _fillSequence = DOTween.Sequence();

            if (Tween)
            {
                _fillSequence.Append(image.DOFillAmount(EndValue, duration / 2).SetEase(easeIn))
                             .Append(image.DOFillAmount(StartValue, duration / 2).SetEase(easeOut))
                             .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                _fillSequence.Append(image.DOFillAmount(EndValue, duration / 2).SetEase(curveIn))
                             .Append(image.DOFillAmount(StartValue, duration / 2).SetEase(curveOut))
                             .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }


        /// <summary>
        /// Plays a step animation, changing fillAmount in discrete steps with pauses between steps.
        /// </summary>
        private void PlayStepAnimation()
        {
            _fillSequence = DOTween.Sequence();
            float stepDuration = duration / Steps;

            for (int i = 1; i <= Steps; i++)
            {
                float targetValue = Mathf.Lerp(StartValue, EndValue, (float)i / Steps);

                // Move to the next step
                _fillSequence.Append(image.DOFillAmount(targetValue, stepDuration).SetEase(easeIn));

                // Add a pause after each step
                if (i < Steps) // No pause after the last step
                {
                    _fillSequence.AppendInterval(StepPauseTime);
                }
            }

            _fillSequence.SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
        }
    }
}