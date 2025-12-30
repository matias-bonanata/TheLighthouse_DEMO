using UnityEditor;
using UnityEngine;
using System;

namespace UIAnimation
{
    /// <summary>
    /// Custom property drawer for the AnimationObjectData class.
    /// </summary>
    [CustomPropertyDrawer(typeof(AnimationObjectData))]
    public class AnimationObjectDataDrawer : PropertyDrawer
    {
        private const string FoldoutKey = "AnimationObjectData_Foldout_"; // Key for storing foldout state in EditorPrefs

        // Stores the previous value of InteractiveObjectData to detect changes
        private InteractiveObjectData previousInteractiveObjectData;

        /// <summary>
        /// Draws the custom property field in the Inspector.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Retrieve the foldout state from EditorPrefs
            string foldoutKey = FoldoutKey + property.propertyPath;
            bool isExpanded = EditorPrefs.GetBool(foldoutKey, false);

            // Ensure the animation name is not empty
            SerializedProperty animationNameProperty = property.FindPropertyRelative("animationName");
            if (string.IsNullOrEmpty(animationNameProperty.stringValue))
            {
                animationNameProperty.stringValue = "Animation";
            }

            // Calculate the duration of the animation for display in the header
            float duration = GetDuration(property);
            GUIContent headerLabel = new GUIContent(animationNameProperty.stringValue);
            if (duration >= 0)
            {
                headerLabel.text = $"{animationNameProperty.stringValue}  [{duration:F2}s]";
            }

            // Draw the foldout for the animation settings
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, headerLabel, true);
            EditorPrefs.SetBool(foldoutKey, isExpanded);

            if (isExpanded)
            {
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Draw the animation name field
                Rect animationNameRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(animationNameRect, animationNameProperty, new GUIContent("Animation Name"));
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Draw the InteractiveObjectData dropdown
                SerializedProperty interactiveObjectDataProperty = property.FindPropertyRelative("interactiveObjectData");
                InteractiveObjectData selectedType = (InteractiveObjectData)interactiveObjectDataProperty.enumValueIndex;

                // Track changes to InteractiveObjectData
                EditorGUI.BeginChangeCheck();

                Rect interactiveObjectDataRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(interactiveObjectDataRect, interactiveObjectDataProperty);

                // If InteractiveObjectData is changed, show a confirmation dialog
                if (EditorGUI.EndChangeCheck())
                {
                    bool userConfirmed = EditorUtility.DisplayDialog(
                        "Warning",
                        "Changing the InteractiveObjectData will clear all fields in this section. Are you sure you want to proceed?",
                        "Yes",
                        "No"
                    );

                    if (userConfirmed)
                    {
                        // Clear unused fields and update the previous value
                        ClearAllFields(property);
                        previousInteractiveObjectData = selectedType;
                    }
                    else
                    {
                        // Revert to the previous value if the user cancels
                        interactiveObjectDataProperty.enumValueIndex = (int)previousInteractiveObjectData;
                    }
                }

                float yOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Draw fields based on the selected InteractiveObjectData type
                switch (selectedType)
                {
                    case InteractiveObjectData.RectTransform:
                        Rect rectTransformRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                        Rect uiARectTransformRect = new Rect(position.x, position.y + yOffset * 2, position.width, EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(
                            rectTransformRect,
                            property.FindPropertyRelative("objectList.rectTransform"),
                            new GUIContent("RectTransform", "The RectTransform component to animate.")
                        );
                        EditorGUI.PropertyField(
                            uiARectTransformRect,
                            property.FindPropertyRelative("animationList.uIARectTransform"),
                            new GUIContent("UIARectTransform", "The animation script for the RectTransform.")
                        );
                        position.y += yOffset * 2;
                        break;

                    case InteractiveObjectData.Image:
                        Rect imageRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                        Rect uiAImageRect = new Rect(position.x, position.y + yOffset * 2, position.width, EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(
                            imageRect,
                            property.FindPropertyRelative("objectList.image"),
                            new GUIContent("Image", "The Image component to animate.")
                        );
                        EditorGUI.PropertyField(
                            uiAImageRect,
                            property.FindPropertyRelative("animationList.uIAImage"),
                            new GUIContent("UIAImage", "The animation script for the Image.")
                        );
                        position.y += yOffset * 2;
                        break;

                    case InteractiveObjectData.Canvas:
                        Rect canvasGroupRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                        Rect uiACanvasRect = new Rect(position.x, position.y + yOffset * 2, position.width, EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(
                            canvasGroupRect,
                            property.FindPropertyRelative("objectList.canvasGroup"),
                            new GUIContent("CanvasGroup", "The CanvasGroup component to animate.")
                        );
                        EditorGUI.PropertyField(
                            uiACanvasRect,
                            property.FindPropertyRelative("animationList.uIACanvas"),
                            new GUIContent("UIACanvas", "The animation script for the CanvasGroup.")
                        );
                        position.y += yOffset * 2;
                        break;

                    case InteractiveObjectData.Animator:
                        Rect animatorGroupRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                        Rect uIAUnityAnimationRect = new Rect(position.x, position.y + yOffset * 2, position.width, EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(
                            animatorGroupRect,
                            property.FindPropertyRelative("objectList.animator"),
                            new GUIContent("Animator", "The Animator component to animate.")
                        );
                        EditorGUI.PropertyField(
                            uIAUnityAnimationRect,
                            property.FindPropertyRelative("animationList.uIAUnityAnimation"),
                            new GUIContent("UIAUnityAnimation", "The animation script for the Animator.")
                        );
                        position.y += yOffset * 2;
                        break;

                    case InteractiveObjectData.Patrical:
                        Rect ParticalSystemGroupRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                        Rect uIAPatriclesRect = new Rect(position.x, position.y + yOffset * 2, position.width, EditorGUIUtility.singleLineHeight);
                        EditorGUI.PropertyField(
                            ParticalSystemGroupRect,
                            property.FindPropertyRelative("objectList.particleSystem"),
                            new GUIContent("ParticleSystem", "The ParticleSystem component to animate.")
                        );
                        EditorGUI.PropertyField(
                            uIAPatriclesRect,
                            property.FindPropertyRelative("animationList.uIAPatricles"),
                            new GUIContent("UIAPatricles", "The animation script for the ParticleSystem.")
                        );
                        position.y += yOffset * 2;
                        break;
                }

                // Check if required fields are empty and display a warning
                bool isObjectListEmpty = false;
                bool isAnimationListEmpty = false;

                switch (selectedType)
                {
                    case InteractiveObjectData.RectTransform:
                        isObjectListEmpty = property.FindPropertyRelative("objectList.rectTransform").objectReferenceValue == null;
                        isAnimationListEmpty = property.FindPropertyRelative("animationList.uIARectTransform").objectReferenceValue == null;
                        break;

                    case InteractiveObjectData.Image:
                        isObjectListEmpty = property.FindPropertyRelative("objectList.image").objectReferenceValue == null;
                        isAnimationListEmpty = property.FindPropertyRelative("animationList.uIAImage").objectReferenceValue == null;
                        break;

                    case InteractiveObjectData.Canvas:
                        isObjectListEmpty = property.FindPropertyRelative("objectList.canvasGroup").objectReferenceValue == null;
                        isAnimationListEmpty = property.FindPropertyRelative("animationList.uIACanvas").objectReferenceValue == null;
                        break;

                    case InteractiveObjectData.Animator:
                        isObjectListEmpty = property.FindPropertyRelative("objectList.animator").objectReferenceValue == null;
                        isAnimationListEmpty = property.FindPropertyRelative("animationList.uIAUnityAnimation").objectReferenceValue == null;
                        break;

                    case InteractiveObjectData.Patrical:
                        isObjectListEmpty = property.FindPropertyRelative("objectList.particleSystem").objectReferenceValue == null;
                        isAnimationListEmpty = property.FindPropertyRelative("animationList.uIAPatricles").objectReferenceValue == null;
                        break;
                }

                if (isObjectListEmpty || isAnimationListEmpty)
                {
                    string helpBoxMessage = "";

                    if (isObjectListEmpty && isAnimationListEmpty)
                    {
                        helpBoxMessage = "Please assign an object and an animation.";
                    }
                    else if (isObjectListEmpty)
                    {
                        helpBoxMessage = "Please assign an object.";
                    }
                    else if (isAnimationListEmpty)
                    {
                        helpBoxMessage = "Please assign an animation.";
                    }

                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
                    Rect helpBoxRect = new Rect(position.x, position.y, position.width, helpBoxHeight);
                    EditorGUI.HelpBox(helpBoxRect, helpBoxMessage, MessageType.Warning);

                    position.y += helpBoxHeight;
                }
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Clears fields that are no longer relevant after changing the InteractiveObjectData type.
        /// </summary>
        private void ClearAllFields(SerializedProperty property)
        {
            // Clear RectTransform fields
            property.FindPropertyRelative("objectList.rectTransform").objectReferenceValue = null;
            property.FindPropertyRelative("animationList.uIARectTransform").objectReferenceValue = null;

            // Clear Image fields
            property.FindPropertyRelative("objectList.image").objectReferenceValue = null;
            property.FindPropertyRelative("animationList.uIAImage").objectReferenceValue = null;

            // Clear CanvasGroup fields
            property.FindPropertyRelative("objectList.canvasGroup").objectReferenceValue = null;
            property.FindPropertyRelative("animationList.uIACanvas").objectReferenceValue = null;

            // Clear Animator fields
            property.FindPropertyRelative("objectList.animator").objectReferenceValue = null;
            property.FindPropertyRelative("animationList.uIAUnityAnimation").objectReferenceValue = null;

            // Clear Partical fields
            property.FindPropertyRelative("objectList.particleSystem").objectReferenceValue = null;
            property.FindPropertyRelative("animationList.uIAPatricles").objectReferenceValue = null;
        }

        /// <summary>
        /// Calculates the height of the property drawer based on its expanded state and content.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            string foldoutKey = FoldoutKey + property.propertyPath;
            if (EditorPrefs.GetBool(foldoutKey, false))
            {
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                SerializedProperty interactiveObjectDataProperty = property.FindPropertyRelative("interactiveObjectData");
                InteractiveObjectData selectedType = (InteractiveObjectData)interactiveObjectDataProperty.enumValueIndex;

                switch (selectedType)
                {
                    case InteractiveObjectData.RectTransform:
                    case InteractiveObjectData.Image:
                    case InteractiveObjectData.Canvas:
                    case InteractiveObjectData.Animator:
                    case InteractiveObjectData.Patrical:
                        height += 2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                        break;
                }

                bool isObjectListEmpty = false;
                bool isAnimationListEmpty = false;

                switch (selectedType)
                {
                    case InteractiveObjectData.RectTransform:
                        isObjectListEmpty = property.FindPropertyRelative("objectList.rectTransform").objectReferenceValue == null;
                        isAnimationListEmpty = property.FindPropertyRelative("animationList.uIARectTransform").objectReferenceValue == null;
                        break;

                    case InteractiveObjectData.Image:
                        isObjectListEmpty = property.FindPropertyRelative("objectList.image").objectReferenceValue == null;
                        isAnimationListEmpty = property.FindPropertyRelative("animationList.uIAImage").objectReferenceValue == null;
                        break;

                    case InteractiveObjectData.Canvas:
                        isObjectListEmpty = property.FindPropertyRelative("objectList.canvasGroup").objectReferenceValue == null;
                        isAnimationListEmpty = property.FindPropertyRelative("animationList.uIACanvas").objectReferenceValue == null;
                        break;

                    case InteractiveObjectData.Animator:
                        isObjectListEmpty = property.FindPropertyRelative("objectList.animator").objectReferenceValue == null;
                        isAnimationListEmpty = property.FindPropertyRelative("animationList.uIAUnityAnimation").objectReferenceValue == null;
                        break;

                    case InteractiveObjectData.Patrical:
                        isObjectListEmpty = property.FindPropertyRelative("objectList.particleSystem").objectReferenceValue == null;
                        isAnimationListEmpty = property.FindPropertyRelative("animationList.uIAPatricles").objectReferenceValue == null;
                        break;
                }

                if (isObjectListEmpty || isAnimationListEmpty)
                {
                    height += EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            return height;
        }

        /// <summary>
        /// Retrieves the duration of the animation based on the selected type.
        /// </summary>
        private float GetDuration(SerializedProperty property)
        {
            SerializedProperty interactiveObjectDataProperty = property.FindPropertyRelative("interactiveObjectData");
            InteractiveObjectData selectedType = (InteractiveObjectData)interactiveObjectDataProperty.enumValueIndex;

            switch (selectedType)
            {
                case InteractiveObjectData.RectTransform:
                    SerializedProperty uIARectTransformProperty = property.FindPropertyRelative("animationList.uIARectTransform");
                    if (uIARectTransformProperty.objectReferenceValue != null && uIARectTransformProperty.objectReferenceValue is UIARectTransform rectTransform)
                    {
                        return rectTransform.duration;
                    }
                    break;

                case InteractiveObjectData.Image:
                    SerializedProperty uIAImageProperty = property.FindPropertyRelative("animationList.uIAImage");
                    if (uIAImageProperty.objectReferenceValue != null && uIAImageProperty.objectReferenceValue is UIAImage image)
                    {
                        return image.duration;
                    }
                    break;

                case InteractiveObjectData.Canvas:
                    SerializedProperty uIACanvasProperty = property.FindPropertyRelative("animationList.uIACanvas");
                    if (uIACanvasProperty.objectReferenceValue != null && uIACanvasProperty.objectReferenceValue is UIACanvas canvas)
                    {
                        return canvas.duration;
                    }
                    break;

                case InteractiveObjectData.Animator:
                    SerializedProperty uIAUnityAnimationProperty = property.FindPropertyRelative("animationList.uIAUnityAnimation");
                    if (uIAUnityAnimationProperty.objectReferenceValue != null && uIAUnityAnimationProperty.objectReferenceValue is UIAUnityAnimation animator)
                    {
                        return animator.duration;
                    }
                    break;

                case InteractiveObjectData.Patrical:
                    SerializedProperty uIAPatriclesProperty = property.FindPropertyRelative("animationList.uIAPatricles");
                    if (uIAPatriclesProperty.objectReferenceValue != null && uIAPatriclesProperty.objectReferenceValue is UIAPatricles particles)
                    {
                        return particles.duration;
                    }
                    break;
            }

            return -1;
        }
    }
}