#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastStudios.EditorTools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace FastStudios
{
    [CustomPropertyDrawer(typeof(ValueBinding))]
    public class ValueBindingDrawer : PropertyDrawer
    {
        public static VisualTreeAsset UXML = Resources.Load<VisualTreeAsset>("FastStudios/ForEditor/UXML/ValueBindingUXML");
        public static StyleSheet USS = Resources.Load<StyleSheet>("FastStudios/ForEditor/USS/CondtionUSS");
        const string kSearch = "FastStudios/ForEditor/Icons/search";

        static AdvancedDropdownState sComponentDropdownState = new AdvancedDropdownState();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var search = Resources.Load<Texture>(kSearch);
            var root = new VisualElement();
            root.name = "BinderRootRoot";
            UXML.CloneTree(root);

            if (USS != null) root.styleSheets.Add(USS);

            root.Bind(property.serializedObject);

            List<string> memberChoicesCache = new List<string>();
            List<string> memberChoicesAll = new();

            var componentTypes = new List<Type>();
            var componentOptions = new List<GUIContent>();

            var targetProp = property.FindPropertyRelative("target");
            var compTypeProp = property.FindPropertyRelative("componentTypeName");
            var sourceProp = property.FindPropertyRelative("source");
            var memberProp = property.FindPropertyRelative("member");

            bool hasTargetModel = (targetProp != null && compTypeProp != null);

            var targetField = root.Q<ObjectField>("TargetField");
            var sourceField = root.Q<ObjectField>("SourceField");
            var compPopup = root.Q<PopupField<string>>("ComponentPopup");
            var memberPopup = root.Q<PopupField<string>>("MemberPopup");
            var searchBtn = root.Q<VisualElement>("MemberSearchButton");
            var searchField = root.Q<TextField>("MemberSearchField");
            bool isLeft = property.name == "left";
            bool isRight = property.name == "right";

            if (targetField != null)
            {
                targetField.objectType = typeof(GameObject);
                targetField.allowSceneObjects = true;
            }

            if (isLeft)
            {
                if (targetField != null) targetField.label = "Read From";
                if (sourceField != null) sourceField.label = "Read From (Source)";
            }

            if (isRight)
            {
                var rightDyn = FindSibling(property, "RightSideDynamic");
                void RefreshRightLabel()
                {
                    if (targetField != null) targetField.label = "Reference";
                    if (sourceField != null) sourceField.label = "Reference (Source)";
                }

                RefreshRightLabel();
                if (rightDyn != null)
                    root.TrackPropertyValue(rightDyn, _ => RefreshRightLabel());
            }

            if (compPopup == null)
            {
                compPopup = new PopupField<string>("Component", new List<string> { InspectorLabels.None }, 0);
                compPopup.name = "ComponentPopup";
                root.Add(compPopup);
            }
            if (memberPopup == null)
            {
                memberPopup = new PopupField<string>("Variable", new List<string> { InspectorLabels.None }, 0);
                memberPopup.name = "MemberPopup";
                root.Add(memberPopup);
            }

            Button quickBtn = null;
            Button cancelBtn = null;
            VisualElement dropdownBtn = null;
            memberPopup.style.paddingLeft = 0;

            var findField = new TextField { name = "MemberFindField", isDelayed = false };
            findField.style.display = DisplayStyle.None;
            findField.style.flexGrow = 1;
            findField.label = "";

            cancelBtn = new Button(() => ExitFindMode())
            {
                name = "MemberCancelFind",
                text = "X",
                tooltip = "Cancel search"
            };
            cancelBtn.style.flexShrink = 0;
            cancelBtn.style.display = DisplayStyle.None;

            quickBtn = new Button(() => EnterFindMode()) { name = "MemberQuickFind" };
            quickBtn.tooltip = "Quick find member (type to search)";
            quickBtn.style.width = 18;
            quickBtn.style.height = 18;
            quickBtn.style.marginRight = 4;
            quickBtn.style.flexShrink = 0;
            quickBtn.style.backgroundImage = new StyleBackground(search as Texture2D);
            quickBtn.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(Length.Percent(50), Length.Percent(50)));

            var ddTex = EditorGUIUtility.IconContent("d_dropdown")?.image as Texture2D;
            dropdownBtn = new VisualElement { name = "MemberDropdownButton" };
            dropdownBtn.style.width = 12;
            dropdownBtn.style.height = 12;
            dropdownBtn.pickingMode = PickingMode.Ignore;
            dropdownBtn.style.alignSelf = Align.Center;
            dropdownBtn.style.flexShrink = 0;
            dropdownBtn.style.backgroundImage = new StyleBackground(ddTex);
            dropdownBtn.style.display = DisplayStyle.None;

            memberPopup?.Add(cancelBtn);

            var input = memberPopup?.Q<VisualElement>(className: "unity-base-field__input")
                      ?? memberPopup?.Q<VisualElement>(className: "unity-base-popup-field__input");

            input?.Insert(0, quickBtn);

            memberPopup?.Add(findField);

            var componentIcon = new VisualElement { name = "ComponentIcon" };
            componentIcon.style.width = 16;
            componentIcon.style.height = 16;
            componentIcon.style.marginRight = 4;
            componentIcon.style.alignSelf = Align.Center;

            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                EnsureInjectedInInput();
                EnsureComponentIconInjected();
            });
            memberPopup.RegisterCallback<GeometryChangedEvent>(_ => EnsureInjectedInInput());
            if (compPopup != null)
                compPopup.RegisterCallback<GeometryChangedEvent>(_ => EnsureComponentIconInjected());

            root.schedule.Execute(() =>
            {
                EnsureInjectedInInput();
                EnsureComponentIconInjected();
            });

            void ApplyMemberFilter(string term)
            {
                if (memberPopup == null || memberChoicesAll == null) return;

                var filtered = new List<string> { InspectorLabels.None };

                if (string.IsNullOrEmpty(term))
                {
                    filtered.AddRange(memberChoicesAll.Where(s => !string.Equals(s, InspectorLabels.None, StringComparison.OrdinalIgnoreCase)));
                }
                else
                {
                    filtered.AddRange(
                        memberChoicesAll.Where(s =>
                            !string.Equals(s, InspectorLabels.None, StringComparison.OrdinalIgnoreCase) &&
                            s.StartsWith(term, StringComparison.OrdinalIgnoreCase)
                        )
                    );
                }

                var currentSel = memberProp.stringValue;
                int newIndex = Mathf.Max(0, filtered.IndexOf(currentSel));

                memberPopup.choices = filtered;
                memberPopup.index = newIndex;

                if (newIndex == 0 && !string.IsNullOrEmpty(currentSel))
                {
                    memberProp.stringValue = string.Empty;
                    memberProp.serializedObject.ApplyModifiedProperties();
                }
            }

            void EnsureInjectedInInput()
            {
                var mInput = memberPopup.Q<VisualElement>(className: "unity-base-popup-field__input");
                if (mInput == null) return;

                void MoveIfNeeded(VisualElement ve, int index = -1)
                {
                    if (ve == null) return;
                    if (ve.parent != mInput)
                    {
                        ve.RemoveFromHierarchy();
                        if (index >= 0 && index <= mInput.childCount) mInput.Insert(index, ve);
                        else mInput.Add(ve);
                    }
                }

                MoveIfNeeded(quickBtn, 0);
                MoveIfNeeded(cancelBtn, 1);
                MoveIfNeeded(findField, 2);
                MoveIfNeeded(dropdownBtn, 3);

                findField.style.flexGrow = 1;
                findField.style.minWidth = 0;
                mInput.style.minWidth = 0;
            }

            void EnsureComponentIconInjected()
            {
                if (compPopup == null) return;

                var cInput = compPopup.Q<VisualElement>(className: "unity-base-popup-field__input");
                if (cInput == null) return;

                if (componentIcon.parent != cInput)
                {
                    componentIcon.RemoveFromHierarchy();
                    cInput.Insert(0, componentIcon);
                }

                componentIcon.style.minWidth = 0;
                cInput.style.minWidth = 0;
            }

            void ToggleDefaultInputChildren(bool show)
            {
                var mInput = memberPopup.Q<VisualElement>(className: "unity-base-popup-field__input");
                if (mInput == null) return;

                foreach (var c in mInput.Children())
                {
                    if (c == quickBtn || c == findField || c == cancelBtn || c == dropdownBtn) continue;
                    c.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            void EnterFindMode()
            {
                EnsureInjectedInInput();
                ToggleDefaultInputChildren(false);

                quickBtn.style.display = DisplayStyle.None;
                cancelBtn.style.display = DisplayStyle.Flex;
                dropdownBtn.style.display = DisplayStyle.Flex;

                findField.value = string.Empty;
                findField.style.display = DisplayStyle.Flex;

                ApplyMemberFilter(string.Empty);

                root.schedule.Execute(() => { findField.Focus(); findField.SelectAll(); });
            }

            void ConfirmFind()
            {
                var list = memberChoicesCache ?? new List<string>();
                string term = findField.value?.Trim();

                int idx = -1;
                if (!string.IsNullOrEmpty(term))
                {
                    idx = list.FindIndex(s => !string.IsNullOrEmpty(s) &&
                                              !s.Equals(InspectorLabels.None, StringComparison.OrdinalIgnoreCase) &&
                                              string.Equals(s, term, StringComparison.OrdinalIgnoreCase));
                    if (idx < 0)
                        idx = list.FindIndex(s => !string.IsNullOrEmpty(s) &&
                                                  !s.Equals(InspectorLabels.None, StringComparison.OrdinalIgnoreCase) &&
                                                  s.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                if (idx >= 0)
                {
                    var value = list[idx];
                    memberProp.stringValue = value == InspectorLabels.None ? string.Empty : value;
                    memberProp.serializedObject.ApplyModifiedProperties();
                    RefreshMemberChoices();
                    memberPopup.index = Mathf.Clamp(idx, 0, memberPopup.choices.Count - 1);
                }
                else
                {
                    Debug.LogError($"[Conditions] Member '{term}' not found.");
                    memberProp.stringValue = string.Empty;
                    memberProp.serializedObject.ApplyModifiedProperties();
                    RefreshMemberChoices();
                }

                ExitFindMode();
            }

            void ExitFindMode()
            {
                EnsureInjectedInInput();

                findField.style.display = DisplayStyle.None;
                cancelBtn.style.display = DisplayStyle.None;
                dropdownBtn.style.display = DisplayStyle.None;

                ToggleDefaultInputChildren(true);
                quickBtn.style.display = DisplayStyle.Flex;

                memberPopup.choices = memberChoicesAll ?? new List<string> { InspectorLabels.None };
                var current = memberProp.stringValue;
                memberPopup.index = Mathf.Max(0, memberPopup.choices.IndexOf(current));

                memberPopup.Blur();
                root.schedule.Execute(() => quickBtn?.Focus());
            }

            findField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    evt.StopImmediatePropagation();
                    evt.StopPropagation();
                    ConfirmFind();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    evt.StopImmediatePropagation();
                    evt.StopPropagation();
                    ExitFindMode();
                }
            }, TrickleDown.TrickleDown);

            void SetDisplay(VisualElement el, bool on) { if (el != null) el.style.display = on ? DisplayStyle.Flex : DisplayStyle.None; }

            void RefreshComponentIcon()
            {
                EnsureComponentIconInjected();

                if (compPopup == null || componentOptions.Count == 0)
                {
                    componentIcon.style.backgroundImage = null;
                    componentIcon.style.visibility = Visibility.Hidden;
                    return;
                }

                int idx = Mathf.Clamp(compPopup.index, 0, componentOptions.Count - 1);
                var tex = componentOptions[idx].image as Texture2D;

                if (tex != null)
                {
                    componentIcon.style.backgroundImage = new StyleBackground(tex);
                    componentIcon.style.visibility = Visibility.Visible;
                }
                else
                {
                    componentIcon.style.backgroundImage = null;
                    componentIcon.style.visibility = Visibility.Hidden;
                }
            }

            void RefreshComponentChoices()
            {
                componentTypes.Clear();
                componentOptions.Clear();

                if (!hasTargetModel || !(targetProp.objectReferenceValue is GameObject go))
                {
                    SetDisplay(compPopup, false);
                    RefreshComponentIcon();
                    return;
                }

                var types = go.GetComponents<Component>()
                              .Where(c => c != null)
                              .Select(c => c.GetType())
                              .Distinct()
                              .OrderBy(t => t.Name)
                              .ToList();

                if (!types.Contains(typeof(Transform))) types.Insert(0, typeof(Transform));
                types.Insert(0, typeof(GameObject));

                var names = new List<string> { InspectorLabels.None };
                componentOptions.Add(new GUIContent(InspectorLabels.None, image: null));

                foreach (var t in types)
                {
                    componentTypes.Add(t);

                    string displayName = t == typeof(GameObject) ? "GameObject" : t.Name;
                    names.Add(displayName);

                    Texture2D iconTex = null;
                    var iconContent = EditorGUIUtility.ObjectContent(null, t);
                    if (iconContent != null)
                        iconTex = iconContent.image as Texture2D;

                    componentOptions.Add(new GUIContent(displayName, iconTex));
                }

                Type currentType = null;
                if (!string.IsNullOrEmpty(compTypeProp.stringValue))
                    currentType = Type.GetType(compTypeProp.stringValue);

                int idxCurrent = 0;
                if (currentType != null)
                {
                    int j = componentTypes.FindIndex(t => t == currentType);
                    if (j >= 0) idxCurrent = j + 1;
                }

                compPopup.choices = names;
                compPopup.index = idxCurrent;
                SetDisplay(compPopup, true);

                RefreshComponentIcon();
            }

            void RefreshMemberChoices()
            {
                object instance = null;
                Type instanceType = null;

                if (hasTargetModel && targetProp.objectReferenceValue is GameObject go)
                {
                    Type chosenType = null;
                    if (!string.IsNullOrEmpty(compTypeProp.stringValue))
                        chosenType = Type.GetType(compTypeProp.stringValue);

                    if (chosenType == typeof(GameObject)) { instance = go; instanceType = typeof(GameObject); }
                    else if (chosenType == typeof(Transform)) { instance = go.transform; instanceType = typeof(Transform); }
                    else if (chosenType != null) { instance = go.GetComponent(chosenType); instanceType = chosenType; }
                }
                else if (sourceProp != null && sourceProp.objectReferenceValue is Component comp)
                {
                    instance = comp; instanceType = comp.GetType();
                }

                var choices = new List<string> { InspectorLabels.None };

                bool Supported(Type t)
                {
                    if (t == null) return false;
                    if (t == typeof(string) || t == typeof(bool) || t.IsEnum) return true;
                    if (typeof(IConvertible).IsAssignableFrom(t)) return true;
                    if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return true;
                    if (t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Quaternion)) return true;
                    return false;
                }

                if (instance != null && instanceType != null)
                {
                    var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                    if (instance is GameObject)
                    {
                        choices.AddRange(new[] { "GameObject", "activeSelf", "activeInHierarchy", "tag", "layer", "name" });
                    }
                    else if (instance is Transform)
                    {
                        choices.AddRange(new[]
                        {
                        "parent","root","childCount",
                        "position","localPosition",
                        "rotation","localRotation",
                        "eulerAngles","localEulerAngles",
                        "lossyScale","localScale"
                    });
                    }
                    else
                    {
                        if (instance is Behaviour) choices.Add("enabled");

                        foreach (var f in instanceType.GetFields(flags))
                            if (f.DeclaringType == instanceType &&
                                (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null) &&
                                Supported(f.FieldType))
                                choices.Add(f.Name);

                        foreach (var p in instanceType.GetProperties(flags))
                        {
                            if (p.DeclaringType != instanceType) continue;
                            if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;
                            var getter = p.GetGetMethod(true);
                            if (getter == null || !getter.IsPublic) continue;
                            if (!Supported(p.PropertyType)) continue;
                            choices.Add(p.Name);
                        }
                    }
                }

                int current = Mathf.Max(0, choices.IndexOf(memberProp.stringValue));
                memberPopup.choices = choices;
                memberPopup.index = current;
                memberChoicesCache = choices.ToList();
                memberChoicesAll = choices.ToList();
                memberPopup.choices = choices;
                memberPopup.index = current;
                SetDisplay(memberPopup, choices.Count > 1);
            }

            void ShowComponentDropdown()
            {
                if (!hasTargetModel || !(targetProp.objectReferenceValue is GameObject)) return;
                if (compPopup == null || componentOptions.Count == 0) return;

                var dropdown = new VBComponentDropdown(componentOptions, selectedIndex =>
                {
                    if (selectedIndex < 0 || selectedIndex >= compPopup.choices.Count) return;

                    compPopup.index = selectedIndex;
                    RefreshComponentIcon();
                });

                var world = compPopup.worldBound;
                if (world.width <= 0 || world.height <= 0)
                {
                    world = new Rect(0, 0, 200, EditorGUIUtility.singleLineHeight);
                }

                world.y += world.height;
                dropdown.Show(world);
            }

            if (compPopup != null)
            {
                compPopup.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0) return;

                    evt.StopImmediatePropagation();
                    evt.StopPropagation();

                    ShowComponentDropdown();
                }, TrickleDown.TrickleDown);
            }

            compPopup.RegisterValueChangedCallback(_ =>
            {
                if (!hasTargetModel) return;
                if (!(targetProp.objectReferenceValue is GameObject go)) return;

                if (compPopup.index <= 0) compTypeProp.stringValue = string.Empty;
                else
                {
                    var types = go.GetComponents<Component>()
                                  .Where(c => c != null).Select(c => c.GetType())
                                  .Distinct().OrderBy(t => t.Name).ToList();

                    if (!types.Contains(typeof(Transform))) types.Insert(0, typeof(Transform));
                    types.Insert(0, typeof(GameObject));

                    var chosen = types[compPopup.index - 1];
                    compTypeProp.stringValue = chosen.AssemblyQualifiedName;
                }
                compTypeProp.serializedObject.ApplyModifiedProperties();

                memberProp.stringValue = string.Empty;
                memberProp.serializedObject.ApplyModifiedProperties();

                RefreshMemberChoices();
                RefreshComponentIcon();
            });

            memberPopup.RegisterValueChangedCallback(_ =>
            {
                var i = memberPopup.index;
                var value = (i <= 0 || i >= memberPopup.choices.Count) ? string.Empty : memberPopup.choices[i];
                memberProp.stringValue = value;
                memberProp.serializedObject.ApplyModifiedProperties();
            });

            SetDisplay(targetField, hasTargetModel);
            SetDisplay(sourceField, false);

            root.TrackPropertyValue(targetProp, _ =>
            {
                var obj = targetProp.objectReferenceValue;

                if (obj != null && !(obj is GameObject))
                {
                    var go = (obj as Component)?.gameObject;
                    targetProp.objectReferenceValue = go;
                    targetProp.serializedObject.ApplyModifiedProperties();
                }

                if (targetProp.objectReferenceValue == null)
                {
                    if (!string.IsNullOrEmpty(compTypeProp.stringValue))
                    {
                        compTypeProp.stringValue = string.Empty;
                        compTypeProp.serializedObject.ApplyModifiedProperties();
                    }
                    if (!string.IsNullOrEmpty(memberProp.stringValue))
                    {
                        memberProp.stringValue = string.Empty;
                        memberProp.serializedObject.ApplyModifiedProperties();
                    }
                }

                RefreshComponentChoices();
                RefreshMemberChoices();
            });

            if (sourceProp != null) root.TrackPropertyValue(sourceProp, _ => { RefreshMemberChoices(); });

            findField.RegisterCallback<KeyUpEvent>(_ =>
            {
                ApplyMemberFilter(findField.value ?? string.Empty);
            });

            RefreshComponentChoices();
            RefreshMemberChoices();

            // Font Apply
            {
                root.style.unityFontDefinition = new StyleFontDefinition(FSEditorUI.mainFont);
            }

            return root;
        }

        SerializedProperty FindSibling(SerializedProperty self, string siblingName)
        {
            var path = self.propertyPath;
            int lastDot = path.LastIndexOf('.');
            if (lastDot < 0) return null;
            var parent = path.Substring(0, lastDot);
            return self.serializedObject.FindProperty(parent + "." + siblingName);
        }

        class VBComponentDropdownItem : AdvancedDropdownItem
        {
            public readonly int index;

            public VBComponentDropdownItem(string name, Texture2D icon, int index)
                : base(name)
            {
                this.icon = icon;
                this.index = index;
            }
        }

        class VBComponentDropdown : AdvancedDropdown
        {
            readonly List<GUIContent> _options;
            readonly Action<int> _onSelected;

            public VBComponentDropdown(List<GUIContent> options, Action<int> onSelected)
                : base(sComponentDropdownState)
            {
                _options = options;
                _onSelected = onSelected;
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Components");

                if (_options != null)
                {
                    for (int i = 0; i < _options.Count; i++)
                    {
                        var opt = _options[i];
                        var iconTex = opt.image as Texture2D;
                        var item = new VBComponentDropdownItem(opt.text, iconTex, i);
                        root.AddChild(item);
                    }
                }

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is VBComponentDropdownItem cItem)
                {
                    _onSelected?.Invoke(cItem.index);
                }
            }
        }

    }
}
#endif
