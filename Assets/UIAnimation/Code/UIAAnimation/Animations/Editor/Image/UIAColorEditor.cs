using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    [CustomEditor(typeof(UIAColor))]
    public class UIAColorEditor : UIAnimationEditor
    {
        /// <summary>
        /// Draws the custom inspector for the UIAColor.
        /// </summary>
        public override void OnInspectorGUI()
        {
            UIAColor uiAColor = (UIAColor)target;
            serializedObject.Update();
            MonoScript script = MonoScript.FromScriptableObject(uiAColor);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);

            // General settings
            showGeneralSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGeneralSettings, "General Settings");
            if (showGeneralSettings)
            {
                DrawGeneralSettings(uiAColor);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Animation settings
            showAnimationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationSettings, "Animation Settings");
            if (showAnimationSettings)
            {
                DrawAnimationSettings(uiAColor);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Randomization settings
            if (uiAColor.Randomize)
            {
                showRandomizeSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showRandomizeSettings, "Randomize Settings");
                if (showRandomizeSettings)
                {
                    DrawRandomizeSettings(uiAColor);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            // Display the RandomColor array if ManualRandomColour is enabled
            if (uiAColor.CurrentMode == UIAColor.AnimationMode.RandomColor)
            {
                if (uiAColor.ManualRandomColour)
                {
                    SerializedProperty randomColorProperty = serializedObject.FindProperty("RandomColor");
                    GUIContent randomColorsTooltip = new GUIContent("Random Colors", "The list of colors to randomly choose from.");
                    EditorGUILayout.PropertyField(randomColorProperty, randomColorsTooltip, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(uiAColor);
            }
        }

        /// <summary>
        /// Draws the general settings section of the inspector.
        /// </summary>
        /// <param name="uiAColor">The UIAColor instance being edited.</param>
        private void DrawGeneralSettings(UIAColor uiAColor)
        {
            GUIContent currentModeTooltip = new GUIContent("Current Mode", "The current animation mode (FadeIn, FadeOut, Blink, Pulse, etc.).");
            uiAColor.CurrentMode = (UIAColor.AnimationMode)EditorGUILayout.EnumPopup(currentModeTooltip, uiAColor.CurrentMode);

            // GUIContent timeScaleModTooltip = new GUIContent("Time Scale Mod", "How the animation is affected by time scale.");
            // uiAColor.timeScaleMod = (UIAnimation.TimeScaleMod)EditorGUILayout.EnumPopup(timeScaleModTooltip, uiAColor.timeScaleMod);

            GUIContent randomizeTooltip = new GUIContent("Randomize", "Enable to randomize animation parameters.");
            uiAColor.Randomize = EditorGUILayout.Toggle(randomizeTooltip, uiAColor.Randomize);

            GUIContent tweenTooltip = new GUIContent("Use Tween", "Enable to use tweening for animations.");
            uiAColor.Tween = EditorGUILayout.Toggle(tweenTooltip, uiAColor.Tween);

            if (uiAColor.CurrentMode == UIAColor.AnimationMode.RandomColor)
            {
                GUIContent manualRandomColourTooltip = new GUIContent("Manual Random Colour", "Enable to manually specify a list of random colors.");
                uiAColor.ManualRandomColour = EditorGUILayout.Toggle(manualRandomColourTooltip, uiAColor.ManualRandomColour);
            }
        }

        /// <summary>
        /// Draws the animation settings section of the inspector.
        /// </summary>
        /// <param name="uiAColor">The UIAColor instance being edited.</param>
        private void DrawAnimationSettings(UIAColor uiAColor)
        {
            if (uiAColor.Tween)
            {
                GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the start of the animation.");
                uiAColor.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uiAColor.easeIn);

                GUIContent easeOutTooltip = new GUIContent("Ease Out", "The easing function for the end of the animation.");
                uiAColor.easeOut = (Ease)EditorGUILayout.EnumPopup(easeOutTooltip, uiAColor.easeOut);
            }
            else
            {
                GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the start of the animation.");
                uiAColor.curveIn = EditorGUILayout.CurveField(curveInTooltip, uiAColor.curveIn);

                GUIContent curveOutTooltip = new GUIContent("Curve Out", "The animation curve for the end of the animation.");
                uiAColor.curveOut = EditorGUILayout.CurveField(curveOutTooltip, uiAColor.curveOut);
            }

            // Color settings based on the animation mode
            switch (uiAColor.CurrentMode)
            {
                case UIAColor.AnimationMode.FadeIn:
                case UIAColor.AnimationMode.FadeOut:
                    GUIContent startColorTooltip = new GUIContent("Start Color", "The starting color of the animation.");
                    uiAColor.StartColor = EditorGUILayout.ColorField(startColorTooltip, uiAColor.StartColor);
                    break;

                case UIAColor.AnimationMode.Blink:
                case UIAColor.AnimationMode.Pulse:
                case UIAColor.AnimationMode.ColorChange:
                case UIAColor.AnimationMode.PingPong:
                    GUIContent startColorTooltip2 = new GUIContent("Start Color", "The starting color of the animation.");
                    uiAColor.StartColor = EditorGUILayout.ColorField(startColorTooltip2, uiAColor.StartColor);

                    GUIContent endColorTooltip = new GUIContent("End Color", "The ending color of the animation.");
                    uiAColor.EndColor = EditorGUILayout.ColorField(endColorTooltip, uiAColor.EndColor);
                    break;

                case UIAColor.AnimationMode.RandomColor:
                    // No additional settings required for RandomColor
                    break;
            }

            if (!uiAColor.Randomize)
            {
                GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                uiAColor.duration = EditorGUILayout.FloatField(durationTooltip, uiAColor.duration);
            }
        }

        /// <summary>
        /// Draws the randomization settings section of the inspector.
        /// </summary>
        /// <param name="uiAColor">The UIAColor instance being edited.</param>
        private void DrawRandomizeSettings(UIAColor uiAColor)
        {
            GUIContent minDurationTooltip = new GUIContent("Min Duration", "The minimum duration of the animation in seconds.");
            uiAColor.minDuration = EditorGUILayout.FloatField(minDurationTooltip, uiAColor.minDuration);

            GUIContent maxDurationTooltip = new GUIContent("Max Duration", "The maximum duration of the animation in seconds.");
            uiAColor.maxDuration = EditorGUILayout.FloatField(maxDurationTooltip, uiAColor.maxDuration);
        }
    }
}