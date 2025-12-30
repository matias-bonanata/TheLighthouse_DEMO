using UnityEngine;
using DG.Tweening;

namespace UIAnimation
{
    /// <summary>
    /// ScriptableObject for managing scale animations of a RectTransform.
    /// Offers two animation modes: PingPong and TargetSize.
    /// PingPong mode scales back and forth between values,
    /// TargetSize mode scales from start to end value.
    /// </summary>
    [CreateAssetMenu(fileName = "New Scale Animation", menuName = "UI Animations/RectTransform/Scale")]
    public class UIAScale : UIARectTransform
    {
        /// <summary>
        /// Modes for the scale animation.
        /// </summary>
        public enum AnimationMode
        {
            PingPong,
            TargetSize
        }

        [Tooltip("The current mode of the animation.")]
        public AnimationMode CurrentMode = AnimationMode.PingPong;

        [Tooltip("The initial scale value for the animation.")]
        public Vector3 StartValue;

        [Tooltip("The middle scale value for advanced animations.")]
        public Vector3 MiddleValue;

        [Tooltip("If true, StartValue will be set to the current scale of the RectTransform when Play is called.")]
        public bool AutoStartVelue;

        [Tooltip("Enables advanced animation with intermediate scaling.")]
        public bool Advanced = false;

        private Sequence scaleTween; // Handles animations made up of multiple steps.
        private Tweener scaleTweener; // Handles simpler animations.

        /// <summary>
        /// Initializes the animation by setting the RectTransform's scale based on the mode.
        /// </summary>
        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);

            // Set the initial scale if the mode is TargetSize.
            if (CurrentMode == AnimationMode.TargetSize && !AutoStartVelue)
            {
                _parent.localScale = StartValue;
            }
        }

        /// <summary>
        /// Plays the selected animation after a delay.
        /// </summary>
        public override void Play()
        {
            base.Play();

            // Если AutoStartVelue = true, устанавливаем StartValue в текущий масштаб _parent
            if (AutoStartVelue)
            {
                StartValue = _parent.localScale;
            }

            switch (CurrentMode)
            {
                case AnimationMode.PingPong:
                    PlayPingPongAnimation();
                    break;
                case AnimationMode.TargetSize:
                    PlayTargetSizeAnimation();
                    break;
            }
        }

        /// <summary>
        /// Stops the animation and resets the scale to the initial value with an easing effect.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            // Stop the scale animations and reset references.
            if (scaleTween != null)
            {
                scaleTween.Kill();
                scaleTween = null;
            }
            if (scaleTweener != null)
            {
                scaleTweener.Kill();
                scaleTweener = null;
            }

            // Return to the initial scale with animation.
            _parent.DOScale(initialScale, 0.5f).SetEase(Ease.InOutSine);
        }

        /// <summary>
        /// Plays a PingPong animation that alternates between the start and end scale values.
        /// </summary>
        private void PlayPingPongAnimation()
        {
            scaleTween = DOTween.Sequence();

            if (Tween)
            {
                // Animate to EndValue.
                scaleTween.Append(_parent.DOScale(EndValue, duration)
                    .SetEase(easeIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled));

                // Animate back to the initial scale.
                scaleTween.Append(_parent.DOScale(initialScale, duration)
                    .SetEase(easeOut)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled));
            }
            else
            {
                // Using curves for animations.
                scaleTween.Append(_parent.DOScale(EndValue, duration)
                    .SetEase(curveIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled));

                scaleTween.Append(_parent.DOScale(initialScale, duration)
                    .SetEase(curveOut)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled));
            }
        }

        /// <summary>
        /// Plays a scale animation from StartValue to EndValue.
        /// If Advanced is true, scales from StartValue to MiddleValue, then to EndValue.
        /// </summary>
        private void PlayTargetSizeAnimation()
        {
            if (Advanced)
            {
                // Advanced animation: StartValue -> MiddleValue -> EndValue.
                scaleTween = DOTween.Sequence();

                // Устанавливаем начальное значение масштаба
                _parent.localScale = StartValue;

                if (Tween)
                {
                    // Scale to MiddleValue.
                    scaleTween.Append(_parent.DOScale(MiddleValue, duration / 2)
                        .SetEase(easeIn)
                        .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled));

                    // Scale to EndValue.
                    scaleTween.Append(_parent.DOScale(EndValue, duration / 2)
                        .SetEase(easeOut)
                        .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled));
                }
                else
                {
                    // Using curves for animations.
                    scaleTween.Append(_parent.DOScale(MiddleValue, duration / 2)
                        .SetEase(curveIn)
                        .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled));

                    scaleTween.Append(_parent.DOScale(EndValue, duration / 2)
                        .SetEase(curveOut)
                        .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled));
                }
            }
            else
            {
                // Simple animation: StartValue -> EndValue.
                _parent.localScale = StartValue;

                if (Tween)
                {
                    scaleTweener = _parent.DOScale(EndValue, duration)
                        .SetEase(easeIn)
                        .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
                }
                else
                {
                    scaleTweener = _parent.DOScale(EndValue, duration)
                        .SetEase(curveIn)
                        .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
                }
            }
        }
    }
}