using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// Custom editor for the UIARotate script, providing a user-friendly interface to configure rotation animations.
    /// This editor allows customization of general settings, animation settings, and randomization options.
    /// </summary>
    [CustomEditor(typeof(UIARotate))]
    public class UIARotateEditor : UIAnimationEditor
    {
        /// <summary>
        /// Draws the custom inspector for the UIARotate component.
        /// </summary>
        public override void OnInspectorGUI()
        {
            UIARotate uiARotate = (UIARotate)target;
            MonoScript script = MonoScript.FromScriptableObject(uiARotate);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);

            // General settings
            showGeneralSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGeneralSettings, "General Settings");
            if (showGeneralSettings)
            {
                DrawGeneralSettings(uiARotate);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Animation settings
            showAnimationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationSettings, "Animation Settings");
            if (showAnimationSettings)
            {
                DrawAnimationSettings(uiARotate);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Randomization settings
            if (uiARotate.Randomize)
            {
                showRandomizeSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showRandomizeSettings, "Randomize Settings");
                if (showRandomizeSettings)
                {
                    DrawRandomizeSettings(uiARotate);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(uiARotate);
            }
        }

        /// <summary>
        /// Draws the general settings section of the inspector.
        /// </summary>
        /// <param name="uiARotate">The UIARotate instance being edited.</param>
        private void DrawGeneralSettings(UIARotate uiARotate)
        {
            GUIContent currentModeTooltip = new GUIContent("Current Mode", "The current animation mode (Clockwise, Normal, Shake, SideToSide).");
            uiARotate.CurrentMode = (UIARotate.AnimationMode)EditorGUILayout.EnumPopup(currentModeTooltip, uiARotate.CurrentMode);

            // GUIContent timeScaleModTooltip = new GUIContent("Time Scale Mod", "How the animation is affected by time scale.");
            // uiARotate.timeScaleMod = (UIAnimation.TimeScaleMod)EditorGUILayout.EnumPopup(timeScaleModTooltip, uiARotate.timeScaleMod);

            GUIContent randomizeTooltip = new GUIContent("Randomize", "Enable to randomize animation parameters.");
            uiARotate.Randomize = EditorGUILayout.Toggle(randomizeTooltip, uiARotate.Randomize);

            GUIContent tweenTooltip = new GUIContent("Use Tween", "Enable to use tweening for animations.");
            uiARotate.Tween = EditorGUILayout.Toggle(tweenTooltip, uiARotate.Tween);
        }

        /// <summary>
        /// Draws the animation settings section of the inspector.
        /// </summary>
        /// <param name="uiARotate">The UIARotate instance being edited.</param>
        private void DrawAnimationSettings(UIARotate uiARotate)
        {
            if (uiARotate.CurrentMode == UIARotate.AnimationMode.Clockwise)
            {
                if (uiARotate.Tween)
                {
                    GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the start of the animation.");
                    uiARotate.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uiARotate.easeIn);
                }
                else
                {
                    GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the start of the animation.");
                    uiARotate.curveIn = EditorGUILayout.CurveField(curveInTooltip, uiARotate.curveIn);
                }

                if (!uiARotate.Randomize)
                {
                    GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                    uiARotate.duration = EditorGUILayout.FloatField(durationTooltip, uiARotate.duration);
                }

                // Add dropdowns for each axis
                GUIContent xAxisTooltip = new GUIContent("X Axis", "The rotation value for the X axis.");
                uiARotate.EndValue.x = DrawAxisDropdown(xAxisTooltip, uiARotate.EndValue.x);

                GUIContent yAxisTooltip = new GUIContent("Y Axis", "The rotation value for the Y axis.");
                uiARotate.EndValue.y = DrawAxisDropdown(yAxisTooltip, uiARotate.EndValue.y);

                GUIContent zAxisTooltip = new GUIContent("Z Axis", "The rotation value for the Z axis.");
                uiARotate.EndValue.z = DrawAxisDropdown(zAxisTooltip, uiARotate.EndValue.z);
            }

            if (uiARotate.CurrentMode == UIARotate.AnimationMode.Normal)
            {
                if (uiARotate.Tween)
                {
                    GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the start of the animation.");
                    uiARotate.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uiARotate.easeIn);

                    GUIContent easeOutTooltip = new GUIContent("Ease Out", "The easing function for the end of the animation.");
                    uiARotate.easeOut = (Ease)EditorGUILayout.EnumPopup(easeOutTooltip, uiARotate.easeOut);
                }
                else
                {
                    GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the start of the animation.");
                    uiARotate.curveIn = EditorGUILayout.CurveField(curveInTooltip, uiARotate.curveIn);

                    GUIContent curveOutTooltip = new GUIContent("Curve Out", "The animation curve for the end of the animation.");
                    uiARotate.curveOut = EditorGUILayout.CurveField(curveOutTooltip, uiARotate.curveOut);
                }

                if (!uiARotate.Randomize)
                {
                    GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                    uiARotate.duration = EditorGUILayout.FloatField(durationTooltip, uiARotate.duration);

                    GUIContent endValueTooltip = new GUIContent("End Value", "The target rotation values for the animation.");
                    uiARotate.EndValue = EditorGUILayout.Vector3Field(endValueTooltip, uiARotate.EndValue);
                }
            }
            else if (uiARotate.CurrentMode == UIARotate.AnimationMode.Shake)
            {
                if (uiARotate.Tween)
                {
                    GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the start of the animation.");
                    uiARotate.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uiARotate.easeIn);
                }
                else
                {
                    GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the start of the animation.");
                    uiARotate.curveIn = EditorGUILayout.CurveField(curveInTooltip, uiARotate.curveIn);
                }

                if (!uiARotate.Randomize)
                {
                    GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                    uiARotate.duration = EditorGUILayout.FloatField(durationTooltip, uiARotate.duration);

                    GUIContent endValueTooltip = new GUIContent("End Value", "The target rotation values for the animation.");
                    uiARotate.EndValue = EditorGUILayout.Vector3Field(endValueTooltip, uiARotate.EndValue);
                }
            }
            else if (uiARotate.CurrentMode == UIARotate.AnimationMode.SideToSide)
            {
                if (uiARotate.Tween)
                {
                    GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the start of the animation.");
                    uiARotate.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uiARotate.easeIn);

                    GUIContent easeOutTooltip = new GUIContent("Ease Out", "The easing function for the end of the animation.");
                    uiARotate.easeOut = (Ease)EditorGUILayout.EnumPopup(easeOutTooltip, uiARotate.easeOut);
                }
                else
                {
                    GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the start of the animation.");
                    uiARotate.curveIn = EditorGUILayout.CurveField(curveInTooltip, uiARotate.curveIn);

                    GUIContent curveOutTooltip = new GUIContent("Curve Out", "The animation curve for the end of the animation.");
                    uiARotate.curveOut = EditorGUILayout.CurveField(curveOutTooltip, uiARotate.curveOut);
                }

                if (!uiARotate.Randomize)
                {
                    GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                    uiARotate.duration = EditorGUILayout.FloatField(durationTooltip, uiARotate.duration);
                }

                GUIContent minEndValueTooltip = new GUIContent("Min End Value", "The minimum rotation values for the animation (Rocking to the first side).");
                uiARotate.minEndValue = EditorGUILayout.Vector3Field(minEndValueTooltip, uiARotate.minEndValue);

                GUIContent maxEndValueTooltip = new GUIContent("Max End Value", "The maximum rotation values for the animation (Rocking to the other side).");
                uiARotate.maxEndValue = EditorGUILayout.Vector3Field(maxEndValueTooltip, uiARotate.maxEndValue);
            }
        }

        /// <summary>
        /// Helper method to create a dropdown for selecting axis values.
        /// </summary>
        /// <param name="label">The label for the dropdown.</param>
        /// <param name="currentValue">The current value of the axis.</param>
        /// <returns>The selected value from the dropdown.</returns>
        private float DrawAxisDropdown(GUIContent label, float currentValue)
        {
            int selectedIndex = 0;
            if (currentValue == -360) selectedIndex = 0;
            else if (currentValue == 0) selectedIndex = 1;
            else if (currentValue == 360) selectedIndex = 2;

            string[] options = { "-360", "0", "360" };
            selectedIndex = EditorGUILayout.Popup(label, selectedIndex, options);

            switch (selectedIndex)
            {
                case 0: return -360;
                case 1: return 0;
                case 2: return 360;
                default: return currentValue;
            }
        }

        /// <summary>
        /// Draws the randomization settings section of the inspector.
        /// </summary>
        /// <param name="uiARotate">The UIARotate instance being edited.</param>
        private void DrawRandomizeSettings(UIARotate uiARotate)
        {
            GUIContent minDurationTooltip = new GUIContent("Min Duration", "The minimum duration of the animation in seconds.");
            uiARotate.minDuration = EditorGUILayout.FloatField(minDurationTooltip, uiARotate.minDuration);

            GUIContent maxDurationTooltip = new GUIContent("Max Duration", "The maximum duration of the animation in seconds.");
            uiARotate.maxDuration = EditorGUILayout.FloatField(maxDurationTooltip, uiARotate.maxDuration);

            if (uiARotate.CurrentMode == UIARotate.AnimationMode.Normal || uiARotate.CurrentMode == UIARotate.AnimationMode.Shake)
            {
                GUIContent minEndValueTooltip = new GUIContent("Min End Value", "The minimum rotation values for the animation.");
                uiARotate.minEndValue = EditorGUILayout.Vector3Field(minEndValueTooltip, uiARotate.minEndValue);

                GUIContent maxEndValueTooltip = new GUIContent("Max End Value", "The maximum rotation values for the animation.");
                uiARotate.maxEndValue = EditorGUILayout.Vector3Field(maxEndValueTooltip, uiARotate.maxEndValue);
            }
        }
    }
}