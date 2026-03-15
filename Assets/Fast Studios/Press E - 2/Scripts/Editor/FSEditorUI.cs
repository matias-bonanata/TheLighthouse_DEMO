#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;

namespace FastStudios.EditorTools
{
    public static class FSEditorUI
    {
        public const string HiddenClass = "Hide";
        public const string HiddenClass1 = "Hide1";
        public const string HiddenClassAlt = "Hide3";
        public const string FoldoutContainer = "FoldoutContainer";
        public static Font mainFont = Resources.Load<Font>("FastStudios/Fonts/OpenSans-Bold");

        static SerializedProperty GetProp(SerializedObject so, string path) => so.FindProperty(path);

        public static void SetVisible(bool show, string uss = HiddenClass, params VisualElement[] targets)
        {
            if (targets == null) return;
            foreach (var t in targets)
            {
                if (t == null) continue;
                t.EnableInClassList(uss, !show);
            }
        }

        public static void ShowIfBool(VisualElement root, SerializedObject so, string boolPropPath, params VisualElement[] targets)
            => ShowIfBool(root, so, boolPropPath, HiddenClass1, targets);

        public static void ShowIfBool(VisualElement root, SerializedObject so, string boolPropPath, string hideClass, params VisualElement[] targets)
        {
            var p = GetProp(so, boolPropPath);
            void Refresh() => SetVisible(p != null && p.boolValue, hideClass, targets);
            if (p != null) root.TrackPropertyValue(p, _ => Refresh());
            Refresh();
        }

        public static void ShowIfBool(VisualElement root, SerializedProperty boolProp, params VisualElement[] targets)
            => ShowIfBool(root, boolProp, HiddenClass1, targets);

        public static void ShowIfBool(VisualElement root, SerializedProperty boolProp, string hideClass, params VisualElement[] targets)
        {
            void Refresh() => SetVisible(boolProp != null && boolProp.boolValue, hideClass, targets);
            if (boolProp != null) root.TrackPropertyValue(boolProp, _ => Refresh());
            Refresh();
        }


        public static void ShowIfPredicate(
            VisualElement root,
            Func<bool> predicate,
            VisualElement[] targets,
            SerializedObject so,
            IEnumerable<string> dependentPropPaths,
            string hideClass = HiddenClass1)
        {
            if (predicate == null) return;

            void Refresh() => SetVisible(predicate(), hideClass, targets);

            if (dependentPropPaths != null)
            {
                foreach (var path in dependentPropPaths.Where(s => !string.IsNullOrEmpty(s)))
                {
                    var p = GetProp(so, path);
                    if (p != null) root.TrackPropertyValue(p, _ => Refresh());
                }
            }

            Refresh();
        }

        public static void ShowIfPredicate(
            VisualElement root,
            Func<bool> predicate,
            VisualElement[] targets,
            string hideClass = HiddenClass1)
        {
            if (predicate == null) return;
            SetVisible(predicate(), hideClass, targets);
        }

        public static List<(string key, VisualElement header, VisualElement container, SerializedProperty sp)>
        AutoFoldouts(
            VisualElement scope,
            SerializedObject so,
            Func<string, bool, bool> getFoldout,
            Action<string, bool> setFoldout,
            string hideClass = HiddenClass,
            string foldoutSuffix = "Foldout",
            string containerSuffix = "Container")
        {
            var result = new List<(string, VisualElement, VisualElement, SerializedProperty)>();

            var headers = scope.Query<VisualElement>().ToList()
                               .Where(v => !string.IsNullOrEmpty(v.name) && v.name.EndsWith(foldoutSuffix));

            foreach (var header in headers)
            {
                string key = header.name;
                string containerName = key.Substring(0, key.Length - foldoutSuffix.Length) + containerSuffix;
                var container = scope.Q<VisualElement>(name: containerName);
                if (container == null) continue;
                if (container.ClassListContains(FoldoutContainer)) continue;

                var sp = so.FindProperty(key);
                bool initial = (sp != null) ? sp.boolValue : (getFoldout?.Invoke(key, false) ?? false);

                SetupFoldoutPair(key, header, container, sp, initial, so, setFoldout, hideClass);
                result.Add((key, header, container, sp));
            }

            return result;
        }

        static void SetupFoldoutPair(
            string key,
            VisualElement header,
            VisualElement container,
            SerializedProperty sp,
            bool initial,
            SerializedObject so,
            Action<string, bool> setFoldout,
            string hideClass)
        {
            SetVisible(initial, hideClass, container);
            container.AddToClassList(FoldoutContainer);

            if (sp != null && header != null)
                header.GetFirstAncestorOfType<VisualElement>()?.TrackPropertyValue(sp, _ => SetVisible(sp.boolValue, hideClass, container));

            header.RegisterCallback<ClickEvent>(_ =>
            {
                bool show = container.ClassListContains(hideClass);
                if (sp != null)
                {
                    so.Update();
                    sp.boolValue = show;
                    so.ApplyModifiedProperties();
                }
                else
                {
                    setFoldout?.Invoke(key, show);
                }

                SetVisible(show, hideClass, container);
            });
        }

        public static bool IsSupportedByUniversals(SerializedPropertyType t)
        {
            switch (t)
            {
                case SerializedPropertyType.ObjectReference: return false;
                default: return true;
            }
        }


    }
}
#endif
