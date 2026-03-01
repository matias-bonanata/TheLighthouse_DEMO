// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Febucci.TextAnimatorForUnity;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This component auto-scrolls Text Animator text in a Scroll Rect
    /// as it types out.
    /// </summary>
    [RequireComponent(typeof(TypewriterComponent))]
    [RequireComponent(typeof(LayoutElement))]
    public class TextAnimatorAutoScroll : MonoBehaviour
    {

        [SerializeField] private ScrollRect scrollRect;

        private TextMeshProUGUI tmp;
        private TypewriterComponent typewriter;
        private LayoutElement layoutElement;

        private void Awake()
        {
            tmp = GetComponent<TextMeshProUGUI>();
            typewriter = GetComponent<TypewriterComponent>();
            layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            if (tmp == null || typewriter == null || scrollRect == null || layoutElement == null)
            {
                Debug.LogWarning($"{GetType().Name}: ScrollRect, TextMeshProUGUI, or Typewriter not found. Disabling auto scroll.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!typewriter.IsShowingText) return;
            tmp.ForceMeshUpdate();
            layoutElement.preferredHeight = Mathf.Max(0, tmp.textBounds.size.y);
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }

}
