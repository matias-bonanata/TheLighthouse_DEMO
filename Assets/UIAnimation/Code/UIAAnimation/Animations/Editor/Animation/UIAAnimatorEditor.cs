using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    [CustomEditor(typeof(UIAAnimator))]
    public class UIAAnimatorEditor : UIAnimationEditor
    {
        public override void OnInspectorGUI()
        {
            UIAAnimator uiaAnimator = (UIAAnimator)target;
            MonoScript script = MonoScript.FromScriptableObject(uiaAnimator);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);

            // General settings
            showGeneralSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGeneralSettings, "General Settings");
            if (showGeneralSettings)
            {
                DrawGeneralSettings(uiaAnimator);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Animation settings (commented out for future use)
            // showAnimationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationSettings, "Animation Settings");
            // if (showAnimationSettings)
            // {
            //     DrawAnimationSettings(uiaAnimator);
            // }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(uiaAnimator);
            }
        }

        /// <summary>
        /// Draws the General Settings section in the Inspector.
        /// Allows the user to configure the animation mode (Trigger or Bool) and the animation name.
        /// If the animation mode is Bool, it also provides a toggle for the boolean value.
        /// </summary>
        /// <param name="uiaAnimator">The UIAAnimator instance being edited.</param>
        private void DrawGeneralSettings(UIAAnimator uiaAnimator)
        {
            // Animation mode (Trigger or Bool)
            GUIContent animationModeTooltip = new GUIContent("Animation Mode", "The mode of the animation: Trigger or Bool.");
            uiaAnimator.animationMode = (UIAAnimator.AnimationMode)EditorGUILayout.EnumPopup(animationModeTooltip, uiaAnimator.animationMode);

            // Animation name
            GUIContent animationNameTooltip = new GUIContent("Animation Name", "The name of the animation parameter in the Animator.");
            uiaAnimator.AnimationName = EditorGUILayout.TextField(animationNameTooltip, uiaAnimator.AnimationName);

            // Display the value toggle if the mode is Bool
            if (uiaAnimator.animationMode == UIAAnimator.AnimationMode.Bool)
            {
                GUIContent valueTooltip = new GUIContent("Value", "The boolean value to set for the animation parameter (true or false).");
                uiaAnimator.value = EditorGUILayout.Toggle(valueTooltip, uiaAnimator.value);
            }
        }

        private void DrawAnimationSettings(UIAAnimator uiaAnimator)
        {
            // Future implementation for additional animation settings
        }
    }
}