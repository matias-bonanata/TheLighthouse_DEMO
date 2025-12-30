using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    [CustomEditor(typeof(UIACanvasGroup))]
    public class UIACanvasGroupEditor : UIAnimationEditor
    {
        /// <summary>
        /// Draws the custom inspector for the UIACanvasGroup.
        /// </summary>
        public override void OnInspectorGUI()
        {
            UIACanvasGroup uiACanvasGroup = (UIACanvasGroup)target;
            MonoScript script = MonoScript.FromScriptableObject(uiACanvasGroup);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);

            // General settings
            showGeneralSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGeneralSettings, "General Settings");
            if (showGeneralSettings)
            {
                DrawGeneralSettings(uiACanvasGroup);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Animation settings
            showAnimationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationSettings, "Animation Settings");
            if (showAnimationSettings)
            {
                DrawAnimationSettings(uiACanvasGroup);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Randomization settings
            if (uiACanvasGroup.Randomize)
            {
                showRandomizeSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showRandomizeSettings, "Randomize Settings");
                if (showRandomizeSettings)
                {
                    DrawRandomizeSettings(uiACanvasGroup);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(uiACanvasGroup);
            }
        }

        /// <summary>
        /// Draws the general settings section of the inspector.
        /// </summary>
        /// <param name="uiACanvasGroup">The UIACanvasGroup instance being edited.</param>
        private void DrawGeneralSettings(UIACanvasGroup uiACanvasGroup)
        {
            GUIContent currentModeTooltip = new GUIContent("Current Mode", "The current animation mode (FadeIn, FadeOut, Blink, Pulse).");
            uiACanvasGroup.CurrentMode = (UIACanvasGroup.AnimationMode)EditorGUILayout.EnumPopup(currentModeTooltip, uiACanvasGroup.CurrentMode);

            // GUIContent timeScaleModTooltip = new GUIContent("Time Scale Mod", "How the animation is affected by time scale.");
            // uiACanvasGroup.timeScaleMod = (UIAnimation.TimeScaleMod)EditorGUILayout.EnumPopup(timeScaleModTooltip, uiACanvasGroup.timeScaleMod);

            GUIContent randomizeTooltip = new GUIContent("Randomize", "Enable to randomize animation parameters.");
            uiACanvasGroup.Randomize = EditorGUILayout.Toggle(randomizeTooltip, uiACanvasGroup.Randomize);

            GUIContent tweenTooltip = new GUIContent("Use Tween", "Enable to use tweening for animations.");
            uiACanvasGroup.Tween = EditorGUILayout.Toggle(tweenTooltip, uiACanvasGroup.Tween);
        }

        /// <summary>
        /// Draws the animation settings section of the inspector.
        /// </summary>
        /// <param name="uiACanvasGroup">The UIACanvasGroup instance being edited.</param>
        private void DrawAnimationSettings(UIACanvasGroup uiACanvasGroup)
        {
            if (uiACanvasGroup.CurrentMode == UIACanvasGroup.AnimationMode.FadeIn)
            {
                if (uiACanvasGroup.Tween)
                {
                    GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the fade-in animation.");
                    uiACanvasGroup.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uiACanvasGroup.easeIn);
                }
                else
                {
                    GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the fade-in animation.");
                    uiACanvasGroup.curveIn = EditorGUILayout.CurveField(curveInTooltip, uiACanvasGroup.curveIn);
                }
            }
            else if (uiACanvasGroup.CurrentMode == UIACanvasGroup.AnimationMode.FadeOut)
            {
                if (uiACanvasGroup.Tween)
                {
                    GUIContent easeOutTooltip = new GUIContent("Ease Out", "The easing function for the fade-out animation.");
                    uiACanvasGroup.easeOut = (Ease)EditorGUILayout.EnumPopup(easeOutTooltip, uiACanvasGroup.easeOut);
                }
                else
                {
                    GUIContent curveOutTooltip = new GUIContent("Curve Out", "The animation curve for the fade-out animation.");
                    uiACanvasGroup.curveOut = EditorGUILayout.CurveField(curveOutTooltip, uiACanvasGroup.curveOut);
                }
            }
            else if (uiACanvasGroup.CurrentMode == UIACanvasGroup.AnimationMode.Blink
            || uiACanvasGroup.CurrentMode == UIACanvasGroup.AnimationMode.Pulse)
            {
                if (uiACanvasGroup.Tween)
                {
                    GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the animation's start.");
                    uiACanvasGroup.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uiACanvasGroup.easeIn);

                    GUIContent easeOutTooltip = new GUIContent("Ease Out", "The easing function for the animation's end.");
                    uiACanvasGroup.easeOut = (Ease)EditorGUILayout.EnumPopup(easeOutTooltip, uiACanvasGroup.easeOut);
                }
                else
                {
                    GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the animation's start.");
                    uiACanvasGroup.curveIn = EditorGUILayout.CurveField(curveInTooltip, uiACanvasGroup.curveIn);

                    GUIContent curveOutTooltip = new GUIContent("Curve Out", "The animation curve for the animation's end.");
                    uiACanvasGroup.curveOut = EditorGUILayout.CurveField(curveOutTooltip, uiACanvasGroup.curveOut);
                }
            }

            if (!uiACanvasGroup.Randomize)
            {
                GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                uiACanvasGroup.duration = EditorGUILayout.FloatField(durationTooltip, uiACanvasGroup.duration);

                GUIContent initializeStartValueTooltip = new GUIContent("Initialize Start Value", "Whether to initialize the start value of the animation.");
                uiACanvasGroup.InitializeStartVelue = EditorGUILayout.Toggle(initializeStartValueTooltip, uiACanvasGroup.InitializeStartVelue);

                GUIContent startValueTooltip = new GUIContent("Start Value", "The starting value of the animation (0 to 1).");
                uiACanvasGroup.StartValue = EditorGUILayout.Slider(startValueTooltip, uiACanvasGroup.StartValue, 0f, 1f);

                GUIContent endValueTooltip = new GUIContent("End Value", "The ending value of the animation (0 to 1).");
                uiACanvasGroup.EndValue = EditorGUILayout.Slider(endValueTooltip, uiACanvasGroup.EndValue, 0f, 1f);
            }
            else
            {
                GUIContent initializeStartValueTooltip = new GUIContent("Initialize Start Value", "Whether to initialize the start value of the animation.");
                uiACanvasGroup.InitializeStartVelue = EditorGUILayout.Toggle(initializeStartValueTooltip, uiACanvasGroup.InitializeStartVelue);
            }
        }

        /// <summary>
        /// Draws the randomization settings section of the inspector.
        /// </summary>
        /// <param name="uiACanvasGroup">The UIACanvasGroup instance being edited.</param>
        private void DrawRandomizeSettings(UIACanvasGroup uiACanvasGroup)
        {
            GUIContent minDurationTooltip = new GUIContent("Min Duration", "The minimum duration of the animation in seconds.");
            uiACanvasGroup.minDuration = EditorGUILayout.FloatField(minDurationTooltip, uiACanvasGroup.minDuration);

            GUIContent maxDurationTooltip = new GUIContent("Max Duration", "The maximum duration of the animation in seconds.");
            uiACanvasGroup.maxDuration = EditorGUILayout.FloatField(maxDurationTooltip, uiACanvasGroup.maxDuration);

            GUIContent minStartValueTooltip = new GUIContent("Min Start Value", "The minimum starting value of the animation (0 to 1).");
            uiACanvasGroup.minStartVelue = EditorGUILayout.Slider(minStartValueTooltip, uiACanvasGroup.minStartVelue, 0f, 1f);

            GUIContent maxStartValueTooltip = new GUIContent("Max Start Value", "The maximum starting value of the animation (0 to 1).");
            uiACanvasGroup.maxStartVelue = EditorGUILayout.Slider(maxStartValueTooltip, uiACanvasGroup.maxStartVelue, 0f, 1f);

            GUIContent minEndValueTooltip = new GUIContent("Min End Value", "The minimum ending value of the animation (0 to 1).");
            uiACanvasGroup.minEndValue = EditorGUILayout.Slider(minEndValueTooltip, uiACanvasGroup.minEndValue, 0f, 1f);

            GUIContent maxEndValueTooltip = new GUIContent("Max End Value", "The maximum ending value of the animation (0 to 1).");
            uiACanvasGroup.maxEndValue = EditorGUILayout.Slider(maxEndValueTooltip, uiACanvasGroup.maxEndValue, 0f, 1f);
        }
    }
}