#if UNITY_EDITOR
using FastStudios.EditorTools;
using UnityEditor;
using UnityEngine.UIElements;

namespace FastStudios
{
    [CustomEditor(typeof(Reference))]
    public class ReferenceEditor : Editor
    {
        public VisualTreeAsset UXML;
        public StyleSheet USS;
        public VisualElement _root;

        public Reference localTarget;

        public override VisualElement CreateInspectorGUI()
        {
            localTarget = target as Reference;
            VisualElement root = new VisualElement();
            UXML.CloneTree(root);
            _root = root;

            if (USS != null) root.styleSheets.Add(USS);

            VisualElement playerObject = root.Q<VisualElement>(name = "playerObject");
            
            FSEditorUI.ShowIfPredicate(root, () => { return localTarget.autoAssignThisObject == false; }, new[] { playerObject }, serializedObject, new[] { "autoAssignThisObject" });

            // Font Apply
            {
                root.style.unityFontDefinition = new StyleFontDefinition(FSEditorUI.mainFont);
            }

            return root;
        }
    }
}

#endif
