// Copyright (c) Pixel Crushers. All rights reserved.

using System.Text.RegularExpressions;
using UnityEngine;
using Febucci.TextAnimatorForUnity;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Add this to your dialogue UI if you want the sequencer {{end}} keyword to
    /// account for Text Animator typing time.
    /// </summary>
    public class TextAnimatorEndKeyword : MonoBehaviour
    {

        public TypingsTimingsScriptableBase timings;

        protected virtual void Awake()
        {
            if (timings == null)
            {
                Debug.LogError($"Dialogue System: You must assign a typewriter timings asset to {GetType().Name}.", this);
            }
            else
            {
                ConversationView.overrideGetDefaultSubtitleDuration = GetTextAnimatorSubtitleDuration;
            }
        }

        protected virtual float GetTextAnimatorSubtitleDuration(string text)
        {
            // Remove markup tags:
            var cleanText = text.Contains('<')
                ? Regex.Replace(text, @"<[^> ]+>", string.Empty)
                : text;

            // Since we haven't populated the TextAnimator yet, we can't use
            // timings.GetWaitAppearanceTimeOf(characterData, textAnimator).
            // Instead, we manually compute according to the timings asset.
            if (timings is TypingDelaysByCharacter timingsByCharacter)
            {
                int numMiddle = 0;
                int numLong = 0;
                for (int i = 0; i < cleanText.Length; i++)
                {
                    char c = cleanText[i];
                    var isMiddleLengthChar = c == ';' || c == ':' || c == ')' || c == '-' || c == ',';
                    var isLongChar = c == '!' || c == '?' || c == '.';
                    if (isMiddleLengthChar) numMiddle++;
                    else if (isLongChar) numLong++;
                }
                int numNormal = cleanText.Length - (numMiddle + numLong);
                return 
                    (timingsByCharacter.waitForNormalChars * numNormal) +
                    (timingsByCharacter.waitMiddle * numMiddle) +
                    (timingsByCharacter.waitLong * numLong);
            }
            else if (timings is TypingDelaysByWord timingByWord)
            {
                int numNormalWords = 0;
                int numPunctuationWords = 0;
                char prevChar = ' ';
                for (int i = 0; i < cleanText.Length; i++)
                {
                    char c = cleanText[i];
                    var isEndOfNormalWord = c == ' ' && prevChar != ' ';
                    var isEndOfPunctuationWord = char.IsPunctuation(c) && char.IsLetterOrDigit(prevChar);
                    if (isEndOfNormalWord) numNormalWords++;
                    else if (isEndOfPunctuationWord) numPunctuationWords++;
                    prevChar = c;
                }
                return
                    (timingByWord.waitForNormalWord * numNormalWords) +
                    (timingByWord.waitForWordWithPunctuation * numPunctuationWords);
            }
            else
            {
                ConversationView.overrideGetDefaultSubtitleDuration = null;
                var duration = ConversationView.GetDefaultSubtitleDurationInSeconds(cleanText);
                ConversationView.overrideGetDefaultSubtitleDuration = GetTextAnimatorSubtitleDuration;
                return duration;
            }
        }

    }
}
