using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    [CustomEditor(typeof(UIATranslate))]
    public class UIATranslateEditor : UIAnimationEditor
    {
        /// <summary>
        /// Draws the custom inspector for the UIATranslate.
        /// </summary>
        public override void OnInspectorGUI()
        {
            UIATranslate uIATranslate = (UIATranslate)target;
            MonoScript script = MonoScript.FromScriptableObject(uIATranslate);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);

            // General settings
            showGeneralSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGeneralSettings, "General Settings");
            if (showGeneralSettings)
            {
                DrawGeneralSettings(uIATranslate);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Animation settings
            showAnimationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationSettings, "Animation Settings");
            if (showAnimationSettings)
            {
                DrawAnimationSettings(uIATranslate);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            // Randomization settings
            if (uIATranslate.Randomize)
            {
                showRandomizeSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showRandomizeSettings, "Randomize Settings");
                if (showRandomizeSettings)
                {
                    DrawRandomizeSettings(uIATranslate);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(uIATranslate);
            }
        }

        /// <summary>
        /// Draws the general settings section of the inspector.
        /// </summary>
        /// <param name="uIATranslate">The UIATranslate instance being edited.</param>
        private void DrawGeneralSettings(UIATranslate uIATranslate)
        {
            GUIContent currentModeTooltip = new GUIContent("Current Mode", "The current animation mode (e.g., PingPong, TargetPosition).");
            uIATranslate.CurrentMode = (UIATranslate.AnimationMode)EditorGUILayout.EnumPopup(currentModeTooltip, uIATranslate.CurrentMode);

            // GUIContent timeScaleModTooltip = new GUIContent("Time Scale Mod", "How the animation is affected by time scale.");
            // uIATranslate.timeScaleMod = (UIAnimation.TimeScaleMod)EditorGUILayout.EnumPopup(timeScaleModTooltip, uIATranslate.timeScaleMod);

            GUIContent randomizeTooltip = new GUIContent("Randomize", "Enable to randomize animation parameters.");
            uIATranslate.Randomize = EditorGUILayout.Toggle(randomizeTooltip, uIATranslate.Randomize);

            GUIContent tweenTooltip = new GUIContent("Use Tween", "Enable to use tweening for animations.");
            uIATranslate.Tween = EditorGUILayout.Toggle(tweenTooltip, uIATranslate.Tween);
        }

        /// <summary>
        /// Draws the animation settings section of the inspector.
        /// </summary>
        /// <param name="uIATranslate">The UIATranslate instance being edited.</param>
        private void DrawAnimationSettings(UIATranslate uIATranslate)
        {
            if (uIATranslate.CurrentMode == UIATranslate.AnimationMode.PingPong)
            {
                if (uIATranslate.Tween)
                {
                    GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the start of the animation.");
                    uIATranslate.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uIATranslate.easeIn);

                    GUIContent easeOutTooltip = new GUIContent("Ease Out", "The easing function for the end of the animation.");
                    uIATranslate.easeOut = (Ease)EditorGUILayout.EnumPopup(easeOutTooltip, uIATranslate.easeOut);
                }
                else
                {
                    GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the start of the animation.");
                    uIATranslate.curveIn = EditorGUILayout.CurveField(curveInTooltip, uIATranslate.curveIn);

                    GUIContent curveOutTooltip = new GUIContent("Curve Out", "The animation curve for the end of the animation.");
                    uIATranslate.curveOut = EditorGUILayout.CurveField(curveOutTooltip, uIATranslate.curveOut);
                }

                if (!uIATranslate.Randomize)
                {
                    GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                    uIATranslate.duration = EditorGUILayout.FloatField(durationTooltip, uIATranslate.duration);

                    GUIContent endValueTooltip = new GUIContent("End Value", "The target position values for the animation.");
                    uIATranslate.EndValue = EditorGUILayout.Vector3Field(endValueTooltip, uIATranslate.EndValue);
                }
            }
            else if (uIATranslate.CurrentMode == UIATranslate.AnimationMode.TargetPosition)
            {
                if (uIATranslate.Tween)
                {
                    GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the start of the animation.");
                    uIATranslate.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uIATranslate.easeIn);
                }
                else
                {
                    GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the start of the animation.");
                    uIATranslate.curveIn = EditorGUILayout.CurveField(curveInTooltip, uIATranslate.curveIn);
                }

                if (!uIATranslate.Randomize)
                {
                    GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                    uIATranslate.duration = EditorGUILayout.FloatField(durationTooltip, uIATranslate.duration);

                    GUIContent AutoStartPositionTooltip = new GUIContent("Auto Start Position", "Initializes the position as the starting position of the position on which you are located");
                    uIATranslate.AutoStartPosition = EditorGUILayout.Toggle(AutoStartPositionTooltip, uIATranslate.AutoStartPosition);

                    if (!uIATranslate.AutoStartPosition)
                    {
                        GUIContent initializeStartPositionTooltip = new GUIContent("Initialize Start Position", "Initialize the start position when the game starts. If true, StartValue will be applied when the game starts. If false, it will be applied when the animation starts.");
                        uIATranslate.InitializeStartPosition = EditorGUILayout.Toggle(initializeStartPositionTooltip, uIATranslate.InitializeStartPosition);

                        GUIContent startValueTooltip = new GUIContent("Start Value", "The starting position values for the animation.");
                        uIATranslate.StartValue = EditorGUILayout.Vector3Field(startValueTooltip, uIATranslate.StartValue);
                    }


                    GUIContent endValueTooltip = new GUIContent("End Value", "The target position values for the animation.");
                    uIATranslate.EndValue = EditorGUILayout.Vector3Field(endValueTooltip, uIATranslate.EndValue);
                }
                else
                {
                    GUIContent startValueTooltip = new GUIContent("Start Value", "The starting position values for the animation.");
                    uIATranslate.StartValue = EditorGUILayout.Vector3Field(startValueTooltip, uIATranslate.StartValue);
                }
            }
        }

        /// <summary>
        /// Draws the randomization settings section of the inspector.
        /// </summary>
        /// <param name="uIATranslate">The UIATranslate instance being edited.</param>
        private void DrawRandomizeSettings(UIATranslate uIATranslate)
        {
            GUIContent minDurationTooltip = new GUIContent("Min Duration", "The minimum duration of the animation in seconds.");
            uIATranslate.minDuration = EditorGUILayout.FloatField(minDurationTooltip, uIATranslate.minDuration);

            GUIContent maxDurationTooltip = new GUIContent("Max Duration", "The maximum duration of the animation in seconds.");
            uIATranslate.maxDuration = EditorGUILayout.FloatField(maxDurationTooltip, uIATranslate.maxDuration);

            if (uIATranslate.CurrentMode == UIATranslate.AnimationMode.PingPong || uIATranslate.CurrentMode == UIATranslate.AnimationMode.TargetPosition)
            {
                GUIContent minEndValueTooltip = new GUIContent("Min End Value", "The minimum target position values for the animation.");
                uIATranslate.minEndValue = EditorGUILayout.Vector3Field(minEndValueTooltip, uIATranslate.minEndValue);

                GUIContent maxEndValueTooltip = new GUIContent("Max End Value", "The maximum target position values for the animation.");
                uIATranslate.maxEndValue = EditorGUILayout.Vector3Field(maxEndValueTooltip, uIATranslate.maxEndValue);
            }
        }
    }
}