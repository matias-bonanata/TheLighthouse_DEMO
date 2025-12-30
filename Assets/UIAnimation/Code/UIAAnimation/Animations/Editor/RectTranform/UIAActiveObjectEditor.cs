using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    [CustomEditor(typeof(UIAActiveObject))]
    public class UIAActiveObjectEditor : UIAnimationEditor
    {
        public override void OnInspectorGUI()
        {
            UIAActiveObject uIAActiveObject = (UIAActiveObject)target;

            // Draw the script reference
            MonoScript script = MonoScript.FromScriptableObject(uIAActiveObject);
            GUIContent scriptTooltip = new GUIContent("Script", "The script associated with this UIAActiveObject.");
            EditorGUILayout.ObjectField(scriptTooltip, script, typeof(MonoScript), false);

            // Active Status field with tooltip
            GUIContent statusTooltip = new GUIContent("Active Status", "The current active status of the object (Enabled, Disabled).");
            uIAActiveObject.Status = (UIAActiveObject.ActiveStatus)EditorGUILayout.EnumPopup(statusTooltip, uIAActiveObject.Status);

            GUIContent delayTooltip = new GUIContent("Delay", "The delay in seconds before changing the active state.");
            uIAActiveObject.Delay = EditorGUILayout.FloatField(delayTooltip, uIAActiveObject.Delay);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(uIAActiveObject);
            }
        }
    }
}