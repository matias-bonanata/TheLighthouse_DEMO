// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using Febucci.TextAnimatorForUnity;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This script replaces the normal continue button functionality with
    /// a two-stage process. If Text Animator's typewriter is still playing, it
    /// simply fast forwards to the end. Otherwise it sends OnContinue to the UI.
    /// </summary>
    public class TextAnimatorContinueButtonFastForward : MonoBehaviour
    {

        [Tooltip("Dialogue UI that the continue button affects.")]
        public StandardDialogueUI dialogueUI;

        [Tooltip("Text Animator Player to fast forward if it's not done playing.")]
        public TypewriterComponent textAnimatorPlayer;

        [Tooltip("Hide the continue button when continuing.")]
        public bool hideContinueButtonOnContinue = false;

        public enum CheckIfTypingMode
        {
            CheckAllLettersShown,
            CheckIsShowingText
        }

        [Tooltip("How to determine if Text Animator is still typing text.\n- Check All Letters Shown: If the text is still typing or appearance animations are still playing (as they might still hide a character until the very end).\nCheck Is Showing Text: Check the IsShowingText property to check if still typing even if appearance animations haven't finished yet.")]
        public CheckIfTypingMode checkIfTypingMode = CheckIfTypingMode.CheckAllLettersShown;

        protected UnityEngine.UI.Button continueButton;

        protected AbstractDialogueUI m_runtimeDialogueUI;
        protected virtual AbstractDialogueUI runtimeDialogueUI
        {
            get
            {
                if (dialogueUI != null) return dialogueUI;
                var panel = GetComponentInParent<StandardUISubtitlePanel>();
                if (panel != null) return panel.dialogueUI;
                else return GetComponentInParent<AbstractDialogueUI>() ?? DialogueManager.dialogueUI as AbstractDialogueUI;
            }
        }

        public virtual void Awake()
        {
            continueButton = GetComponent<UnityEngine.UI.Button>();
        }

        public virtual void OnFastForward()
        {
            var isStillTyping = (textAnimatorPlayer != null) &&
                (checkIfTypingMode == CheckIfTypingMode.CheckAllLettersShown)
                    ? !textAnimatorPlayer.TextAnimator.allLettersShown
                    : textAnimatorPlayer.IsShowingText;
            if (isStillTyping)
            {
                textAnimatorPlayer.SkipTypewriter();
            }
            else
            {
                if (hideContinueButtonOnContinue && continueButton != null) continueButton.gameObject.SetActive(false);
                if (runtimeDialogueUI != null) runtimeDialogueUI.OnContinue();
            }
        }

    }
}
