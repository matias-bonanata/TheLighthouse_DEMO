using System.Collections.Generic;

namespace UIAnimation
{
    public class AnimationController : IAnimationStrategy
    {
        private List<UIAnimation> animations;

        public AnimationController(List<UIAnimation> animations)
        {
            this.animations = animations;
        }

        public void Play()
        {
            foreach (var animation in animations)
            {
                animation.Play();
            }
        }

        public void Stop()
        {
            foreach (var animation in animations)
            {
                animation.Stop();
            }
        }
    }
}