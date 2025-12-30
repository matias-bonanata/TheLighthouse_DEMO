using UnityEngine;
using DG.Tweening;

namespace UIAnimation
{
    /// <summary>
    /// ScriptableObject for managing color animations of an Image component.
    /// Offers seven animation modes: FadeIn, FadeOut, Blink, Pulse, ColorChange, RandomColor, and PingPong.
    /// FadeIn mode gradually changes the alpha from transparent to opaque,
    /// FadeOut mode gradually changes the alpha from opaque to transparent,
    /// Blink mode alternates between two colors,
    /// Pulse mode creates a smooth transition between two colors back and forth,
    /// ColorChange mode transitions between two colors,
    /// RandomColor mode changes the color to a random value,
    /// PingPong mode transitions between StartColor and EndColor back and forth.
    /// </summary>
    [CreateAssetMenu(fileName = "New Color Animation", menuName = "UI Animations/Image/Color")]
    public class UIAColor : UIAImage
    {
        public enum AnimationMode
        {
            FadeIn,         // Smooth appearance (from transparent to opaque)
            FadeOut,        // Smooth disappearance (from opaque to transparent)
            Blink,          // Blinking (alternating between two colors)
            Pulse,          // Pulsating (smooth transition between two colors back and forth)
            ColorChange,    // Smooth transition between two colors
            RandomColor,    // Random color change
            PingPong        // Smooth transition between StartColor and EndColor back and forth
        }

        [Tooltip("The current mode of the animation.")]
        public AnimationMode CurrentMode = AnimationMode.FadeIn;

        public Color StartColor = Color.white; // Starting color
        public Color EndColor = Color.black;   // Target color
        public bool ManualRandomColour = false;
        public Color[] RandomColor;         // Array of colors for random selection

        private Tween _colorTween; // Stores the current color animation

        /// <summary>
        /// Initializes the animation by setting the initial color if needed.
        /// </summary>
        /// <param name="parent">The RectTransform to which the animation will be applied.</param>
        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);

            // Set the initial color if needed
            if (image != null)
            {
                image.color = StartColor;
            }
        }

        /// <summary>
        /// Plays the animation based on the selected mode.
        /// </summary>
        public override void Play()
        {
            base.Play();

            if (image == null)
                return;

            // Stop the current animation if it exists
            if (_colorTween != null)
            {
                _colorTween.Kill();
                _colorTween = null;
            }

            // Choose the animation mode
            switch (CurrentMode)
            {
                case AnimationMode.FadeIn:
                    PlayFadeIn();
                    break;
                case AnimationMode.FadeOut:
                    PlayFadeOut();
                    break;
                case AnimationMode.Blink:
                    PlayBlink();
                    break;
                case AnimationMode.Pulse:
                    PlayPulse();
                    break;
                case AnimationMode.ColorChange:
                    PlayColorChange();
                    break;
                case AnimationMode.RandomColor:
                    PlayRandomColor();
                    break;
                case AnimationMode.PingPong:
                    PlayPingPong();
                    break;
            }
        }

        /// <summary>
        /// Stops the animation and resets the color to its initial value.
        /// </summary>
        /// <summary>
        /// Stops the animation and smoothly resets the color to its initial value using DOTween.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            // Stop the current animation if it exists
            if (_colorTween != null)
            {
                _colorTween.Kill();
                _colorTween = null;
            }

            // Smoothly reset the color to the initial value using DOTween
            if (image != null)
            {
                if (Tween)
                {
                    _colorTween = image.DOColor(StartColor, duration / 2)
                        .SetEase(easeOut)
                        .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
                }
                else
                {
                    _colorTween = image.DOColor(StartColor, duration / 2)
                        .SetEase(curveOut)
                        .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
                }
            }
        }

        /// <summary>
        /// Plays a fade-in animation, transitioning from transparent to opaque.
        /// </summary>
        private void PlayFadeIn()
        {
            image.color = new Color(StartColor.r, StartColor.g, StartColor.b, 0); // Start from transparent

            if (Tween)
            {
                _colorTween = image.DOFade(StartColor.a, duration)
                    .SetEase(easeIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                _colorTween = image.DOFade(StartColor.a, duration)
                    .SetEase(curveIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }

        /// <summary>
        /// Plays a fade-out animation, transitioning from opaque to transparent.
        /// </summary>
        private void PlayFadeOut()
        {
            image.color = StartColor; // Start from opaque

            if (Tween)
            {
                _colorTween = image.DOFade(0, duration)
                    .SetEase(easeOut)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                _colorTween = image.DOFade(0, duration)
                    .SetEase(curveOut)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }

        /// <summary>
        /// Plays a blinking animation, alternating between StartColor and EndColor.
        /// </summary>
        private void PlayBlink()
        {
            if (Tween)
            {
                _colorTween = DOTween.Sequence()
                    .Append(image.DOColor(EndColor, duration / 2).SetEase(easeIn))
                    .Append(image.DOColor(StartColor, duration / 2).SetEase(easeOut))
                    .SetLoops(-1, LoopType.Restart) // Infinite loop
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                _colorTween = DOTween.Sequence()
                    .Append(image.DOColor(EndColor, duration / 2).SetEase(curveIn))
                    .Append(image.DOColor(StartColor, duration / 2).SetEase(curveOut))
                    .SetLoops(-1, LoopType.Restart) // Infinite loop
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }

        /// <summary>
        /// Plays a pulsating animation, smoothly transitioning between StartColor and EndColor back and forth.
        /// </summary>
        private void PlayPulse()
        {
            if (Tween)
            {
                _colorTween = DOTween.Sequence()
                    .Append(image.DOColor(EndColor, duration / 2).SetEase(easeIn))
                    .Append(image.DOColor(StartColor, duration / 2).SetEase(easeOut))
                    .SetLoops(-1, LoopType.Yoyo) // Infinite loop with yoyo effect
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                _colorTween = DOTween.Sequence()
                    .Append(image.DOColor(EndColor, duration / 2).SetEase(curveIn))
                    .Append(image.DOColor(StartColor, duration / 2).SetEase(curveOut))
                    .SetLoops(-1, LoopType.Yoyo) // Infinite loop with yoyo effect
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }

        /// <summary>
        /// Plays a color change animation, transitioning from StartColor to EndColor.
        /// </summary>
        private void PlayColorChange()
        {
            if (Tween)
            {
                _colorTween = image.DOColor(EndColor, duration)
                    .SetEase(easeIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                _colorTween = image.DOColor(EndColor, duration)
                    .SetEase(curveIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }

        /// <summary>
        /// Plays a random color change animation, either selecting a color from the RandomColor array or generating a random color.
        /// </summary>
        private void PlayRandomColor()
        {
            Color randomColor;

            if (ManualRandomColour && RandomColor != null && RandomColor.Length > 0)
            {
                // Select a random color from the RandomColor array
                int randomIndex = Random.Range(0, RandomColor.Length);
                randomColor = RandomColor[randomIndex];
            }
            else
            {
                // Generate a random color manually
                randomColor = new Color(
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f)
                );
            }

            if (Tween)
            {
                _colorTween = image.DOColor(randomColor, duration)
                    .SetEase(easeIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                _colorTween = image.DOColor(randomColor, duration)
                    .SetEase(curveIn)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }

        /// <summary>
        /// Plays a ping-pong animation, transitioning between StartColor and EndColor back and forth.
        /// </summary>
        private void PlayPingPong()
        {
            if (Tween)
            {
                _colorTween = DOTween.Sequence()
                    .Append(image.DOColor(EndColor, duration / 2).SetEase(easeIn))
                    .Append(image.DOColor(StartColor, duration / 2).SetEase(easeOut))
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                _colorTween = DOTween.Sequence()
                    .Append(image.DOColor(EndColor, duration / 2).SetEase(curveIn))
                    .Append(image.DOColor(StartColor, duration / 2).SetEase(curveOut))
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }
    }
}