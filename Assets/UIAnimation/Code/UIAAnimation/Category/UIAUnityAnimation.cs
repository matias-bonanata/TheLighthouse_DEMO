using UnityEngine;

namespace UIAnimation
{
    public class UIAUnityAnimation : UIAnimation
    {
        protected Animator animator;
        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);
            animator = _parent.GetComponent<Animator>();
        }
    }
}