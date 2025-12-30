using UnityEngine;
using DG.Tweening;

namespace UIAnimation
{
    /// <summary>
    /// ScriptableObject for managing rotation animations of a RectTransform.
    /// Offers four animation modes: Normal, SideToSide, Shake, and Loop.
    /// Normal mode rotates to a target angle and back,
    /// SideToSide mode creates a pendulum-like motion,
    /// Shake mode creates a dynamic shaking effect,
    /// Loop mode creates a continuous rotation in a specified direction.
    /// </summary>
    [CreateAssetMenu(fileName = "New Rotate Animation", menuName = "UI Animations/RectTransform/Rotate")]
    public class UIARotate : UIARectTransform
    {
        private Tween rotationTween; // Holds the current rotation animation

        // Enum to define animation modes
        public enum AnimationMode
        {
            Normal,      // Rotates to a specific angle and back
            SideToSide,  // Rotates between two angles
            Shake,       // Applies a shaking rotation effect
            Clockwise         // Continuous rotation in a specified direction
        }
        [Tooltip("The current mode of the animation.")]
        public AnimationMode CurrentMode = AnimationMode.Normal; // Current animation mode

        /// <summary>
        /// Initialize the animation with the provided RectTransform parent.
        /// </summary>
        /// <param name="parent">The RectTransform to apply the animation to.</param>
        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);
        }

        /// <summary>
        /// Starts the animation based on the selected mode after an optional delay.
        /// </summary>
        public override void Play()
        {
            base.Play();
            switch (CurrentMode)
            {
                case AnimationMode.Normal:
                    PlayNormalAnimation();
                    break;
                case AnimationMode.SideToSide:
                    PlaySideToSideAnimation();
                    break;
                case AnimationMode.Shake:
                    PlayShakeAnimation();
                    break;
                case AnimationMode.Clockwise:
                    PlayClockwiseAnimation();
                    break;
            }
        }

        /// <summary>
        /// Stops the animation and resets the rotation to its initial value.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            if (_parent != null)
            {
                _parent.DORotate(initialRotation.eulerAngles, duration).SetEase(easeOut);
            }
            if (rotationTween != null)
            {
                rotationTween.Kill();
                rotationTween = null;
            }
        }

        /// <summary>
        /// Plays a standard rotation animation to a target angle and back to the initial angle.
        /// </summary>
        private void PlayNormalAnimation()
        {
            Vector3 targetRotation = initialRotation.eulerAngles;

            if (EndValue.x != 0) targetRotation.x += EndValue.x;
            if (EndValue.y != 0) targetRotation.y += EndValue.y;
            if (EndValue.z != 0) targetRotation.z += EndValue.z;

            if (Tween)
            {
                rotationTween = _parent.DORotate(targetRotation, duration, RotateMode.FastBeyond360)
                    .SetEase(easeIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled)
                    .OnComplete(() =>
                    {
                        _parent.DORotate(initialRotation.eulerAngles, duration, RotateMode.FastBeyond360)
                            .SetEase(easeOut);
                    });
            }
            else
            {
                rotationTween = _parent.DORotate(targetRotation, duration, RotateMode.FastBeyond360)
                    .SetEase(curveIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled)
                    .OnComplete(() =>
                    {
                        _parent.DORotate(initialRotation.eulerAngles, duration, RotateMode.FastBeyond360)
                            .SetEase(curveOut);
                    });
            }
        }

        /// <summary>
        /// Plays a side-to-side rotation animation between two predefined angles.
        /// </summary>
        private void PlaySideToSideAnimation()
        {
            Vector3 leftRotation = initialRotation.eulerAngles + minEndValue;
            Vector3 rightRotation = initialRotation.eulerAngles + maxEndValue;

            if (Tween)
            {
                rotationTween = DOTween.Sequence()
                    .Append(_parent.DORotate(leftRotation, duration / 2, RotateMode.Fast)
                        .SetEase(easeIn)
                        .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled))
                    .Append(_parent.DORotate(rightRotation, duration / 2, RotateMode.Fast)
                        .SetEase(easeOut)
                        .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled))
                    .OnComplete(() => Stop());
            }
            else
            {
                rotationTween = DOTween.Sequence()
                    .Append(_parent.DORotate(leftRotation, duration / 2, RotateMode.Fast)
                        .SetEase(curveIn)
                        .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled))
                    .Append(_parent.DORotate(rightRotation, duration / 2, RotateMode.Fast)
                        .SetEase(curveOut)
                        .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled))
                    .OnComplete(() => Stop());
            }
        }

        /// <summary>
        /// Plays a shake rotation animation for a dynamic and randomized effect.
        /// </summary>
        private void PlayShakeAnimation()
        {
            if (Tween)
            {
                rotationTween = _parent.DOShakeRotation(duration, strength: EndValue, vibrato: 10, randomness: 90)
                    .SetEase(easeIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                rotationTween = _parent.DOShakeRotation(duration, strength: EndValue, vibrato: 10, randomness: 90)
                    .SetEase(curveIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }

        /// <summary>
        /// Plays a continuous rotation animation in the specified direction.
        /// </summary>
        private void PlayClockwiseAnimation()
        {
            if (Tween)
            {
                rotationTween = _parent.DORotate(EndValue, duration, RotateMode.FastBeyond360)
                    .SetEase(easeIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled)
                    .SetLoops(-1, LoopType.Restart);
            }
            else
            {
                rotationTween = _parent.DORotate(EndValue, duration, RotateMode.FastBeyond360)
                    .SetEase(curveIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled)
                    .SetLoops(-1, LoopType.Restart);
            }
        }
    }
}