using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace UIAnimation
{
    [CustomEditor(typeof(UIASprite))]
    public class UIASpriteEditor : UIAnimationEditor
    {
        /// <summary>
        /// Draws the custom inspector for the UIASprite.
        /// </summary>
        public override void OnInspectorGUI()
        {
            UIASprite uiASprite = (UIASprite)target;
            serializedObject.Update();
            MonoScript script = MonoScript.FromScriptableObject(uiASprite);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);

            // General settings
            showGeneralSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGeneralSettings, "General Settings");
            if (showGeneralSettings)
            {
                DrawGeneralSettings(uiASprite);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Animation settings
            showAnimationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationSettings, "Animation Settings");
            if (showAnimationSettings)
            {
                DrawAnimationSettings(uiASprite);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Randomization settings
            if (uiASprite.Randomize)
            {
                showRandomizeSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showRandomizeSettings, "Randomize Settings");
                if (showRandomizeSettings)
                {
                    DrawRandomizeSettings(uiASprite);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            if (uiASprite.CurrentMode == UIASprite.AnimationMode.SpriteSwap)
            {
                if (uiASprite.IsLoop)
                {
                    SerializedProperty spritesProperty = serializedObject.FindProperty("Sprites");
                    GUIContent spritesTooltip = new GUIContent("Sprites", "The list of sprites to swap between during the animation.");
                    EditorGUILayout.PropertyField(spritesProperty, spritesTooltip, true);
                }
                else
                {
                    GUIContent targetSpriteTooltip = new GUIContent("Target Sprite", "The target sprite to swap to.");
                    uiASprite.TargetSprite = (Sprite)EditorGUILayout.ObjectField(
                        targetSpriteTooltip,
                        uiASprite.TargetSprite,
                        typeof(Sprite),
                        false
                    );
                }
            }

            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(uiASprite);
            }
        }

        /// <summary>
        /// Draws the general settings section of the inspector.
        /// </summary>
        /// <param name="uiASprite">The UIASprite instance being edited.</param>
        private void DrawGeneralSettings(UIASprite uiASprite)
        {
            GUIContent currentModeTooltip = new GUIContent("Current Mode", "The current animation mode (e.g., SpriteSwap).");
            uiASprite.CurrentMode = (UIASprite.AnimationMode)EditorGUILayout.EnumPopup(currentModeTooltip, uiASprite.CurrentMode);

            // GUIContent timeScaleModTooltip = new GUIContent("Time Scale Mod", "How the animation is affected by time scale.");
            // uiASprite.timeScaleMod = (UIAnimation.TimeScaleMod)EditorGUILayout.EnumPopup(timeScaleModTooltip, uiASprite.timeScaleMod);

            GUIContent isLoopTooltip = new GUIContent("Loop", "Enable to loop the animation.");
            uiASprite.IsLoop = EditorGUILayout.Toggle(isLoopTooltip, uiASprite.IsLoop);

            if (uiASprite.IsLoop)
            {
                GUIContent randomizeTooltip = new GUIContent("Randomize", "Enable to randomize animation parameters.");
                uiASprite.Randomize = EditorGUILayout.Toggle(randomizeTooltip, uiASprite.Randomize);
            }
        }

        /// <summary>
        /// Draws the animation settings section of the inspector.
        /// </summary>
        /// <param name="uiASprite">The UIASprite instance being edited.</param>
        private void DrawAnimationSettings(UIASprite uiASprite)
        {
            if (!uiASprite.Randomize && uiASprite.IsLoop)
            {
                GUIContent durationTooltip = new GUIContent("Duration", "The duration of the animation in seconds.");
                uiASprite.duration = EditorGUILayout.FloatField(durationTooltip, uiASprite.duration);
            }
        }

        /// <summary>
        /// Draws the randomization settings section of the inspector.
        /// </summary>
        /// <param name="uiASprite">The UIASprite instance being edited.</param>
        private void DrawRandomizeSettings(UIASprite uiASprite)
        {
            GUIContent minDurationTooltip = new GUIContent("Min Duration", "The minimum duration of the animation in seconds.");
            uiASprite.minDuration = EditorGUILayout.FloatField(minDurationTooltip, uiASprite.minDuration);

            GUIContent maxDurationTooltip = new GUIContent("Max Duration", "The maximum duration of the animation in seconds.");
            uiASprite.maxDuration = EditorGUILayout.FloatField(maxDurationTooltip, uiASprite.maxDuration);
        }
    }
}