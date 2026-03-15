#if UNITY_EDITOR
using System;
using System.Linq;
using FastStudios.EditorTools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FastStudios
{
    [CustomEditor(typeof(GrabDeposit))]
    public class DepositEditor : Editor
    {
        private VisualElement _root;
        public VisualTreeAsset visualTree;
        public StyleSheet USS;

        private const int MainTabIndex = 100;
        private const int EventsTabIndex = 101;
        private int selectedTab = MainTabIndex;
        private const string HiddenClass = "Hide";

        GrabDeposit localTarget;

        public Color normalTabColor = new Color(0.345098f, 0.345098f, 0.345098f);
        public Color selectedTabColor = new Color(0.4117647f, 0.4117647f, 0.4117647f);

        private VisualElement _mainTab;
        private VisualElement _eventsTab;

        void LoadLastSession(GrabDeposit lTarget)
        {
            if (lTarget.lastSelectedTab >= 5 && lTarget.lastSelectedTab <= 8)
                lTarget.lastSelectedTab = MainTabIndex + (lTarget.lastSelectedTab - 5);

            if (lTarget.lastSelectedTab < MainTabIndex)
                lTarget.lastSelectedTab = MainTabIndex;

            selectedTab = lTarget.lastSelectedTab;
        }

        public override VisualElement CreateInspectorGUI()
        {
            localTarget = target as GrabDeposit;

            VisualElement root = new VisualElement();
            visualTree.CloneTree(root);
            _root = root;

            LoadLastSession(localTarget);

            if (USS != null) root.styleSheets.Add(USS);

            VisualElement MainVars = root.Q<VisualElement>(name = "MainVars");
            VisualElement Upper = MainVars.Q<VisualElement>(name = "Upper");
            VisualElement Lower = MainVars.Q<VisualElement>(name = "Lower");
            VisualElement TabsParent = Upper.Q<VisualElement>(name = "Tabs");
            VisualElement[] allVisuals = { root, MainVars, Upper, Lower, TabsParent };

            _mainTab = Lower.Q<VisualElement>(name: "MainTab");
            _eventsTab = Lower.Q<VisualElement>(name: "EventsTabs");

            root.Bind(serializedObject);

            Button mainTabButton = TabsParent.Q<Button>(name = "MainButton");
            Button eventsButton = TabsParent.Q<Button>(name = "EventsButton");
            Button[] allButtons = { mainTabButton, eventsButton };

            SetupPageChange(mainTabButton, MainTabIndex, allButtons);
            SetupPageChange(eventsButton, EventsTabIndex, allButtons);

            FSEditorUI.AutoFoldouts(
                root,
                serializedObject,
                (key, deflt) => localTarget.GetFoldout(key, deflt),
                (key, open) => localTarget.SetFoldout(key, open),
                FSEditorUI.HiddenClass
            );

            SetTabIndex(selectedTab, allButtons);

            // Show Ifs
            {
                VisualElement NewAngles = root.Q<VisualElement>(name = "NewAngles");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.isToOverrideRotation; }, new[] { NewAngles }, serializedObject, new[] { "isToOverrideRotation" });

                VisualElement AutoRelease = root.Q<VisualElement>(name = "AutoRelease");
                VisualElement NeedsToBeHeldingToDeposit = root.Q<VisualElement>(name = "NeedsToBeHeldingToDeposit");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.depositMethod == DepositMethod.TriggerCollider || localTarget.depositMethod == DepositMethod.Both; }, new[] { AutoRelease, NeedsToBeHeldingToDeposit }, serializedObject, new[] { "depositMethod" });

                VisualElement CanPlaceDownFreely = root.Q<VisualElement>(name = "CanPlaceDownFreely");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.depositMethod == DepositMethod.Interact || localTarget.depositMethod == DepositMethod.Both; }, new[] { CanPlaceDownFreely }, serializedObject, new[] { "depositMethod" });
                
                VisualElement isToOverrideInput = root.Q<VisualElement>(name = "isToOverrideInput");
                VisualElement NewInput = root.Q<VisualElement>(name = "NewInput");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.depositMethod == DepositMethod.Interact || localTarget.depositMethod == DepositMethod.Both; }, new[] { isToOverrideInput, NewInput }, serializedObject, new[] { "depositMethod" });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(isToOverrideInput), "Hide2", new[] { NewInput });
            }

            // Font Apply
            {
                root.style.unityFontDefinition = new StyleFontDefinition(FSEditorUI.mainFont);
            }

            return root;
        }

        void SetupPageChange(Button button, int index, Button[] allButtons)
        {
            Action action = () => SetTabIndex(index, allButtons);
            button.clicked += action;
        }

        void SetTabIndex(int i, Button[] allButtons)
        {
            selectedTab = i;
            localTarget.lastSelectedTab = selectedTab;

            int index = i == MainTabIndex ? 0 : 1;

            Button button = allButtons[index];

            button.style.backgroundColor = selectedTabColor;

            foreach (Button btn in allButtons.Where(x => x != button))
            {
                btn.style.backgroundColor = normalTabColor;
            }

            HideNonTabElements();
        }

        void HideNonTabElements()
        {
            if (_mainTab != null && _eventsTab != null)
            {
                FSEditorUI.SetVisible(selectedTab == MainTabIndex, HiddenClass, _mainTab);
                FSEditorUI.SetVisible(selectedTab == EventsTabIndex, HiddenClass, _eventsTab);
            }
        }

    }
}
#endif
