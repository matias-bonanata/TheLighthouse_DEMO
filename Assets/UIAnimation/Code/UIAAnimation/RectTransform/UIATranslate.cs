using UnityEngine;
using DG.Tweening;

namespace UIAnimation
{
    [CreateAssetMenu(fileName = "New Translate Animation", menuName = "UI Animations/RectTransform/Translate")]
    public class UIATranslate : UIARectTransform
    {
        public enum AnimationMode
        {
            PingPong,
            TargetPosition
        }

        public Vector3 StartValue;
        public AnimationMode CurrentMode = AnimationMode.PingPong;
        public bool InitializeStartPosition = false;
        public bool AutoStartPosition = false;
        private Sequence translateTween;
        private Tweener translateTweener;

        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);
            if (AutoStartPosition)
            {
                StartValue = _parent.anchoredPosition;
                _parent.anchoredPosition = StartValue;
                InitializeStartPosition = false;
            }
            if (CurrentMode == AnimationMode.TargetPosition && InitializeStartPosition)
            {
                _parent.anchoredPosition = StartValue;
            }
        }

        public override void Play()
        {
            base.Play();
            switch (CurrentMode)
            {
                case AnimationMode.PingPong:
                    PlayPingPongAnimation();
                    break;
                case AnimationMode.TargetPosition:
                    PlayTargetPositionAnimation();
                    break;
            }
        }

        public override void Stop()
        {
            base.Stop();
            translateTween?.Kill();
            translateTweener?.Kill();

            // Плавно повертаємо до початкової позиції
            _parent.DOAnchorPos(initialAnchoredPosition, 0.5f).SetEase(Ease.InOutSine);
        }

        private void PlayPingPongAnimation()
        {
            Vector2 targetPosition = initialAnchoredPosition + EndValue;

            translateTween = DOTween.Sequence();
            translateTween.Append(_parent.DOAnchorPos(targetPosition, duration)
                .SetEase(easeIn)
                .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled));
            translateTween.Append(_parent.DOAnchorPos(initialAnchoredPosition, duration)
                .SetEase(easeOut)
                .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled));
        }

        private void PlayTargetPositionAnimation()
        {
            _parent.anchoredPosition = StartValue;
            translateTweener = _parent.DOAnchorPos(EndValue, duration)
                .SetEase(easeIn)
                .SetUpdate(timeScaleMod == TimeScaleMod.Unscaled);
        }
    }
}
