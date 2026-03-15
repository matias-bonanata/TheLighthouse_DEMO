#if UNITY_EDITOR
using FastStudios.EditorTools;
using UnityEditor;
using UnityEngine.UIElements;

namespace FastStudios
{
    [CustomEditor(typeof(GrabReference))]
    public class GrabReferenceEditor : Editor
    {
        public VisualTreeAsset UXML;
        public StyleSheet USS;
        public VisualElement _root;

        public GrabReference localTarget;

        public override VisualElement CreateInspectorGUI()
        {
            localTarget = target as GrabReference;
            VisualElement root = new VisualElement();
            UXML.CloneTree(root);
            _root = root;

            if (USS != null) root.styleSheets.Add(USS);

            VisualElement GrabTransform = root.Q<VisualElement>(name = "GrabTransform");

            FSEditorUI.ShowIfPredicate(root, () => { return localTarget.autoAssignThisTransform == false; }, new[] { GrabTransform }, serializedObject, new[] { "autoAssignThisTransform" });

            // Font Apply
            {
                root.style.unityFontDefinition = new StyleFontDefinition(FSEditorUI.mainFont);
            }

            return root;
        }
    }
}

#endif
