using UnityEngine;

#if UNITY_EDITOR
using FastStudios.EditorTools;
using UnityEditor;
using UnityEngine.UIElements;

namespace FastStudios
{
    [CustomEditor(typeof(AnchorChanger))]
    public class AnchorChangerEditor : Editor
    {
        public VisualTreeAsset UXML;
        public StyleSheet USS;
        public VisualElement _root;

        public AnchorChanger localTarget;

        public override VisualElement CreateInspectorGUI()
        {
            localTarget = target as AnchorChanger;
            VisualElement root = new VisualElement();
            UXML.CloneTree(root);
            _root = root;

            if (USS != null) root.styleSheets.Add(USS);

            Button Apply = root.Q<Button>(name = "Apply");
            Button Cancel = root.Q<Button>(name = "Cancel");
            Button Restore = root.Q<Button>(name = "Restore");

            Apply.clicked += () => localTarget.Apply();
            Cancel.clicked += () => localTarget.Cancel();
            Restore.clicked += () => localTarget.Restore();

            FSEditorUI.ShowIfPredicate(root, () => { return localTarget.oldValue != Vector3.zero; }, new[] { Restore }, serializedObject, new[] { "oldValue" });

            // Font Apply
            {
                root.style.unityFontDefinition = new StyleFontDefinition(FSEditorUI.mainFont);
            }

            return root;
        }
    }
}

#endif
