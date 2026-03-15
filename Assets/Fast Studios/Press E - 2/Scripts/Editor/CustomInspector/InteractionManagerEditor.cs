using UnityEditor;
using UnityEngine.UIElements;
using System;
using FastStudios.EditorTools;

namespace FastStudios
{
    [CustomEditor(typeof(InteractionManager))]
    public class InteractionManagerEditor : Editor
    {
        public VisualTreeAsset visualTree;
        public StyleSheet USS;
        private int selectedTab = 100;

        private VisualElement _MainTab;
        private VisualElement _EventTab;

        private const int MainTabIndex = 100;
        private const int EventTabIndex = 101;
        private const string HideClassName = "Hide";

        public override VisualElement CreateInspectorGUI()
        {
            var localTarget = target as InteractionManager;

            VisualElement root = new VisualElement();
            visualTree.CloneTree(root);

            if (USS != null) root.styleSheets.Add(USS);

            VisualElement mainImage = root.Q<VisualElement>(name = "MainImage");
            VisualElement mainVars = root.Q<VisualElement>(name = "MainVars");
            VisualElement leftCol = root.Q<VisualElement>(name = "LeftCol");
            VisualElement rightCol = root.Q<VisualElement>(name = "RightCol");
            VisualElement[] allVisuals = { root, mainImage, mainVars, leftCol, rightCol };

            Button mainTabButton = leftCol.Q<Button>(name = "MainButton");
            Button eventsTabButton = leftCol.Q<Button>(name = "EventsButton");
            Button[] allButtons = { mainTabButton, eventsTabButton };

            _MainTab = root.Q<VisualElement>(name = "MainTab");
            _EventTab = root.Q<VisualElement>(name = "EventsTab");

            SetupPageChange(mainTabButton, MainTabIndex, root, allButtons, allVisuals);
            SetupPageChange(eventsTabButton, EventTabIndex, root, allButtons, allVisuals);

            FSEditorUI.AutoFoldouts(
                root,
                serializedObject,
                (key, deflt) => localTarget.GetFoldout(key, deflt),
                (key, open) => localTarget.SetFoldout(key, open),
                FSEditorUI.HiddenClass
            );

            SetTabIndex(selectedTab, root, allButtons, allVisuals);

            // Show Ifs
            {
                VisualElement inputSystem = root.Q<VisualElement>(name = "inputSystem");
                VisualElement KeybindsContainer = root.Q<VisualElement>(name = "KeybindsContainer");
                VisualElement interactionInput = root.Q<VisualElement>(name = "interactionInput");
                VisualElement GrabKeybinds = root.Q<VisualElement>(name = "GrabKeybinds");
                VisualElement InspectionKeybinds = root.Q<VisualElement>(name = "InspectionKeybinds");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.OnlyGetUIButtonsInput == false; }, new[] { inputSystem, interactionInput, GrabKeybinds, InspectionKeybinds}, serializedObject, new[] { "OnlyGetUIButtonsInput" }, "HideAlt");

                VisualElement PlayerObjectField = root.Q<VisualElement>(name = "PlayerObjectField");
                VisualElement PlayerTagField = root.Q<VisualElement>(name = "PlayerTagField");
                VisualElement PlayerLayerField = root.Q<VisualElement>(name = "PlayerLayerField");
                VisualElement PlayerObjectNameField = root.Q<VisualElement>(name = "PlayerObjectNameField");
                VisualElement PlayerScriptField = root.Q<VisualElement>(name = "PlayerScriptField");

                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.PlayerDetection == PlayerDetection.GameObject; }, new[] { PlayerObjectField }, serializedObject, new[] { "PlayerDetection" }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.PlayerDetection == PlayerDetection.Tag; }, new[] { PlayerTagField }, serializedObject, new[] { "PlayerDetection" }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.PlayerDetection == PlayerDetection.Layer; }, new[] { PlayerLayerField }, serializedObject, new[] { "PlayerDetection" }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.PlayerDetection == PlayerDetection.ObjectName; }, new[] { PlayerObjectNameField }, serializedObject, new[] { "PlayerDetection" }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.PlayerDetection == PlayerDetection.MonoBehaviour; }, new[] { PlayerScriptField }, serializedObject, new[] { "PlayerDetection" }, "Hide2");

                VisualElement ItemPlayerGrabTransform = root.Q<VisualElement>(name = "ItemPlayerGrabTransform");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.UseGrabReference == false; }, new[] { ItemPlayerGrabTransform }, serializedObject, new[] { "UseGrabReference" }, "Hide2");
            }

            // Font Apply
            {
                root.style.unityFontDefinition = new StyleFontDefinition(FSEditorUI.mainFont);
            }

            return root;
        }

        void SetupPageChange(Button button, int index, VisualElement root, Button[] allButtons, VisualElement[] allVisuals)
        {
            Action action = () => SetTabIndex(index, root, allButtons, allVisuals);
            button.clicked += action;
        }

        void SetTabIndex(int i, VisualElement root, Button[] allButtons, VisualElement[] allVisuals)
        {
            selectedTab = i;

            HideNonTabElements(root, allButtons, allVisuals);
        }

        void HideNonTabElements(VisualElement root, Button[] allButtons, VisualElement[] allVisuals)
        {
            if (_MainTab != null && _EventTab != null)
            {
                FSEditorUI.SetVisible(selectedTab == MainTabIndex, HideClassName, _MainTab);
                FSEditorUI.SetVisible(selectedTab == EventTabIndex, HideClassName, _EventTab);
            }
        }
    }

}