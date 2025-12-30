using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    /// <summary>
    /// Base custom editor for the UIAnimation ScriptableObject.
    /// Provides a user-friendly interface for configuring general animation settings,
    /// tweening options, and randomization parameters.
    /// Derived classes can extend this editor to add custom properties.
    /// </summary>
    [CustomEditor(typeof(UIAnimation), true)]
    public class UIAnimationEditor : Editor
    {
        protected bool showGeneralSettings = true; // Whether to display general settings
        protected bool showAnimationSettings = true; // Whether to display tween settings
        protected bool showRandomizeSettings = true; // Whether to display randomization settings

        /// <summary>
        /// Draws the custom inspector for the UIAnimation.
        /// </summary>
        public override void OnInspectorGUI()
        {
            UIAnimation uiAnimation = (UIAnimation)target;

            // Draw the script reference
            MonoScript script = MonoScript.FromScriptableObject(uiAnimation);
            GUIContent scriptTooltip = new GUIContent("Script", "The script associated with this UIAnimation component.");
            EditorGUILayout.ObjectField(scriptTooltip, script, typeof(MonoScript), false);

            // General settings
            showGeneralSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGeneralSettings, "General Settings");
            if (showGeneralSettings)
            {
                DrawGeneralSettings(uiAnimation);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Tween settings
            showAnimationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationSettings, "Tween Settings");
            if (showAnimationSettings)
            {
                DrawTweenSettings(uiAnimation);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Randomization settings
            if (uiAnimation.Randomize)
            {
                showRandomizeSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showRandomizeSettings, "Randomize Settings");
                if (showRandomizeSettings)
                {
                    DrawRandomizeSettings(uiAnimation);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            else
            {
                // If randomization is disabled, display the standard duration field
                GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                uiAnimation.duration = EditorGUILayout.FloatField(durationTooltip, uiAnimation.duration);
            }

            // Draw custom properties for derived classes
            DrawCustomProperties(uiAnimation);

            if (GUI.changed)
            {
                // Mark the object as dirty to ensure changes are saved
                EditorUtility.SetDirty(uiAnimation);
            }
        }

        /// <summary>
        /// Draws the general settings section of the inspector.
        /// </summary>
        /// <param name="uiAnimation">The UIAnimation instance being edited.</param>
        private void DrawGeneralSettings(UIAnimation uiAnimation)
        {
            GUIContent randomizeTooltip = new GUIContent("Randomize", "Enable to randomize animation parameters.");
            uiAnimation.Randomize = EditorGUILayout.Toggle(randomizeTooltip, uiAnimation.Randomize);
        }

        /// <summary>
        /// Draws the tween settings section of the inspector.
        /// </summary>
        /// <param name="uiAnimation">The UIAnimation instance being edited.</param>
        private void DrawTweenSettings(UIAnimation uiAnimation)
        {
            GUIContent tweenTooltip = new GUIContent("Use Tween", "Enable to use tweening for animations.");
            uiAnimation.Tween = EditorGUILayout.Toggle(tweenTooltip, uiAnimation.Tween);

            if (uiAnimation.Tween)
            {
                GUIContent easeInTooltip = new GUIContent("Ease In", "The easing function for the start of the animation.");
                uiAnimation.easeIn = (Ease)EditorGUILayout.EnumPopup(easeInTooltip, uiAnimation.easeIn);

                GUIContent easeOutTooltip = new GUIContent("Ease Out", "The easing function for the end of the animation.");
                uiAnimation.easeOut = (Ease)EditorGUILayout.EnumPopup(easeOutTooltip, uiAnimation.easeOut);
            }
            else
            {
                GUIContent curveInTooltip = new GUIContent("Curve In", "The animation curve for the start of the animation.");
                uiAnimation.curveIn = EditorGUILayout.CurveField(curveInTooltip, uiAnimation.curveIn);

                GUIContent curveOutTooltip = new GUIContent("Curve Out", "The animation curve for the end of the animation.");
                uiAnimation.curveOut = EditorGUILayout.CurveField(curveOutTooltip, uiAnimation.curveOut);
            }
        }

        /// <summary>
        /// Draws the randomization settings section of the inspector.
        /// </summary>
        /// <param name="uiAnimation">The UIAnimation instance being edited.</param>
        private void DrawRandomizeSettings(UIAnimation uiAnimation)
        {
            GUIContent minDurationTooltip = new GUIContent("Min Duration", "The minimum duration of the animation in seconds.");
            uiAnimation.minDuration = EditorGUILayout.FloatField(minDurationTooltip, uiAnimation.minDuration);

            GUIContent maxDurationTooltip = new GUIContent("Max Duration", "The maximum duration of the animation in seconds.");
            uiAnimation.maxDuration = EditorGUILayout.FloatField(maxDurationTooltip, uiAnimation.maxDuration);
        }

        /// <summary>
        /// Draws custom properties for derived classes.
        /// This method can be overridden in derived classes to add additional properties.
        /// </summary>
        /// <param name="uiAnimation">The UIAnimation instance being edited.</param>
        protected virtual void DrawCustomProperties(UIAnimation uiAnimation)
        {
            // This method can be overridden in derived classes to add custom properties
        }
    }
}