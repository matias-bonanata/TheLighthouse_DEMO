using System;
using UnityEngine;
using UnityEngine.UI;

namespace UIAnimation
{
    [Serializable]
    public class AnimationObjectData
    {
        [Serializable]
        public class AnimationList
        {
            public UIAImage uIAImage;
            public UIARectTransform uIARectTransform;
            public UIACanvas uIACanvas;
            public UIAUnityAnimation uIAUnityAnimation;
            public UIAPatricles uIAPatricles;
        }

        [Serializable]
        public struct ObjectList
        {
            public RectTransform rectTransform;
            public Image image;
            public CanvasGroup canvasGroup;
            public Animator animator;
            public ParticleSystem particleSystem;
        }
        public string animationName = "Animation";
        public InteractiveObjectData interactiveObjectData;
        public ObjectList objectList;
        public AnimationList animationList;

        public AnimationObjectData()
        {
            animationList = new AnimationList();
        }
    }
}