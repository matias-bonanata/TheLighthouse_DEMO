using System;
using System.Collections.Generic;
using System.Linq;
using FastStudios.EditorTools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FastStudios
{
    [CustomEditor(typeof(RotationLerp))]
    public class RotationLerpEditor : Editor
    {
        private VisualElement _root;
        public VisualTreeAsset visualTree;
        public StyleSheet USS;
        RotationLerp localTarget;
        private LerpTimelinePreviewUI _timeline;

        private const int RotationTabIndex = 100;
        private const int LerpTabIndex = 101;
        private int selectedTab = RotationTabIndex;
        private const string HiddenClass = "Hide";
        public Color normalTabColor = new Color(0.345098f, 0.345098f, 0.345098f);
        public Color selectedTabColor = new Color(0.4117647f, 0.4117647f, 0.4117647f);
        VisualElement RotationTab;
        VisualElement LerpTab;

        void LoadLastSession(RotationLerp lTarget)
        {
            if (lTarget.lastSelectedTab >= 5 && lTarget.lastSelectedTab <= 8)
                lTarget.lastSelectedTab = RotationTabIndex + (lTarget.lastSelectedTab - 5);

            if (lTarget.lastSelectedTab < RotationTabIndex)
                lTarget.lastSelectedTab = RotationTabIndex;

            selectedTab = lTarget.lastSelectedTab;
        }

        public override VisualElement CreateInspectorGUI()
        {
            localTarget = target as RotationLerp;

            VisualElement root = new VisualElement();
            visualTree.CloneTree(root);
            _root = root;

            if (USS != null) root.styleSheets.Add(USS);

            LoadLastSession(localTarget);

            VisualElement MainVars = root.Q<VisualElement>(name = "MainVars");
            VisualElement Upper = MainVars.Q<VisualElement>(name = "Upper");
            VisualElement Lower = MainVars.Q<VisualElement>(name = "Lower");
            VisualElement TabsParent = Upper.Q<VisualElement>(name = "Tabs");
            VisualElement[] allVisuals = { root, MainVars, Upper, Lower, TabsParent };

            RotationTab = Lower.Q<VisualElement>(name: "RotationTab");
            LerpTab = Lower.Q<VisualElement>(name: "LerpTab");

            root.Bind(serializedObject);

            Button RotationButton = TabsParent.Q<Button>(name = "RotationButton");
            Button LerpButton = TabsParent.Q<Button>(name = "LerpButton");
            Button[] allButtons = { RotationButton, LerpButton };

            SetupPageChange(RotationButton, RotationTabIndex, allButtons);
            SetupPageChange(LerpButton, LerpTabIndex, allButtons);

            FSEditorUI.AutoFoldouts(
                root,
                serializedObject,
                (key, deflt) => localTarget.GetFoldout(key, deflt),
                (key, open) => localTarget.SetFoldout(key, open),
                FSEditorUI.HiddenClass
            );

            SetTabIndex(selectedTab, allButtons);

            AddUniversalMenu();

            InteractableUniversalsSO.OnChanged -= OnUniversalsChanged;
            InteractableUniversalsSO.OnChanged += OnUniversalsChanged;

            // Show Ifs
            {
                VisualElement ObjectToMove = root.Q<VisualElement>(name = "ObjectToMove");
                VisualElement NewRot = root.Q<VisualElement>(name = "NewRot");
                VisualElement EulerAngles = root.Q<VisualElement>(name = "EulerAngles");

                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.AffectOtherObject), ObjectToMove);
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.useEuler), EulerAngles);
                FSEditorUI.ShowIfPredicate(root, () => { return !localTarget.useEuler; }, new[] { NewRot }, serializedObject, new[] { "useEuler" });

                VisualElement ShowGizmosOnlyWhenSelected = root.Q<VisualElement>(name = "ShowGizmosOnlyWhenSelected");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.ShowGizmos), ShowGizmosOnlyWhenSelected);

            }

            _timeline ??= new LerpTimelinePreviewUI();
            _timeline.Setup(
                root,
                localTarget,
                () => localTarget.Duration,
                () => (localTarget.AffectOtherObject && localTarget.ObjectToMove != null) ? localTarget.ObjectToMove : localTarget.transform,
                HiddenClass
            );

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

            int index = i == RotationTabIndex ? 0 : 1;

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
            if (RotationTab != null && LerpTab != null)
            {
                FSEditorUI.SetVisible(selectedTab == RotationTabIndex, HiddenClass, RotationTab);
                FSEditorUI.SetVisible(selectedTab == LerpTabIndex, HiddenClass, LerpTab);
            }
        }

        #region Universals

        private readonly Dictionary<string, PropertyField> _fieldsByPath = new();
        private readonly Dictionary<string, VisualElement> _bindablesByPath = new();
        static bool _applying;

        void OnUniversalEntryValueChanged(string propertyPath)
        {
            if (_applying) return;
            try
            {
                _applying = true;

                foreach (var t in targets)
                {
                    var it = t as RotationLerp;
                    if (it == null) continue;

                    if (!it.IsUniversalBound(propertyPath)) continue;

                    InteractableUniversalsSO.ApplySingle(it, propertyPath);
                }

                Repaint();
            }
            finally { _applying = false; }
        }

        void OnUniversalsChanged()
        {
            if (localTarget == null) return;
            var so = serializedObject;
            bool changed = false;

            foreach (var path in _fieldsByPath.Keys.ToList())
            {
                if (!localTarget.IsUniversalBound(path)) continue;
                if (InteractableUniversalsSO.Instance.TryGet(localTarget.GetType().FullName, path, out var ue))
                {
                    var sp = so.FindProperty(path);
                    if (InteractableUniversalsSO.ApplyToProperty(ue, sp))
                        changed = true;
                }
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(localTarget);
            }
        }

        void AddUniversalMenu()
        {
            foreach (var pf in _root.Query<PropertyField>().ToList())
            {
                var path = pf.bindingPath;
                if (string.IsNullOrEmpty(path)) continue;
                _fieldsByPath[path] = pf;

                AttachUniversalContext(pf, path);

                UpdateReadonlyBadge(path, pf);
            }

            foreach (var be in _root.Query<BindableElement>().ToList())
            {
                if (be.GetFirstAncestorOfType<PropertyField>() != null) continue;

                var path = be.bindingPath;
                if (string.IsNullOrEmpty(path)) continue;

                _bindablesByPath[path] = be;

                AttachUniversalContext(be, path);

                UpdateReadonlyBadge(path, be);
            }

            _root.schedule.Execute(() =>
            {
                if (this == null || target == null) return;

                var so = new SerializedObject(target);
                foreach (var kv in _fieldsByPath.ToArray())
                {
                    if (kv.Value == null) continue;
                    var sp = so.FindProperty(kv.Key);
                    if (sp != null)
                        UpdateReadonlyBadge(kv.Key, kv.Value);
                }
            }).ExecuteLater(0);
        }

        void AttachUniversalContext(VisualElement target, string path)
        {
            void AttachOne(VisualElement ve)
            {
                var m = new ContextualMenuManipulator(evt =>
                {
                    if (InteractableUniversalsSO.Instance.TryGet(localTarget.GetType().FullName, path, out var _))
                    {
                        if (!localTarget.IsUniversalBound(path))
                            evt.menu.AppendAction("Universals/Load Universal", _ => ApplyAndBindUniversal(path), DropdownMenuAction.Status.Normal);
                        else
                            evt.menu.AppendAction("Universals/Unbind Universal", _ => UnbindUniversal(path), DropdownMenuAction.Status.Normal);
                    }
                    evt.menu.AppendAction("Universals/Open Window", _ => InteractableUniversalsWindow.Open(), DropdownMenuAction.Status.Normal);
                    evt.StopPropagation();
                });

                ve.AddManipulator(m);
                ve.pickingMode = PickingMode.Position;
            }

            AttachOne(target);

            void AttachToAllIMGUI()
            {
                foreach (var imgui in target.Query<IMGUIContainer>().ToList())
                    AttachOne(imgui);
            }
            EditorApplication.delayCall += AttachToAllIMGUI;
            target.RegisterCallback<GeometryChangedEvent>(_ => AttachToAllIMGUI());

            if (target is PropertyField pf)
            {
                var sp = serializedObject.FindProperty(path);
                if (sp != null && sp.propertyType == SerializedPropertyType.Quaternion)
                {
                    var wrapper = EnsureWrapper(pf);
                    var overlay = EnsureOverlay(wrapper, path);
                }
            }
        }

        void ApplyAndBindUniversal(string path)
        {
            var so = serializedObject;

            var sp = so.FindProperty(path);
            if (sp == null) return;

            Undo.RecordObject(localTarget, "Bind Universal");

            localTarget.__Editor_SaveUniversalBackup(so, path);

            so.UpdateIfRequiredOrScript();

            sp = so.FindProperty(path);

            if (InteractableUniversalsSO.Instance.TryGet(localTarget.GetType().FullName, path, out var ue) &&
                InteractableUniversalsSO.ApplyToProperty(ue, sp))
            {
                so.ApplyModifiedProperties();
                localTarget.BindUniversal(path);
                if (_fieldsByPath.TryGetValue(path, out var pf)) UpdateReadonlyBadge(path, pf);
                else if (_bindablesByPath.TryGetValue(path, out var be)) UpdateReadonlyBadge(path, be);
                EditorUtility.SetDirty(localTarget);
                PrefabUtility.RecordPrefabInstancePropertyModifications(localTarget);
            }

        }

        void UnbindUniversal(string path)
        {
            var so = serializedObject;

            Undo.RecordObject(localTarget, "Unbind Universal");
            bool restored = localTarget.__Editor_RestoreUniversalBackup(so, path);

            localTarget.UnbindUniversal(path);
            if (_fieldsByPath.TryGetValue(path, out var pf)) UpdateReadonlyBadge(path, pf);

            if (_bindablesByPath.TryGetValue(path, out var be))
                UpdateReadonlyBadge(path, be);

            if (restored)
                EditorUtility.SetDirty(localTarget);

            _root.MarkDirtyRepaint();
            PrefabUtility.RecordPrefabInstancePropertyModifications(localTarget);
        }

        void UpdateReadonlyBadge(string path, PropertyField pf)
        {
            bool bound = localTarget.IsUniversalBound(path);
            pf.SetEnabled(!bound);

            pf.pickingMode = PickingMode.Position;
            foreach (var imgui in pf.Query<IMGUIContainer>().ToList())
                imgui.pickingMode = PickingMode.Position;

            var sp = serializedObject.FindProperty(path);
            bool isQuat = sp != null && sp.propertyType == SerializedPropertyType.Quaternion;

            if (isQuat)
            {
                var wrapper = EnsureWrapper(pf);
                var overlay = EnsureOverlay(wrapper, path);
                if (overlay != null)
                {
                    overlay.style.display = DisplayStyle.Flex;
                    overlay.pickingMode = bound ? PickingMode.Position : PickingMode.Ignore;
                }
            }
            else
            {
                HideOverlay(pf);
            }

            const string kBadgeName = "UniversalBadge";
            var old = pf.Q<Label>(kBadgeName);
            if (old != null) old.RemoveFromHierarchy();

            if (bound)
            {
                var badge = new Label("Universal") { name = kBadgeName };
                badge.style.unityFontStyleAndWeight = FontStyle.Italic;
                badge.style.marginLeft = 6;
                badge.style.opacity = 0.75f;
                pf.Add(badge);
            }
        }

        const string UniversalWrapperName = "__UniversalWrapper__";
        const string UniversalOverlayName = "__UniversalOverlay__";

        VisualElement EnsureWrapper(PropertyField pf)
        {
            if (pf.parent != null && pf.parent.name == UniversalWrapperName)
                return pf.parent;

            var parent = pf.parent;
            if (parent == null) return null;

            int index = parent.IndexOf(pf);
            var wrapper = new VisualElement { name = UniversalWrapperName };
            wrapper.style.position = Position.Relative;
            wrapper.style.flexGrow = 1;

            parent.Insert(index, wrapper);
            parent.Remove(pf);
            wrapper.Add(pf);

            return wrapper;
        }

        VisualElement EnsureOverlay(VisualElement wrapper, string path)
        {
            if (wrapper == null) return null;

            var overlay = wrapper.Q<VisualElement>(UniversalOverlayName);
            if (overlay == null)
            {
                overlay = new VisualElement { name = UniversalOverlayName };
                overlay.style.position = Position.Absolute;
                overlay.style.left = 0; overlay.style.right = 0;
                overlay.style.top = 0; overlay.style.bottom = 0;
                overlay.pickingMode = PickingMode.Position;

                var m = new ContextualMenuManipulator(evt =>
                {
                    if (InteractableUniversalsSO.Instance.TryGet(localTarget.GetType().FullName, path, out var _))
                    {
                        if (!localTarget.IsUniversalBound(path))
                            evt.menu.AppendAction("Universals/Load Universal", _ => ApplyAndBindUniversal(path), DropdownMenuAction.Status.Normal);
                        else
                            evt.menu.AppendAction("Universals/Unbind Universal", _ => UnbindUniversal(path), DropdownMenuAction.Status.Normal);
                    }
                    evt.menu.AppendAction("Universals/Open Window", _ => InteractableUniversalsWindow.Open(), DropdownMenuAction.Status.Normal);

                    evt.StopPropagation();
                });

                overlay.AddManipulator(m);
                wrapper.Add(overlay);
            }

            var pf = wrapper.Q<PropertyField>();
            overlay.style.display = (pf != null && pf.resolvedStyle.display != DisplayStyle.None)
                                    ? DisplayStyle.Flex : DisplayStyle.None;

            wrapper.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                var _pf = wrapper.Q<PropertyField>();
                overlay.style.display = (_pf != null && _pf.resolvedStyle.display != DisplayStyle.None)
                                        ? DisplayStyle.Flex : DisplayStyle.None;
            });

            return overlay;
        }

        void HideOverlay(PropertyField pf)
        {
            var wrapper = (pf.parent != null && pf.parent.name == UniversalWrapperName) ? pf.parent : null;
            var overlay = wrapper?.Q<VisualElement>(UniversalOverlayName);
            if (overlay != null) overlay.style.display = DisplayStyle.None;
        }


        void UpdateReadonlyBadge(string path, VisualElement ve)
        {
            bool bound = localTarget.IsUniversalBound(path);
            ve.SetEnabled(!bound);
            ve.pickingMode = PickingMode.Position;

            const string kBadgeName = "UniversalBadge";
            var oldHere = ve.Q<Label>(kBadgeName);
            if (oldHere != null) oldHere.RemoveFromHierarchy();
            var oldParent = ve.parent?.Q<Label>(kBadgeName);
            if (oldParent != null) oldParent.RemoveFromHierarchy();

            if (bound)
            {
                var badge = new Label("Universal") { name = kBadgeName };
                badge.style.unityFontStyleAndWeight = FontStyle.Italic;
                badge.style.marginLeft = 6;
                badge.style.opacity = 0.75f;
                (ve.parent ?? ve).Add(badge);
            }
        }

        void OnEnable()
        {
            InteractableUniversalsSO.OnEntryValueChanged -= OnUniversalEntryValueChanged;
            InteractableUniversalsSO.OnEntryValueChanged += OnUniversalEntryValueChanged;
        }

        void OnDisable()
        {
            InteractableUniversalsSO.OnEntryValueChanged -= OnUniversalEntryValueChanged;

            _timeline?.Dispose();
            _timeline = null;
        }

        #endregion
    }
}
