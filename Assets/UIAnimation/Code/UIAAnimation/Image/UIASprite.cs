using UnityEngine;
using DG.Tweening;

namespace UIAnimation
{
    /// <summary>
    /// ScriptableObject for managing sprite animations of an Image component.
    /// Offers one animation mode: SpriteSwap.
    /// SpriteSwap mode allows swapping between multiple sprites in a loop or switching to a single target sprite once.
    /// </summary>
    [CreateAssetMenu(fileName = "New Sprite Animation", menuName = "UI Animations/Image/Sprite")]
    public class UIASprite : UIAImage
    {
        public enum AnimationMode
        {
            SpriteSwap
        }

        [Tooltip("The current mode of the animation.")]
        public AnimationMode CurrentMode = AnimationMode.SpriteSwap;

        [Tooltip("If true, the animation will loop through the Sprites array. If false, it will switch to TargetSprite once.")]
        public bool IsLoop = false;

        [Tooltip("Array of sprites to swap between if IsLoop is true.")]
        public Sprite[] Sprites;

        [Tooltip("The sprite to switch to if IsLoop is false.")]
        public Sprite TargetSprite;

        private Sequence _spriteSwapSequence;

        /// <summary>
        /// Initializes the animation by setting the initial sprite if the Sprites array is not empty.
        /// </summary>
        /// <param name="parent">The RectTransform to which the animation will be applied.</param>
        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);

            // Set the initial sprite if the Sprites array is not empty
            if (Sprites != null && Sprites.Length > 0 && image != null)
            {
                image.sprite = Sprites[0];
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
            if (_spriteSwapSequence != null)
            {
                _spriteSwapSequence.Kill();
                _spriteSwapSequence = null;
            }


            switch (CurrentMode)
            {
                case AnimationMode.SpriteSwap:
                    PlaySpriteSwap();
                    break;
            }
        }

        /// <summary>
        /// Stops the animation and resets the sprite to its initial value.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            if (_spriteSwapSequence != null)
            {
                _spriteSwapSequence.Kill();
                _spriteSwapSequence = null;
            }

            if (image != null && Sprites != null && Sprites.Length > 0)
            {
                image.sprite = Sprites[0]; // Reset the sprite to the initial value
            }
        }

        /// <summary>
        /// Plays the sprite swap animation, either looping through the Sprites array or switching to the TargetSprite once.
        /// </summary>
        private void PlaySpriteSwap()
        {
            if (IsLoop)
            {
                if (Sprites == null || Sprites.Length == 0)
                    return;

                // Create a sequence for the sprite swap animation
                _spriteSwapSequence = DOTween.Sequence();

                int currentIndex = 0;
                float interval = duration / Sprites.Length; // Interval between sprite swaps

                // Add steps to the sequence
                for (int i = 0; i < Sprites.Length; i++)
                {
                    int index = i; // Local variable for closure
                    _spriteSwapSequence.AppendCallback(() => image.sprite = Sprites[index])
                                       .AppendInterval(interval);
                }

                // Loop the animation
                _spriteSwapSequence.SetLoops(-1, LoopType.Restart)
                                   .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
            else
            {
                if (TargetSprite == null)
                    return;

                // Single sprite swap
                _spriteSwapSequence = DOTween.Sequence()
                    .AppendCallback(() => image.sprite = TargetSprite)
                    .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
            }
        }
    }
}