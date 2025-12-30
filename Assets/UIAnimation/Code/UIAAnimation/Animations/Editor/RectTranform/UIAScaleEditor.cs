using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    [CustomEditor(typeof(UIAScale))]
    public class UIAScaleEditor : UIAnimationEditor
    {
        /// <summary>
        /// Draws the custom inspector for the UIAScale.
        /// </summary>
        public override void OnInspectorGUI()
        {
            UIAScale uIAScale = (UIAScale)target;
            MonoScript script = MonoScript.FromScriptableObject(uIAScale);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);

            // General settings
            showGeneralSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGeneralSettings, "General Settings");
            if (showGeneralSettings)
            {
                DrawGeneralSettings(uIAScale);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Animation settings
            showAnimationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationSettings, "Animation Settings");
            if (showAnimationSettings)
            {
                DrawAnimationSettings(uIAScale);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Randomization settings
            if (uIAScale.Randomize)
            {
                showRandomizeSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showRandomizeSettings, "Randomize Settings");
                if (showRandomizeSettings)
                {
                    DrawRandomizeSettings(uIAScale);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(uIAScale);
            }
        }

        /// <summary>
        /// Draws the general settings section of the inspector.
        /// </summary>
        /// <param name="uIAScale">The UIAScale instance being edited.</param>
        private void DrawGeneralSettings(UIAScale uIAScale)
        {
            GUIContent currentModeTooltip = new GUIContent("Current Mode", "The current animation mode (e.g., PingPong, TargetSize).");
            uIAScale.CurrentMode = (UIAScale.AnimationMode)EditorGUILayout.EnumPopup(currentModeTooltip, uIAScale.CurrentMode);

            // GUIContent timeScaleModTooltip = new GUIContent("Time Scale Mod", "How the animation is affected by time scale.");
            // uIAScale.timeScaleMod = (UIAnimation.TimeScaleMod)EditorGUILayout.EnumPopup(timeScaleModTooltip, uIAScale.timeScaleMod);

            GUIContent randomizeTooltip = new GUIContent("Randomize", "Enable to randomize animation parameters.");
            uIAScale.Randomize = EditorGUILayout.Toggle(randomizeTooltip, uIAScale.Randomize);

            GUIContent tweenTooltip = new GUIContent("Use Tween", "Enable to use tweening for animations.");
            uIAScale.Tween = EditorGUILayout.Toggle(tweenTooltip, uIAScale.Tween);

            if (uIAScale.CurrentMode == UIAScale.AnimationMode.TargetSize)
            {
                GUIContent advancedTooltip = new GUIContent("Advanced", "Enable advanced settings for the TargetSize animation mode.");
                uIAScale.Advanced = EditorGUILayout.Toggle(advancedTooltip, uIAScale.Advanced);
            }
        }

        /// <summary>
        /// Draws the animation settings section of the inspector.
        /// </summary>
        /// <param name="uIAScale">The UIAScale instance being edited.</param>
        private void DrawAnimationSettings(UIAScale uIAScale)
        {
            if (uIAScale.Tween)
            {
                GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the start of the animation.");
                uIAScale.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uIAScale.easeIn);

                GUIContent easeOutTooltip = new GUIContent("Ease Out", "The easing function for the end of the animation.");
                uIAScale.easeOut = (Ease)EditorGUILayout.EnumPopup(easeOutTooltip, uIAScale.easeOut);
            }
            else
            {
                GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the start of the animation.");
                uIAScale.curveIn = EditorGUILayout.CurveField(curveInTooltip, uIAScale.curveIn);

                GUIContent curveOutTooltip = new GUIContent("Curve Out", "The animation curve for the end of the animation.");
                uIAScale.curveOut = EditorGUILayout.CurveField(curveOutTooltip, uIAScale.curveOut);
            }

            if (uIAScale.CurrentMode == UIAScale.AnimationMode.PingPong)
            {
                if (!uIAScale.Randomize)
                {
                    GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                    uIAScale.duration = EditorGUILayout.FloatField(durationTooltip, uIAScale.duration);

                    GUIContent endValueTooltip = new GUIContent("End Value", "The target scale values for the animation.");
                    uIAScale.EndValue = EditorGUILayout.Vector3Field(endValueTooltip, uIAScale.EndValue);
                }
            }
            else if (uIAScale.CurrentMode == UIAScale.AnimationMode.TargetSize)
            {
                if (!uIAScale.Randomize)
                {
                    GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                    uIAScale.duration = EditorGUILayout.FloatField(durationTooltip, uIAScale.duration);

                    GUIContent autoStartValueTooltip = new GUIContent("Auto Start Value", "Automatically determine the start value based on the current scale.");
                    uIAScale.AutoStartVelue = EditorGUILayout.Toggle(autoStartValueTooltip, uIAScale.AutoStartVelue);

                    if (!uIAScale.Advanced)
                    {
                        if (!uIAScale.AutoStartVelue)
                        {
                            GUIContent startValueTooltip = new GUIContent("Start Value", "The starting scale values for the animation.");
                            uIAScale.StartValue = EditorGUILayout.Vector3Field(startValueTooltip, uIAScale.StartValue);
                        }

                        GUIContent endValueTooltip = new GUIContent("End Value", "The target scale values for the animation.");
                        uIAScale.EndValue = EditorGUILayout.Vector3Field(endValueTooltip, uIAScale.EndValue);
                    }
                    else
                    {
                        if (!uIAScale.AutoStartVelue)
                        {
                            GUIContent startValueTooltip = new GUIContent("Start Value", "The starting scale values for the animation.");
                            uIAScale.StartValue = EditorGUILayout.Vector3Field(startValueTooltip, uIAScale.StartValue);
                        }

                        GUIContent middleValueTooltip = new GUIContent("Middle Value", "The intermediate scale values for the animation.");
                        uIAScale.MiddleValue = EditorGUILayout.Vector3Field(middleValueTooltip, uIAScale.MiddleValue);

                        GUIContent endValueTooltip = new GUIContent("End Value", "The target scale values for the animation.");
                        uIAScale.EndValue = EditorGUILayout.Vector3Field(endValueTooltip, uIAScale.EndValue);
                    }
                }
                else
                {
                    if (!uIAScale.AutoStartVelue)
                    {
                        GUIContent startValueTooltip = new GUIContent("Start Value", "The starting scale values for the animation.");
                        uIAScale.StartValue = EditorGUILayout.Vector3Field(startValueTooltip, uIAScale.StartValue);
                    }

                    GUIContent middleValueTooltip = new GUIContent("Middle Value", "The intermediate scale values for the animation.");
                    uIAScale.MiddleValue = EditorGUILayout.Vector3Field(middleValueTooltip, uIAScale.MiddleValue);
                }
            }
        }

        /// <summary>
        /// Draws the randomization settings section of the inspector.
        /// </summary>
        /// <param name="uIAScale">The UIAScale instance being edited.</param>
        private void DrawRandomizeSettings(UIAScale uIAScale)
        {
            GUIContent minDurationTooltip = new GUIContent("Min Duration", "The minimum duration of the animation in seconds.");
            uIAScale.minDuration = EditorGUILayout.FloatField(minDurationTooltip, uIAScale.minDuration);

            GUIContent maxDurationTooltip = new GUIContent("Max Duration", "The maximum duration of the animation in seconds.");
            uIAScale.maxDuration = EditorGUILayout.FloatField(maxDurationTooltip, uIAScale.maxDuration);

            if (uIAScale.CurrentMode == UIAScale.AnimationMode.PingPong)
            {
                GUIContent minEndValueTooltip = new GUIContent("Min End Value", "The minimum target scale values for the animation.");
                uIAScale.minEndValue = EditorGUILayout.Vector3Field(minEndValueTooltip, uIAScale.minEndValue);

                GUIContent maxEndValueTooltip = new GUIContent("Max End Value", "The maximum target scale values for the animation.");
                uIAScale.maxEndValue = EditorGUILayout.Vector3Field(maxEndValueTooltip, uIAScale.maxEndValue);
            }
            else if (uIAScale.CurrentMode == UIAScale.AnimationMode.TargetSize)
            {
                GUIContent minEndValueTooltip = new GUIContent("Min End Value", "The minimum target scale values for the animation.");
                uIAScale.minEndValue = EditorGUILayout.Vector3Field(minEndValueTooltip, uIAScale.minEndValue);

                GUIContent maxEndValueTooltip = new GUIContent("Max End Value", "The maximum target scale values for the animation.");
                uIAScale.maxEndValue = EditorGUILayout.Vector3Field(maxEndValueTooltip, uIAScale.maxEndValue);
            }
        }
    }
}