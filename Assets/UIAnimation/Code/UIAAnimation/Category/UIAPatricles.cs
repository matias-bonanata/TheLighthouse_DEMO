using UnityEngine;

namespace UIAnimation
{
    public class UIAPatricles : UIAnimation
    {
        protected ParticleSystem particleSystem;

        public override void Initialize(RectTransform parent)
        {
            base.Initialize(parent);
            particleSystem = _parent.GetComponent<ParticleSystem>();
        }
    }
}