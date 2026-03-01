// Copyright (c) Pixel Crushers. All rights reserved.

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Febucci.TextAnimatorForUnity;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using Febucci.TextAnimatorCore.Typing;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Use this subclass of StandardUISubtitlePanel if any of these are true:
    /// - Your subtitle panel uses Text Animator and Accumulate Text is ticked.
    /// - You want to the sequencer to receive "Typed" messages when Text Animator finishes typing.
    /// </summary>
    public class TextAnimatorSubtitlePanel : StandardUISubtitlePanel
    {
        public bool clearTextOnOpen = false;

        private TextAnimator_TMP textAnimator;
        private TypewriterComponent typewriter;

        protected override void Start()
        {
            base.Start();
            textAnimator = subtitleText.gameObject.GetComponent<TextAnimator_TMP>();
            typewriter = subtitleText.gameObject.GetComponent<TypewriterComponent>();
            if (typewriter != null)
            {
                typewriter.onTextShowed.AddListener(OnTextShowed);
            }
        }

        public override void Open()
        {
            if (!isOpen && clearTextOnOpen) ClearText();
            base.Open();
        }

        protected override void SetFormattedText(UITextField textField, string previousText, Subtitle subtitle)
        {
            if (textAnimator == null) textAnimator = subtitleText.gameObject.GetComponent<TextAnimator_TMP>();
            if (typewriter == null) typewriter = subtitleText.gameObject.GetComponent<TypewriterComponent>();
            if (textAnimator == null)
            {
                base.SetFormattedText(textField, previousText, subtitle);
                return;
            }
            
            //textAnimator.typewriterStartsAutomatically = false;
            var currentText = UITools.GetUIFormattedText(subtitle.formattedText);

            if (accumulateText && !string.IsNullOrEmpty(subtitle.formattedText.text))
            {
                if (numAccumulatedLines < maxLines)
                {
                    numAccumulatedLines += (1 + NumCharOccurrences('\n', currentText));
                }
                else
                {
                    // If we're at the max number of lines, remove the first line from the accumulated text:
                    previousText = RemoveFirstLine(previousText);
                    textAnimator.SetText(previousText, hideText: true);
                }
                textAnimator.AppendText(currentText, hideText: true);
            }
            else
            {
                textAnimator.SetText(currentText, true);
            }

            if (typewriter != null && typewriter.enabled)
            {
                typewriter.StartShowingText();
            }
            UITools.SendTextChangeMessage(textField);
            if (!haveSavedOriginalColor)
            {
                originalColor = textField.color;
                haveSavedOriginalColor = true;
            }
            textField.color = (subtitle.formattedText.emphases != null && subtitle.formattedText.emphases.Length > 0) ? subtitle.formattedText.emphases[0].color : originalColor;
        }

        protected void OnTextShowed()
        {
            Sequencer.Message(SequencerMessages.Typed);
        }

    }
}
