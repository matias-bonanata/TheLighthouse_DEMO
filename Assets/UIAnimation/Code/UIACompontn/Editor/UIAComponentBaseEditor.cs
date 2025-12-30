using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UIAnimation
{
    /// <summary>
    /// Base editor class for UIAComponentBase.
    /// Provides a custom Inspector with a ReorderableList for animation settings and foldouts for events and settings.
    /// </summary>
    public class UIAComponentBaseEditor : Editor
    {
        protected ReorderableList animationSettingsList; // ReorderableList for managing animation settings
        protected List<InteractiveData> usedInteractiveData = new List<InteractiveData>(); // Tracks used InteractiveData values

        // Foldout states
        protected bool showEvents = true;
        protected bool showSettings = true;

        protected const string ShowEventsKey = "UIAComponentBaseEditor_ShowEvents"; // Key for storing foldout state in EditorPrefs
        protected const string ShowSettingsKey = "UIAComponentBaseEditor_ShowSettings"; // Key for storing foldout state in EditorPrefs

        /// <summary>
        /// Initializes the editor and sets up the ReorderableList.
        /// </summary>
        protected virtual void OnEnable()
        {
            // Load foldout states from EditorPrefs
            showEvents = EditorPrefs.GetBool(ShowEventsKey, true);
            showSettings = EditorPrefs.GetBool(ShowSettingsKey, true);

            // Initialize the ReorderableList for animationSettings
            animationSettingsList = new ReorderableList(serializedObject,
                serializedObject.FindProperty("animationSettings"),
                true, true, true, true);

            // Set up the ReorderableList header
            animationSettingsList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Animation Settings");
            };

            // Define how each element in the list is drawn
            animationSettingsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = animationSettingsList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;

                // Get the InteractiveData property
                SerializedProperty interactiveDataProperty = element.FindPropertyRelative("_interactiveData");

                // Retrieve SupportedInteractions from the component
                UIAComponentBase uiaComponent = (UIAComponentBase)serializedObject.targetObject;
                var supportedInteractions = uiaComponent.SupportedInteractions;

                // If SupportedInteractions is not empty, display only supported InteractiveData
                if (supportedInteractions != null && supportedInteractions.Any())
                {
                    // Create a list of supported InteractiveData values
                    List<InteractiveData> supportedValues = supportedInteractions.ToList();

                    // Get the current value of InteractiveData
                    InteractiveData currentValue = (InteractiveData)interactiveDataProperty.enumValueIndex;

                    // Find the index of the current value in the list of supported values
                    int currentIndex = supportedValues.IndexOf(currentValue);
                    if (currentIndex == -1) currentIndex = 0; // If the current value is unsupported, select the first value

                    // Display a dropdown menu with supported values
                    Rect interactiveDataRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                    int newIndex = EditorGUI.Popup(interactiveDataRect, "Interactive Data", currentIndex, supportedValues.Select(x => x.ToString()).ToArray());

                    // Update InteractiveData value if the selection changes
                    if (newIndex != currentIndex)
                    {
                        interactiveDataProperty.enumValueIndex = (int)supportedValues[newIndex];
                    }
                }
                else
                {
                    // If SupportedInteractions is empty, display a standard field
                    Rect interactiveDataRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(interactiveDataRect, interactiveDataProperty, new GUIContent("Interactive Data"));
                }

                // Shift position to render the AnimationObjectData list
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Get the AnimationObjectData list property
                SerializedProperty animationObjectDataList = element.FindPropertyRelative("_animationObjectData");

                // Display the AnimationObjectData list
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUI.GetPropertyHeight(animationObjectDataList, true)),
                    animationObjectDataList,
                    new GUIContent("Animation Objects"),
                    true
                );
            };

            // Define the height of each element in the list
            animationSettingsList.elementHeightCallback = (int index) =>
            {
                var element = animationSettingsList.serializedProperty.GetArrayElementAtIndex(index);

                // Height for InteractiveData
                float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Height for the AnimationObjectData list
                SerializedProperty animationObjectDataList = element.FindPropertyRelative("_animationObjectData");
                height += EditorGUI.GetPropertyHeight(animationObjectDataList, true);

                return height;
            };

            // Handle adding new elements to the list
            animationSettingsList.onAddCallback = (ReorderableList list) =>
            {
                // Get the supported interactions
                UIAComponentBase uiaComponent = (UIAComponentBase)serializedObject.targetObject;
                var supportedInteractions = uiaComponent.SupportedInteractions.ToList();

                // Update the list of used InteractiveData
                UpdateUsedInteractiveData();

                // Create a GenericMenu to display the supported interactions
                GenericMenu menu = new GenericMenu();
                foreach (var interaction in supportedInteractions)
                {
                    if (usedInteractiveData.Contains(interaction))
                    {
                        // Disable and gray out already used interactions
                        menu.AddDisabledItem(new GUIContent(interaction.ToString()));
                    }
                    else
                    {
                        menu.AddItem(new GUIContent(interaction.ToString()), false, () => AddAnimationSetting(interaction));
                    }
                }

                // Show the menu
                menu.ShowAsContext();
            };

            // Handle removing elements from the list
            animationSettingsList.onRemoveCallback = (ReorderableList list) =>
            {
                // Remove the element from the list
                animationSettingsList.serializedProperty.DeleteArrayElementAtIndex(list.index);

                // Update the list of used InteractiveData
                UpdateUsedInteractiveData();

                // Apply changes
                serializedObject.ApplyModifiedProperties();
            };
        }

        /// <summary>
        /// Adds a new animation setting with the specified interaction type.
        /// </summary>
        /// <param name="interaction">The interaction type to add.</param>
        private void AddAnimationSetting(InteractiveData interaction)
        {
            // Add a new element to the list
            int index = animationSettingsList.serializedProperty.arraySize;
            animationSettingsList.serializedProperty.arraySize++;
            animationSettingsList.index = index;

            // Get the new element
            var element = animationSettingsList.serializedProperty.GetArrayElementAtIndex(index);

            // Set the InteractiveData value
            element.FindPropertyRelative("_interactiveData").enumValueIndex = (int)interaction;

            // Initialize the AnimationObjectData list
            element.FindPropertyRelative("_animationObjectData").ClearArray();

            // Update the list of used InteractiveData
            UpdateUsedInteractiveData();

            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Updates the list of used InteractiveData values.
        /// </summary>
        private void UpdateUsedInteractiveData()
        {
            // Clear the list of used InteractiveData
            usedInteractiveData.Clear();

            // Populate the list with currently used InteractiveData
            for (int i = 0; i < animationSettingsList.serializedProperty.arraySize; i++)
            {
                var element = animationSettingsList.serializedProperty.GetArrayElementAtIndex(i);
                InteractiveData interaction = (InteractiveData)element.FindPropertyRelative("_interactiveData").enumValueIndex;
                if (!usedInteractiveData.Contains(interaction))
                {
                    usedInteractiveData.Add(interaction);
                }
            }
        }

        /// <summary>
        /// Draws the custom Inspector GUI.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Update the serialized object
            serializedObject.Update();

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
        }

        /// <summary>
        /// Helper method to safely draw a property field.
        /// </summary>
        /// <param name="propertyName">The name of the property to draw.</param>
        /// <param name="label">The label to display for the property.</param>
        protected void DrawPropertyField(string propertyName, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
            }
            else
            {
                Debug.LogWarning($"Property '{propertyName}' not found in serialized object.");
            }
        }
    }
}