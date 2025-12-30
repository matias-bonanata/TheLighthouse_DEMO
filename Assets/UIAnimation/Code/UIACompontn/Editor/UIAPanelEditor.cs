using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    [CustomEditor(typeof(UIAPanel))]
    public class UIAPanelEditor : UIAComponentBaseEditor
    {
        private UIAPanel uIAPanel;
        private bool showButtonsSettingEvents = true;
        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            // Update the serialized object
            serializedObject.Update();

            uIAPanel = (UIAPanel)target;


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
                DrawPropertyField("onOpenEvent", "On Open");
                DrawPropertyField("OnCloseEvent", "On Close");
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

            showButtonsSettingEvents = EditorGUILayout.BeginFoldoutHeaderGroup(showButtonsSettingEvents, "Pnael Setting");
            if (showButtonsSettingEvents)
            {
                GUIContent InteractiveDelayTooltip = new GUIContent("Open Delay", "The parameter is responsible for the time that must elapse before you can open the menu again.");
                uIAPanel.InteractiveDelay = EditorGUILayout.FloatField(InteractiveDelayTooltip, uIAPanel.InteractiveDelay);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            // Apply changes
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}