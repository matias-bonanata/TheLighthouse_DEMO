using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    [CustomEditor(typeof(UIAPatricleSystem))]
    public class UIAPatricleSystemEditor : UIAnimationEditor
    {
        public override void OnInspectorGUI()
        {
            UIAPatricleSystem uIAPatricleSystem = (UIAPatricleSystem)target;

            // Draw the script reference
            MonoScript script = MonoScript.FromScriptableObject(uIAPatricleSystem);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                // Mark the object as dirty to ensure changes are saved
                EditorUtility.SetDirty(uIAPatricleSystem);
            }
        }

        protected override void DrawCustomProperties(UIAnimation uiAnimation)
        {
            UIAPatricleSystem uiParticleSystem = (UIAPatricleSystem)uiAnimation;
        }
    }
}