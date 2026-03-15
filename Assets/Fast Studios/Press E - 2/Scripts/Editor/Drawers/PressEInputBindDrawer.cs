#if UNITY_EDITOR
using FastStudios.EditorTools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FastStudios
{
    [CustomPropertyDrawer(typeof(PressEInputBind))]
    public class PressEInputBindDrawer : PropertyDrawer
    {
        public static VisualTreeAsset UXML = Resources.Load<VisualTreeAsset>("FastStudios/ForEditor/UXML/PressEInputBindDrawer");
        public static StyleSheet USS = Resources.Load<StyleSheet>("FastStudios/ForEditor/USS/PressEInputBindDrawer");

        const string HIDE = "Hide";
        const string HIDE2 = "Hide2";
        const string HIDE3 = "Hide3";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            if (UXML == null)
            {
                root.Add(new Label("Missing Resources/PressEInputBindDrawer.uxml"));
                return root;
            }

            root.styleSheets.Add(USS);
            UXML.CloneTree(root);

            var methodProp = property.FindPropertyRelative("InputMethod");
            var methodField = root.Q<PropertyField>("InputMethodField");
            var keyField = root.Q<PropertyField>("KeyCodeField");
            var mouseField = root.Q<PropertyField>("MouseButtonField");
            var actionField = root.Q<PropertyField>("InputActionField");
            var header = root.Q<Label>("BindHeaderLabel");

            if (header != null) header.text = PrettyBindName(property);

            void Refresh()
            {
                var method = (InputMethod)methodProp.enumValueIndex;

#if ENABLE_INPUT_SYSTEM
                var sys = InteractionManager.ProjectInputSystem;
#else
                var sys = InputSystemEnum.Old;
#endif
                bool showLegacy = sys != InputSystemEnum.New;
                bool showAction = sys != InputSystemEnum.Old;

                FSEditorUI.SetVisible(showLegacy, HIDE3, methodField);
                FSEditorUI.SetVisible(showLegacy, HIDE3, keyField);
                FSEditorUI.SetVisible(showLegacy, HIDE3, mouseField);

                FSEditorUI.SetVisible(method == InputMethod.Keyboard, HIDE, keyField);
                FSEditorUI.SetVisible(method == InputMethod.Mouse, HIDE, mouseField);

#if ENABLE_INPUT_SYSTEM
                FSEditorUI.SetVisible(showAction, HIDE2, actionField);
#else
                FSEditorUI.SetVisible(false, HIDE2, actionField);
#endif
            }

            root.Bind(property.serializedObject);

            methodField.RegisterCallback<SerializedPropertyChangeEvent>(_ => Refresh());

            var inputSystemProp = property.serializedObject.FindProperty("inputSystem");
            if (inputSystemProp != null)
                root.TrackPropertyValue(inputSystemProp, _ => Refresh());

#if ENABLE_INPUT_SYSTEM
            InputSystemEnum lastSys = InteractionManager.ProjectInputSystem;
            IVisualElementScheduledItem poll = null;

            void Poll()
            {
                var cur = InteractionManager.ProjectInputSystem;
                if (cur == lastSys) return;
                lastSys = cur;
                Refresh();
            }

            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                lastSys = InteractionManager.ProjectInputSystem;
                poll = root.schedule.Execute(Poll).Every(150);
            });

            root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                poll?.Pause();
                poll = null;
            });
#endif

            Refresh();
            return root;
        }

        string PrettyBindName(SerializedProperty p)
        {
            var s = p.displayName?.Trim() ?? "";

            if (s.EndsWith(" Binds")) s = s.Substring(0, s.Length - " Binds".Length);
            else if (s.EndsWith(" Bind")) s = s.Substring(0, s.Length - " Bind".Length);
            else if (s.EndsWith("Bind")) s = s.Substring(0, s.Length - "Bind".Length);

            return s.Trim();
        }
    }
}
#endif
