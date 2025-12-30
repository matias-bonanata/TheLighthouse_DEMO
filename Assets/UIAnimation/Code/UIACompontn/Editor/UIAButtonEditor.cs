using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    [CustomEditor(typeof(UIAButton))]
    public class UIAButtonEditor : UIAComponentBaseEditor
    {
        private UIAButton uIAButton;
        private bool showButtonsSettingEvents = true;
        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            // Update the serialized object
            serializedObject.Update();

            uIAButton = (UIAButton)target;

            // Draw the ReorderableList for Animation Settings
            animationSettingsList.DoLayoutList();

            // Foldout for Events
            bool newShowEvents = EditorGUILayout.BeginFoldoutHeaderGroup(showEvents, "Events");
            if (newShowEvents != showEvents)
            {
                showEvents = newShowEvents;
                EditorPrefs.SetBool(ShowEventsKey, showEvents); // Save foldout state
            }
            if (showEvents)
            {
                DrawPropertyField("OnStartEvent", "On Start Event");
                DrawPropertyField("onPointerEnterEvent", "On Pointer Enter");
                DrawPropertyField("onPointerExitEvent", "On Pointer Exit");
                DrawPropertyField("onPointerDownEvent", "On Pointer Down");
                DrawPropertyField("onPointerUpEvent", "On Pointer Up");
                DrawPropertyField("onPointerClickEvent", "On Pointer Click");
                DrawPropertyField("onScriptPlayEvent", "On Scrip Play");
                DrawPropertyField("onScriptStopEvent", "On Scrip Stop");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Foldout for Settings
            bool newShowSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSettings, "Settings");
            if (newShowSettings != showSettings)
            {
                showSettings = newShowSettings;
                EditorPrefs.SetBool(ShowSettingsKey, showSettings); // Save foldout state
            }
            if (showSettings)
            {
                DrawPropertyField("debug", "Debug");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Apply changes
            serializedObject.ApplyModifiedProperties();


            showButtonsSettingEvents = EditorGUILayout.BeginFoldoutHeaderGroup(showButtonsSettingEvents, "Button Setting");
            if (showButtonsSettingEvents)
            {
                GUIContent InteractiveDelayTooltip = new GUIContent("Click Delay", "The parameter is responsible for the time that must elapse before you can press the button again.");
                uIAButton.InteractiveDelay = EditorGUILayout.FloatField(InteractiveDelayTooltip, uIAButton.InteractiveDelay);
            }
        }
    }
}