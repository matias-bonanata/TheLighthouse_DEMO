using UnityEngine;
using DG.Tweening;

namespace UIAnimation
{
    [CreateAssetMenu(fileName = "New Active Object Animation", menuName = "UI Animations/RectTransform/ActiveObject")]
    public class UIAActiveObject : UIARectTransform
    {
        public enum ActiveStatus
        {
            Show,
            Hide
        }

        [Tooltip("The current status of the object.")]
        public ActiveStatus Status = ActiveStatus.Show; // Default status is Show

        [Tooltip("The delay in seconds before changing the active state.")]
        public float Delay = 0f;

        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);
        }

        public override void Play()
        {
            base.Play();
            DOVirtual.DelayedCall(Delay, () => _parent.gameObject.SetActive(Status == ActiveStatus.Show));
        }

        public override void Stop()
        {
            base.Stop();
        }
    }
}
