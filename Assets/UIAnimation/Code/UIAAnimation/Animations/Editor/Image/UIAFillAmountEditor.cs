using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    [CustomEditor(typeof(UIAFillAmount))]
    public class UIAFillAmountEditor : UIAnimationEditor
    {
        /// <summary>
        /// Draws the custom inspector for the UIAFillAmount.
        /// </summary>
        public override void OnInspectorGUI()
        {
            UIAFillAmount uiAFillAmount = (UIAFillAmount)target;
            serializedObject.Update();
            MonoScript script = MonoScript.FromScriptableObject(uiAFillAmount);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);

            // General settings
            showGeneralSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGeneralSettings, "General Settings");
            if (showGeneralSettings)
            {
                DrawGeneralSettings(uiAFillAmount);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Animation settings
            showAnimationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationSettings, "Animation Settings");
            if (showAnimationSettings)
            {
                DrawAnimationSettings(uiAFillAmount);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Randomization settings
            if (uiAFillAmount.Randomize)
            {
                showRandomizeSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showRandomizeSettings, "Randomize Settings");
                if (showRandomizeSettings)
                {
                    DrawRandomizeSettings(uiAFillAmount);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(uiAFillAmount);
            }
        }

        /// <summary>
        /// Draws the general settings section of the inspector.
        /// </summary>
        /// <param name="uiAFillAmount">The UIAFillAmount instance being edited.</param>
        private void DrawGeneralSettings(UIAFillAmount uiAFillAmount)
        {
            GUIContent currentModeTooltip = new GUIContent("Current Mode", "The current animation mode (Normal, PingPong, Step).");
            uiAFillAmount.CurrentMode = (UIAFillAmount.AnimationMode)EditorGUILayout.EnumPopup(currentModeTooltip, uiAFillAmount.CurrentMode);

            // GUIContent timeScaleModTooltip = new GUIContent("Time Scale Mod", "How the animation is affected by time scale.");
            // uiAFillAmount.timeScaleMod = (UIAnimation.TimeScaleMod)EditorGUILayout.EnumPopup(timeScaleModTooltip, uiAFillAmount.timeScaleMod);

            if (uiAFillAmount.CurrentMode == UIAFillAmount.AnimationMode.Normal
            || uiAFillAmount.CurrentMode == UIAFillAmount.AnimationMode.PingPong)
            {
                GUIContent randomizeTooltip = new GUIContent("Randomize", "Enable to randomize animation parameters.");
                uiAFillAmount.Randomize = EditorGUILayout.Toggle(randomizeTooltip, uiAFillAmount.Randomize);
            }

            GUIContent tweenTooltip = new GUIContent("Use Tween", "Enable to use tweening for animations.");
            uiAFillAmount.Tween = EditorGUILayout.Toggle(tweenTooltip, uiAFillAmount.Tween);
        }

        /// <summary>
        /// Draws the animation settings section of the inspector.
        /// </summary>
        /// <param name="uiAFillAmount">The UIAFillAmount instance being edited.</param>
        private void DrawAnimationSettings(UIAFillAmount uiAFillAmount)
        {
            if (uiAFillAmount.CurrentMode == UIAFillAmount.AnimationMode.Normal)
            {
                if (uiAFillAmount.Tween)
                {
                    GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the start of the animation.");
                    uiAFillAmount.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uiAFillAmount.easeIn);
                }
                else
                {
                    GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the start of the animation.");
                    uiAFillAmount.curveIn = EditorGUILayout.CurveField(curveInTooltip, uiAFillAmount.curveIn);
                }
            }
            else if (uiAFillAmount.CurrentMode == UIAFillAmount.AnimationMode.PingPong)
            {
                if (uiAFillAmount.Tween)
                {
                    GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the start of the animation.");
                    uiAFillAmount.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uiAFillAmount.easeIn);

                    GUIContent easeOutTooltip = new GUIContent("Ease Out", "The easing function for the end of the animation.");
                    uiAFillAmount.easeOut = (Ease)EditorGUILayout.EnumPopup(easeOutTooltip, uiAFillAmount.easeOut);
                }
                else
                {
                    GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the start of the animation.");
                    uiAFillAmount.curveIn = EditorGUILayout.CurveField(curveInTooltip, uiAFillAmount.curveIn);

                    GUIContent curveOutTooltip = new GUIContent("Curve Out", "The animation curve for the end of the animation.");
                    uiAFillAmount.curveOut = EditorGUILayout.CurveField(curveOutTooltip, uiAFillAmount.curveOut);
                }
            }
            else if (uiAFillAmount.CurrentMode == UIAFillAmount.AnimationMode.Step)
            {
                if (uiAFillAmount.Tween)
                {
                    GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the start of the animation.");
                    uiAFillAmount.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uiAFillAmount.easeIn);
                }
                else
                {
                    GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the start of the animation.");
                    uiAFillAmount.curveIn = EditorGUILayout.CurveField(curveInTooltip, uiAFillAmount.curveIn);
                }
            }

            if (!uiAFillAmount.Randomize)
            {
                if (uiAFillAmount.CurrentMode == UIAFillAmount.AnimationMode.Step)
                {
                    GUIContent stepPauseTimeTooltip = new GUIContent("Step Pause Time", "The pause time between each step in the animation.");
                    uiAFillAmount.StepPauseTime = EditorGUILayout.FloatField(stepPauseTimeTooltip, uiAFillAmount.StepPauseTime);

                    GUIContent stepsTooltip = new GUIContent("Steps", "The number of steps in the animation.");
                    uiAFillAmount.Steps = EditorGUILayout.IntField(stepsTooltip, uiAFillAmount.Steps);
                }
                else
                {
                    GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                    uiAFillAmount.duration = EditorGUILayout.FloatField(durationTooltip, uiAFillAmount.duration);
                }

                GUIContent startValueTooltip = new GUIContent("Start Value", "The starting fill amount value (0 to 1).");
                uiAFillAmount.StartValue = EditorGUILayout.Slider(startValueTooltip, uiAFillAmount.StartValue, 0f, 1f);

                GUIContent endValueTooltip = new GUIContent("End Value", "The ending fill amount value (0 to 1).");
                uiAFillAmount.EndValue = EditorGUILayout.Slider(endValueTooltip, uiAFillAmount.EndValue, 0f, 1f);
            }
        }

        /// <summary>
        /// Draws the randomization settings section of the inspector.
        /// </summary>
        /// <param name="uiAFillAmount">The UIAFillAmount instance being edited.</param>
        private void DrawRandomizeSettings(UIAFillAmount uiAFillAmount)
        {
            if (uiAFillAmount.CurrentMode != UIAFillAmount.AnimationMode.Step)
            {
                GUIContent minDurationTooltip = new GUIContent("Min Duration", "The minimum duration of the animation in seconds.");
                uiAFillAmount.minDuration = EditorGUILayout.FloatField(minDurationTooltip, uiAFillAmount.minDuration);

                GUIContent maxDurationTooltip = new GUIContent("Max Duration", "The maximum duration of the animation in seconds.");
                uiAFillAmount.maxDuration = EditorGUILayout.FloatField(maxDurationTooltip, uiAFillAmount.maxDuration);
            }

            GUIContent minStartValueTooltip = new GUIContent("Min Start Value", "The minimum starting fill amount value (0 to 1).");
            uiAFillAmount.minStartValue = EditorGUILayout.Slider(minStartValueTooltip, uiAFillAmount.minStartValue, 0f, 1f);

            GUIContent maxStartValueTooltip = new GUIContent("Max Start Value", "The maximum starting fill amount value (0 to 1).");
            uiAFillAmount.maxStartValue = EditorGUILayout.Slider(maxStartValueTooltip, uiAFillAmount.maxStartValue, 0f, 1f);

            GUIContent minEndValueTooltip = new GUIContent("Min End Value", "The minimum ending fill amount value (0 to 1).");
            uiAFillAmount.minEndValue = EditorGUILayout.Slider(minEndValueTooltip, uiAFillAmount.minEndValue, 0f, 1f);

            GUIContent maxEndValueTooltip = new GUIContent("Max End Value", "The maximum ending fill amount value (0 to 1).");
            uiAFillAmount.maxEndValue = EditorGUILayout.Slider(maxEndValueTooltip, uiAFillAmount.maxEndValue, 0f, 1f);
        }
    }
}