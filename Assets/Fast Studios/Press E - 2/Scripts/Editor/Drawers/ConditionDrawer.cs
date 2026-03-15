#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastStudios.EditorTools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FastStudios
{
    [CustomPropertyDrawer(typeof(Condition))]
    public class ConditionDrawer : PropertyDrawer
    {
        public static VisualTreeAsset UXML = Resources.Load<VisualTreeAsset>("FastStudios/ForEditor/UXML/ConditionUXML");
        public static StyleSheet USS = Resources.Load<StyleSheet>("FastStudios/ForEditor/USS/ConditionUSS");
        Color ConditionAttended = new Color(0.20f, 0.80f, 0.30f, 1f);
        Color ConditionNotAttended = new Color(0.85f, 0.20f, 0.20f, 1f);

        public override VisualElement CreatePropertyGUI(SerializedProperty prop)
        {
            VisualElement root = new VisualElement();
            UXML.CloneTree(root);

            root.styleSheets.Add(USS);
            root.Bind(prop.serializedObject);

            root.name = "ConditionRoot";
            var rightBinding = root.Q<VisualElement>("rightBinding");
            var constContainer = root.Q<VisualElement>("constContainer");
            var allConstPF = constContainer.Query<PropertyField>().ToList();

            var LiveStatus = root.Q<VisualElement>("LiveStatus");
            var rightDynamicProp = prop.FindPropertyRelative("RightSideDynamic");
            var isPlaying = prop.FindPropertyRelative("IsPlaying");

            ConstValue(root, prop, allConstPF, rightDynamicProp);

            SetupLiveStatus(root, prop, LiveStatus);

            ShowIfBool(root, rightDynamicProp, true, rightBinding);
            ShowIfBool(root, rightDynamicProp, false, constContainer);

            SetupOperator(root, prop, rightBinding, constContainer, rightDynamicProp);

            SetupRightSideCompatibilityFilter(root, prop, rightBinding, rightDynamicProp);

            var joinField = root.Q<VisualElement>("JoinField");
            SetupJoinVisibility(root, prop, joinField);

            KeyModeHandler(root, prop);

            // Font Apply
            {
                root.style.unityFontDefinition = new StyleFontDefinition(FSEditorUI.mainFont);
            }

            return root;
        }

        void KeyModeHandler(VisualElement root, SerializedProperty prop)
        {
            var normalRoot = root.Q<VisualElement>("NormalCondition");
            var keyRoot = root.Q<VisualElement>("KeyCondition");

            var keyBtnNormal = normalRoot?.Q<VisualElement>("KeyButton");
            var keyBtnKey = keyRoot?.Q<VisualElement>("KeyButton");

            var onlyKeyProp = prop.FindPropertyRelative("UseKey");
            var keyMethodProp = prop.FindPropertyRelative("KeyCheckMethod");
            KeyAccept mode = (KeyAccept)keyMethodProp.enumValueIndex;

            void SetVisible(VisualElement ve, bool on)
            {
                if (ve == null) return;
                ve.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void RefreshMode()
            {
                bool on = onlyKeyProp.boolValue;
                SetVisible(normalRoot, !on);
                SetVisible(keyRoot, on);
                keyBtnNormal?.EnableInClassList("toggled", on);
                keyBtnKey?.EnableInClassList("toggled", on);
            }

            PropertyField FindPFInScope(VisualElement scope, string tail)
            {
                if (scope == null) return null;
                return scope.Query<PropertyField>().ToList()
                    .FirstOrDefault(pf => !string.IsNullOrEmpty(pf.bindingPath)
                                       && pf.bindingPath.EndsWith(tail, StringComparison.Ordinal));
            }

            bool _keyRefreshQueued = false;

            void RequestKeyVarsRefresh()
            {
                if (_keyRefreshQueued) return;
                _keyRefreshQueued = true;

                root.schedule.Execute(() =>
                {
                    keyMethodProp.serializedObject.Update();

                    var keyVars = keyRoot?.Q<VisualElement>("KeyVars");

                    var pfName = FindPFInScope(keyVars, "KeyName");
                    var pfSpecKey = FindPFInScope(keyVars, "SpecificKey");

                    if (pfName == null || pfSpecKey == null)
                    {
                        RequestKeyVarsRefresh();
                        return;
                    }

                    var nameProp = prop.FindPropertyRelative("KeyName");
                    pfName.Unbind();
                    pfName.BindProperty(nameProp);

                    var specProp = prop.FindPropertyRelative("SpecificKey");
                    pfSpecKey.Unbind();
                    pfSpecKey.BindProperty(specProp);

                    SetVisible(pfName, mode == KeyAccept.KeyName);
                    SetVisible(pfSpecKey, mode == KeyAccept.SpecificKey);

                    _keyRefreshQueued = false;
                });
            }

            void ToggleKeyMode()
            {
                onlyKeyProp.boolValue = !onlyKeyProp.boolValue;
                onlyKeyProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                RefreshMode();
                RequestKeyVarsRefresh();
            }

            void BindKeyBtn(VisualElement ve)
            {
                if (ve == null) return;
                ve.pickingMode = PickingMode.Position;
                ve.focusable = true;
                ve.AddManipulator(new Clickable(ToggleKeyMode));
            }
            BindKeyBtn(keyBtnNormal);
            BindKeyBtn(keyBtnKey);

            void BindKeyMethodEnum()
            {
                var keyVars = keyRoot?.Q<VisualElement>("KeyVars");
                var pfMethod = FindPFInScope(keyVars, "KeyCheckMethod");

                if (pfMethod != null)
                {
                    pfMethod.RegisterCallback<SerializedPropertyChangeEvent>(e =>
                    {
                        mode = (KeyAccept)e.changedProperty.enumValueIndex;
                        keyMethodProp.serializedObject.Update();
                        RequestKeyVarsRefresh();
                    });
                }
                else
                {
                    root.schedule.Execute(BindKeyMethodEnum);
                }
            }

            BindKeyMethodEnum();

            root.TrackPropertyValue(keyMethodProp, _ => RequestKeyVarsRefresh());
            root.TrackPropertyValue(onlyKeyProp, _ => RefreshMode());

            root.RegisterCallback<AttachToPanelEvent>(_ => { RefreshMode(); RequestKeyVarsRefresh(); });
            root.RegisterCallback<GeometryChangedEvent>(_ => RequestKeyVarsRefresh());

            RefreshMode();
            RequestKeyVarsRefresh();
        }


        static bool IsUnary(ConditionOperator op) =>
                                                        op == ConditionOperator.IsTrue ||
                                                        op == ConditionOperator.IsFalse ||
                                                        op == ConditionOperator.IsNull ||
                                                        op == ConditionOperator.NotNull;

        void SetupOperator(VisualElement root,
                           SerializedProperty conditionProp,
                           VisualElement rightBinding,
                           VisualElement constContainer,
                           SerializedProperty rightDynamicProp)
        {
            var opProp = conditionProp.FindPropertyRelative("op");
            var left = conditionProp.FindPropertyRelative("left");
            var numberP = conditionProp.FindPropertyRelative("number");

            var opHost = root.Q<VisualElement>("opField");
            if (opHost == null) return;

            void InitialRefresh()
            {
                if (root.panel != null)
                    BuildPopup();
            }
            var magnitudeEl = root.Q<VisualElement>("MagnitudeText");

            bool IsMagnitudeOp(ConditionOperator op) =>
                op == ConditionOperator.Greater ||
                op == ConditionOperator.GreaterOrEqual ||
                op == ConditionOperator.Less ||
                op == ConditionOperator.LessOrEqual;

            void RefreshMagnitudeLabel()
            {
                if (magnitudeEl == null) return;

                var lt = GetLeftValueType(left);
                var op = (ConditionOperator)opProp.enumValueIndex;

                bool isVec = lt == typeof(Vector2) || lt == typeof(Vector3);
                bool show = isVec && IsMagnitudeOp(op);

                SetVisible(show, "HideAlt", magnitudeEl);
            }


            root.RegisterCallback<AttachToPanelEvent>(_ => InitialRefresh());

            root.schedule.Execute(InitialRefresh);

            void DelayEditorPass()
            {
                if (root.panel != null)
                    BuildPopup();
            }
            EditorApplication.delayCall += DelayEditorPass;
            root.RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.delayCall -= DelayEditorPass);

            BuildPopup();

            void BuildPopup()
            {
                if (!IsAlive(conditionProp)) return;

                var leftType = GetLeftValueType(left);
                if (!IsAlive(left)) return;

                var allowed = AllowedOpsFor(leftType);

                if (leftType == typeof(bool) && rightDynamicProp != null && rightDynamicProp.boolValue)
                    allowed = allowed.Where(o => o != ConditionOperator.IsTrue && o != ConditionOperator.IsFalse).ToList();

                if (leftType == typeof(bool) && rightDynamicProp != null && !rightDynamicProp.boolValue)
                    allowed = allowed.Where(o => o != ConditionOperator.Equal && o != ConditionOperator.NotEqual).ToList();

                var current = (ConditionOperator)opProp.enumValueIndex;
                if (!allowed.Contains(current))
                {
                    current = allowed[0];
                    opProp.enumValueIndex = (int)current;
                    opProp.serializedObject.ApplyModifiedProperties();
                }

                var newPopup = new PopupField<ConditionOperator>(
                    label: string.Empty,
                    choices: allowed,
                    defaultValue: current,
                    formatSelectedValueCallback: GetInspectorName,
                    formatListItemCallback: GetInspectorName
                );

                newPopup.RegisterValueChangedCallback(evt =>
                {
                    var newOp = evt.newValue;
                    if ((ConditionOperator)opProp.enumValueIndex != newOp)
                    {
                        opProp.enumValueIndex = (int)newOp;
                        opProp.serializedObject.ApplyModifiedProperties();
                    }
                    RefreshRightVisibility();
                });

                opHost.Clear();
                opHost.Add(newPopup);

                var lt = IsAlive(conditionProp) ? GetLeftValueType(conditionProp.FindPropertyRelative("left")) : null;
                EnforceIntegerNumberIfNeeded(lt);
                RefreshRightVisibility();
            }

            void RefreshRightVisibility()
            {
                if (!IsAlive(conditionProp)) return;
                var left = conditionProp.FindPropertyRelative("left");
                if (!IsAlive(left)) { SetVisible(false, "HideAlt", rightBinding, constContainer, opHost); return; }

                var leftTargetSp = left.FindPropertyRelative("target");
                var leftMemberSp = left.FindPropertyRelative("member");
                var leftType = GetLeftValueType(left);

                bool leftReady =
                    leftTargetSp != null && leftTargetSp.objectReferenceValue != null &&
                    leftMemberSp != null && !string.IsNullOrEmpty(leftMemberSp.stringValue) &&
                    leftType != null;

                SetVisible(leftReady, "HideAlt", opHost);

                if (!leftReady) { SetVisible(false, "HideAlt", rightBinding, constContainer); return; }

                var needsRight = !IsUnary((ConditionOperator)opProp.enumValueIndex);
                var showBinding = needsRight && rightDynamicProp != null && rightDynamicProp.boolValue;
                var showConst = needsRight && !(rightDynamicProp != null && rightDynamicProp.boolValue);

                SetVisible(showBinding, "HideAlt", rightBinding);
                SetVisible(showConst, "HideAlt", constContainer);
            }

            void EnforceIntegerNumberIfNeeded(Type leftType)
            {
                bool intLike = leftType != null && (IsIntLike(leftType) || leftType.IsEnum);
                if (intLike && numberP != null)
                {
                    var v = numberP.floatValue;
                    var rounded = Mathf.Round(v);
                    if (Mathf.Abs(v - rounded) > 0.0001f)
                    {
                        numberP.floatValue = rounded;
                        numberP.serializedObject.ApplyModifiedProperties();
                    }

                    var pfNumber = constContainer.Query<PropertyField>()
                                                 .ToList()
                                                 .FirstOrDefault(p => p.bindingPath == "number");
                }
                else
                {
                    var pfNumber = constContainer.Query<PropertyField>()
                                                 .ToList()
                                                 .FirstOrDefault(p => p.bindingPath == "number");
                    if (pfNumber != null) pfNumber.tooltip = string.Empty;
                }
            }

            void Hook(SerializedProperty sp, Action cb) { if (sp != null) root.TrackPropertyValue(sp, _ => cb()); }

            Hook(opProp, RefreshMagnitudeLabel);

            var leftTarget = left.FindPropertyRelative("target");
            var leftSource = left.FindPropertyRelative("source");
            var leftTypeName = left.FindPropertyRelative("componentTypeName");
            var leftMember = left.FindPropertyRelative("member");

            Hook(leftTarget, BuildPopup);
            Hook(leftSource, BuildPopup);
            Hook(leftTypeName, BuildPopup);
            Hook(leftMember, BuildPopup);
            Hook(rightDynamicProp, BuildPopup);

            Hook(opProp, RefreshRightVisibility);
            Hook(rightDynamicProp, RefreshRightVisibility);
            Hook(numberP, () => EnforceIntegerNumberIfNeeded(GetLeftValueType(left)));

            root.RegisterCallback<AttachToPanelEvent>(_ => RefreshMagnitudeLabel());
            root.schedule.Execute(RefreshMagnitudeLabel);
            EditorApplication.delayCall += RefreshMagnitudeLabel;
            root.RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.delayCall -= RefreshMagnitudeLabel);

            BuildPopup();
            RefreshMagnitudeLabel();
        }

        void ConstValue(VisualElement root, SerializedProperty prop, List<PropertyField> allConstPF, SerializedProperty rightDynamicProp)
        {
            PropertyField Pf(string path) => allConstPF.FirstOrDefault(p => p.bindingPath == path);

            var pfNumber = Pf("number");
            var pfString = Pf("str");
            var pfBool = Pf("boolean");
            var pfGameObj = Pf("GameObject");
            var pfObj = Pf("obj");
            var pfV2 = Pf("vector2");
            var pfV3 = Pf("vector3");
            var pfQuat = Pf("quaternionConst");

            void ShowOnly(PropertyField which)
            {
                foreach (var x in new[] { pfNumber, pfString, pfBool, pfObj, pfGameObj, pfV2, pfV3, pfQuat })
                    if (x != null) x.EnableInClassList("HideAlt", x != which);
            }

            var left = prop.FindPropertyRelative("left");
            var leftTarget = left.FindPropertyRelative("target");
            var leftSource = left.FindPropertyRelative("source");
            var leftTypeName = left.FindPropertyRelative("componentTypeName");
            var leftMember = left.FindPropertyRelative("member");
            var opProp = prop.FindPropertyRelative("op");

            void RefreshConstVisibility()
            {
                if (rightDynamicProp != null && rightDynamicProp.boolValue) return;

                if (!IsAlive(prop)) { ShowOnly(null); return; }
                var left = prop.FindPropertyRelative("left");
                if (!IsAlive(left)) { ShowOnly(null); return; }

                var t = GetLeftValueType(left);
                var op = (ConditionOperator)opProp.enumValueIndex;

                if (t == null) { ShowOnly(null); return; }

                if (t == typeof(Vector2) || t == typeof(Vector3))
                {
                    bool wantsVectorConst = (op == ConditionOperator.Equal || op == ConditionOperator.NotEqual);
                    if (t == typeof(Vector2)) ShowOnly(wantsVectorConst ? pfV2 : pfNumber);
                    else ShowOnly(wantsVectorConst ? pfV3 : pfNumber);
                    return;
                }

                if (t == typeof(string)) ShowOnly(pfString);
                else if (t == typeof(bool)) ShowOnly(pfBool);
                else if (t == typeof(Quaternion)) ShowOnly(pfQuat);
                else if (t != null && typeof(GameObject).IsAssignableFrom(t)) ShowOnly(pfGameObj);
                else if (t != null && typeof(UnityEngine.Object).IsAssignableFrom(t)) ShowOnly(pfObj);
                else ShowOnly(pfNumber);
            }

            void Hook(SerializedProperty sp) { if (sp != null) root.TrackPropertyValue(sp, _ => RefreshConstVisibility()); }
            Hook(rightDynamicProp);
            Hook(leftTarget);
            Hook(leftSource);
            Hook(leftTypeName);
            Hook(leftMember);
            Hook(opProp);

            RefreshConstVisibility();
        }

        static string GetInspectorName(ConditionOperator op)
        {
            var fi = typeof(ConditionOperator).GetField(op.ToString());
            if (fi == null) return op.ToString();
            var attr = fi.GetCustomAttributes(typeof(InspectorNameAttribute), false)
                         .OfType<InspectorNameAttribute>()
                         .FirstOrDefault();
            return attr != null ? attr.displayName : op.ToString();
        }

        static bool IsIntLike(Type t) =>
            t == typeof(sbyte) || t == typeof(byte) ||
            t == typeof(short) || t == typeof(ushort) ||
            t == typeof(int) || t == typeof(uint) ||
            t == typeof(long) || t == typeof(ulong);

        static void ShowIfBool(VisualElement scope, SerializedProperty boolProp, bool showWhenTrue, params VisualElement[] targets)
        {
            if (boolProp == null) { SetVisible(false, "HideAlt", targets); return; }

            void Refresh()
            {
                bool show = boolProp.boolValue == showWhenTrue;
                SetVisible(show, "HideAlt", targets);
            }

            scope.TrackPropertyValue(boolProp, _ => Refresh());
            Refresh();
        }

        static void SetVisible(bool show, string hideClass, params VisualElement[] targets)
        {
            if (targets == null) return;
            foreach (var t in targets)
            {
                if (t == null) continue;
                t.EnableInClassList(hideClass, !show);
            }
        }

        static List<ConditionOperator> AllowedOpsFor(Type t)
        {
            if (t == null) return new List<ConditionOperator> { ConditionOperator.Equal, ConditionOperator.NotEqual };

            if (t == typeof(string))
                return new List<ConditionOperator> { ConditionOperator.Equal, ConditionOperator.NotEqual, ConditionOperator.Contains, ConditionOperator.StartsWith, ConditionOperator.EndsWith };

            if (t == typeof(bool))
                return new List<ConditionOperator> { ConditionOperator.IsTrue, ConditionOperator.IsFalse, ConditionOperator.Equal, ConditionOperator.NotEqual };

            if (t == typeof(Vector2))
                return new List<ConditionOperator>
            {
                ConditionOperator.Equal, ConditionOperator.NotEqual,
                ConditionOperator.Greater, ConditionOperator.GreaterOrEqual, ConditionOperator.Less, ConditionOperator.LessOrEqual,
                ConditionOperator.XEqual, ConditionOperator.YEqual
            };

            if (t == typeof(Vector3))
                return new List<ConditionOperator>
            {
                ConditionOperator.Equal, ConditionOperator.NotEqual,
                ConditionOperator.Greater, ConditionOperator.GreaterOrEqual, ConditionOperator.Less, ConditionOperator.LessOrEqual,
                ConditionOperator.XEqual, ConditionOperator.YEqual, ConditionOperator.ZEqual
            };

            if (t == typeof(Quaternion))
                return new List<ConditionOperator> { ConditionOperator.Equal, ConditionOperator.NotEqual };

            if (typeof(UnityEngine.Object).IsAssignableFrom(t) || t == typeof(GameObject))
                return new List<ConditionOperator> { ConditionOperator.Equal, ConditionOperator.NotEqual, ConditionOperator.IsNull, ConditionOperator.NotNull };

            if (t.IsEnum || typeof(IConvertible).IsAssignableFrom(t))
                return new List<ConditionOperator> { ConditionOperator.Equal, ConditionOperator.NotEqual, ConditionOperator.Greater, ConditionOperator.GreaterOrEqual, ConditionOperator.Less, ConditionOperator.LessOrEqual };

            return new List<ConditionOperator> { ConditionOperator.Equal, ConditionOperator.NotEqual };
        }

        static Type GetLeftValueType(SerializedProperty left)
        {
            if (!IsAlive(left)) return null;
            if (left == null) return null;

            try
            {
                var target = left.FindPropertyRelative("target")?.objectReferenceValue;
                var source = left.FindPropertyRelative("source")?.objectReferenceValue as Component;
                var compStr = left.FindPropertyRelative("componentTypeName")?.stringValue;
                var member = left.FindPropertyRelative("member")?.stringValue;

                object instance = null; Type instType = null;

                if (target is GameObject go)
                {
                    var t = string.IsNullOrEmpty(compStr) ? null : Type.GetType(compStr);
                    if (t == typeof(GameObject)) { instance = go; instType = typeof(GameObject); }
                    else if (t == typeof(Transform)) { instance = go.transform; instType = typeof(Transform); }
                    else if (t != null) { instance = go.GetComponent(t); instType = t; }
                }
                else if (target is Component compT)
                {
                    instance = compT; instType = compT.GetType();
                }
                else if (source != null)
                {
                    instance = source; instType = source.GetType();
                }

                if (instance == null || instType == null || string.IsNullOrEmpty(member)) return null;

                if (instType == typeof(GameObject) && member == "GameObject")
                    return typeof(GameObject);

                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var fi = instType.GetField(member, flags);
                if (fi != null) return fi.FieldType;

                var pi = instType.GetProperty(member, flags);
                if (pi != null && pi.CanRead) return pi.PropertyType;

                return null;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }

        void SetupJoinVisibility(VisualElement root, SerializedProperty itemProp, VisualElement joinField)
        {
            if (joinField == null) return;

            var itemsArray = GetItemsArrayProp(itemProp);
            var joinsArray = GetJoinsArrayProp(itemProp);
            var joinPF = joinField as PropertyField ?? joinField.Q<PropertyField>();

            void EnsureSizeAtLeast()
            {
                if (itemsArray == null || joinsArray == null) return;
                int want = Math.Max(0, itemsArray.arraySize - 1);
                if (joinsArray.arraySize < want)
                {
                    while (joinsArray.arraySize < want)
                    {
                        joinsArray.InsertArrayElementAtIndex(joinsArray.arraySize);
                        var e = joinsArray.GetArrayElementAtIndex(joinsArray.arraySize - 1);
                        e.enumValueIndex = (int)LogicalJoin.And;
                    }
                    joinsArray.serializedObject.ApplyModifiedProperties();
                }
            }

            void Refresh()
            {
                if (itemsArray == null || joinPF == null) return;
                EnsureSizeAtLeast();

                int idx = GetItemIndex(itemProp);
                int size = itemsArray.arraySize;

                bool showHere = idx > 0 && idx < size;
                SetVisible(showHere, "HideJoin", joinField);
                if (!showHere) { joinPF.Unbind(); return; }

                if (joinsArray != null && joinsArray.arraySize >= idx)
                {
                    var joinEl = joinsArray.GetArrayElementAtIndex(idx - 1);
                    joinPF.Unbind();
                    joinPF.BindProperty(joinEl);
                }
            }

            var upBtn = root.Q<VisualElement>("UpButton");
            var downBtn = root.Q<VisualElement>("DownButton");
            var delBtn = root.Q<VisualElement>("RemoveButton");
            SetupItemButtons(root, itemProp, upBtn, downBtn, delBtn);

            if (itemsArray != null) root.TrackPropertyValue(itemsArray, _ => Refresh());
            if (joinsArray != null) root.TrackPropertyValue(joinsArray, _ => Refresh());
            root.RegisterCallback<AttachToPanelEvent>(_ => Refresh());
            root.schedule.Execute(Refresh);
            EditorApplication.delayCall += Refresh;
            root.RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.delayCall -= Refresh);
        }

        void SetupLiveStatus(VisualElement root, SerializedProperty prop, VisualElement liveStatus)
        {
            if (liveStatus == null) return;

            void Refresh()
            {
                bool playing = Application.isPlaying;
                SetVisible(playing, "HideAlt", liveStatus);
                if (!playing) return;

                var cond = GetObjectAtPath<Condition>(prop.serializedObject.targetObject, prop.propertyPath);

                bool ok = false;
                if (cond != null)
                {
                    var g = new ConditionGroup { items = new List<Condition> { cond } };
                    ok = ConditionRuntime.Evaluate(g);
                }

                liveStatus.style.backgroundColor = ok
                    ? ConditionAttended
                    : ConditionNotAttended;

                liveStatus.tooltip = ok ? "Condition Accepted" : "Condition Declined";
            }

            root.RegisterCallback<AttachToPanelEvent>(_ => Refresh());
            root.schedule.Execute(Refresh).Every(200);
        }

        static T GetObjectAtPath<T>(object rootObj, string path) where T : class
        {
            if (rootObj == null || string.IsNullOrEmpty(path)) return null;

            object obj = rootObj;
            var elements = path.Replace(".Array.data[", "[").Split('.');

            foreach (var raw in elements)
            {
                if (string.IsNullOrEmpty(raw)) continue;

                int b = raw.IndexOf('[');
                if (b >= 0)
                {
                    var fieldName = raw.Substring(0, b);
                    var idxStr = raw.Substring(b + 1).TrimEnd(']');

                    obj = GetMemberValue(obj, fieldName);
                    if (!(obj is System.Collections.IList list)) return null;

                    if (!int.TryParse(idxStr, out int idx) || idx < 0 || idx >= list.Count) return null;
                    obj = list[idx];
                }
                else
                {
                    obj = GetMemberValue(obj, raw);
                }

                if (obj == null) return null;
            }

            return obj as T;
        }

        static object GetMemberValue(object obj, string member)
        {
            if (obj == null || string.IsNullOrEmpty(member)) return null;

            var t = obj.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var fi = t.GetField(member, flags);
            if (fi != null) return fi.GetValue(obj);

            var pi = t.GetProperty(member, flags);
            if (pi != null && pi.CanRead) return pi.GetValue(obj);

            return null;
        }


        #region Helpers

        void SetupItemButtons(VisualElement root,
                      SerializedProperty itemProp,
                      VisualElement upBtn, VisualElement downBtn, VisualElement delBtn)
        {
            if (upBtn == null && downBtn == null && delBtn == null) return;

            var itemsArray = GetItemsArrayProp(itemProp);
            if (itemsArray == null) return;

            bool dead = false;

            void DisableAll()
            {
                upBtn?.SetEnabled(false);
                downBtn?.SetEnabled(false);
                delBtn?.SetEnabled(false);
            }

            bool IsAlive() => GetItemIndex(itemProp) >= 0;

            void BindClick(VisualElement ve, Action action)
            {
                if (ve == null || action == null) return;
                ve.pickingMode = PickingMode.Position;
                ve.focusable = true;
                ve.AddManipulator(new Clickable(() =>
                {
                    if (dead) return;
                    action();
                }));
            }

            void RefreshButtons()
            {
                if (dead || root.panel == null) return;

                int idx = GetItemIndex(itemProp);
                if (idx < 0)
                {
                    dead = true;
                    DisableAll();
                    return;
                }

                int size = itemsArray.arraySize;
                upBtn?.SetEnabled(idx > 0);
                downBtn?.SetEnabled(idx >= 0 && idx < size - 1);
                delBtn?.SetEnabled(idx >= 0 && size > 0);
            }

            BindClick(upBtn, () =>
            {
                itemProp.serializedObject.Update();
                int idx = GetItemIndex(itemProp);
                if (idx > 0)
                {
                    itemsArray.MoveArrayElement(idx, idx - 1);
                    itemProp.serializedObject.ApplyModifiedProperties();
                }
                if (IsAlive()) root.schedule.Execute(RefreshButtons);
            });

            BindClick(downBtn, () =>
            {
                itemProp.serializedObject.Update();
                int idx = GetItemIndex(itemProp);
                int size = itemsArray.arraySize;
                if (idx >= 0 && idx < size - 1)
                {
                    itemsArray.MoveArrayElement(idx, idx + 1);
                    itemProp.serializedObject.ApplyModifiedProperties();
                }
                if (IsAlive()) root.schedule.Execute(RefreshButtons);
            });

            BindClick(delBtn, () =>
            {
                itemProp.serializedObject.Update();

                int idx = GetItemIndex(itemProp);
                var itemsArray = GetItemsArrayProp(itemProp);
                var joinsArray = GetJoinsArrayProp(itemProp);

                if (idx >= 0 && idx < itemsArray.arraySize)
                {
                    itemsArray.DeleteArrayElementAtIndex(idx);

                    if (joinsArray != null && joinsArray.arraySize > 0)
                    {
                        int removeAt = (idx < joinsArray.arraySize) ? idx : (joinsArray.arraySize - 1);
                        if (removeAt >= 0) joinsArray.DeleteArrayElementAtIndex(removeAt);
                    }

                    itemProp.serializedObject.ApplyModifiedProperties();
                }

                dead = true;
                DisableAll();
            });

            root.TrackPropertyValue(itemsArray, _ => RefreshButtons());
            root.RegisterCallback<AttachToPanelEvent>(_ => RefreshButtons());
            root.schedule.Execute(RefreshButtons);
        }

        static int GetItemIndex(SerializedProperty item)
        {
            try
            {
                const string marker = "Array.data[";
                var path = item.propertyPath;
                int i = path.LastIndexOf(marker, StringComparison.Ordinal);
                if (i < 0) return -1;
                int start = i + marker.Length;
                int end = path.IndexOf(']', start);
                if (end < 0) return -1;
                var n = path.Substring(start, end - start);
                return int.TryParse(n, out var idx) ? idx : -1;
            }
            catch (ObjectDisposedException)
            {
                return -1;
            }
        }

        static bool IsAlive(SerializedProperty p)
        {
            if (p == null) return false;
            try { var _ = p.propertyPath; return true; }
            catch (ObjectDisposedException) { return false; }
        }

        static SerializedProperty GetJoinsArrayProp(SerializedProperty item)
        {
            var items = GetItemsArrayProp(item);
            if (items == null) return null;
            var path = items.propertyPath;
            int i = path.LastIndexOf(".items", StringComparison.Ordinal);
            if (i < 0) return null;
            var groupPath = path.Substring(0, i);
            return item.serializedObject.FindProperty(groupPath + ".joins");
        }

        static SerializedProperty GetItemsArrayProp(SerializedProperty item)
        {
            const string marker = ".Array.data[";
            var path = item.propertyPath;
            int i = path.LastIndexOf(marker, StringComparison.Ordinal);
            if (i < 0) return null;
            var arrayPath = path.Substring(0, i);
            return item.serializedObject.FindProperty(arrayPath);
        }

        #region Dynamic Type Check

        static bool IsNumeric(Type t)
        {
            if (t == null) return false;
            if (t == typeof(string) || t == typeof(bool)) return false;
            return typeof(IConvertible).IsAssignableFrom(t);
        }

        static bool IsUnityRef(Type t) => t != null && typeof(UnityEngine.Object).IsAssignableFrom(t);

        static bool RightTypeIsCompatible(Type left, ConditionOperator op, Type right)
        {
            if (left == null || right == null) return true;
            if (IsUnary(op)) return false;

            if (left == typeof(string)) return right == typeof(string);
            if (left == typeof(bool)) return right == typeof(bool);

            if (left == typeof(Vector2) || left == typeof(Vector3))
            {
                bool vectorCompare = op == ConditionOperator.Equal || op == ConditionOperator.NotEqual;
                bool axisCompare = op == ConditionOperator.XEqual || op == ConditionOperator.YEqual || op == ConditionOperator.ZEqual;

                if (vectorCompare) return right == left;
                if (axisCompare) return IsNumeric(right);
                return IsNumeric(right);
            }

            if (left == typeof(Quaternion)) return right == typeof(Quaternion);

            if (typeof(UnityEngine.Object).IsAssignableFrom(left))
                return typeof(UnityEngine.Object).IsAssignableFrom(right);

            if (left.IsEnum) return right.IsEnum && right == left;

            return IsNumeric(right);
        }

        static Dictionary<string, Type> GetRightMembersMap(SerializedProperty valueBindingSp)
        {
            var map = new Dictionary<string, Type> { { InspectorLabels.None, null } };

            var targetProp = valueBindingSp.FindPropertyRelative("target");
            var compType = valueBindingSp.FindPropertyRelative("componentTypeName");
            var sourceProp = valueBindingSp.FindPropertyRelative("source");

            object instance = null; Type instType = null;

            if (targetProp?.objectReferenceValue is GameObject go)
            {
                var chosen = string.IsNullOrEmpty(compType.stringValue) ? null : Type.GetType(compType.stringValue);
                if (chosen == typeof(GameObject)) { instance = go; instType = typeof(GameObject); }
                else if (chosen == typeof(Transform)) { instance = go.transform; instType = typeof(Transform); }
                else if (chosen != null) { instance = go.GetComponent(chosen); instType = chosen; }
            }
            else if (sourceProp?.objectReferenceValue is Component comp)
            {
                instance = comp; instType = comp.GetType();
            }

            bool Supported(Type t)
            {
                if (t == null) return false;
                if (t == typeof(string) || t == typeof(bool) || t.IsEnum) return true;
                if (typeof(IConvertible).IsAssignableFrom(t)) return true;
                if (IsUnityRef(t)) return true;
                if (t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Quaternion)) return true;
                return false;
            }

            if (instance == null || instType == null) return map;

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            if (instance is GameObject)
            {
                map["GameObject"] = typeof(GameObject);
                map["activeSelf"] = typeof(bool);
                map["activeInHierarchy"] = typeof(bool);
                map["tag"] = typeof(string);
                map["layer"] = typeof(int);
                map["name"] = typeof(string);
            }
            else if (instance is Transform)
            {
                map["parent"] = typeof(Transform);
                map["root"] = typeof(Transform);
                map["childCount"] = typeof(int);
                map["position"] = typeof(Vector3);
                map["localPosition"] = typeof(Vector3);
                map["rotation"] = typeof(Quaternion);
                map["localRotation"] = typeof(Quaternion);
                map["eulerAngles"] = typeof(Vector3);
                map["localEulerAngles"] = typeof(Vector3);
                map["lossyScale"] = typeof(Vector3);
                map["localScale"] = typeof(Vector3);
            }
            else
            {
                if (instance is Behaviour) map["enabled"] = typeof(bool);

                foreach (var f in instType.GetFields(flags))
                    if (f.DeclaringType == instType &&
                        (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null) &&
                        Supported(f.FieldType))
                        map[f.Name] = f.FieldType;

                foreach (var p in instType.GetProperties(flags))
                {
                    if (p.DeclaringType != instType) continue;
                    if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;
                    var getter = p.GetGetMethod(true);
                    if (getter == null || !getter.IsPublic) continue;
                    if (!Supported(p.PropertyType)) continue;
                    map[p.Name] = p.PropertyType;
                }
            }

            return map;
        }

        void SetupRightSideCompatibilityFilter(VisualElement root,
                                               SerializedProperty conditionProp,
                                               VisualElement rightBinding,
                                               SerializedProperty rightDynamicProp)
        {
            var left = conditionProp.FindPropertyRelative("left");
            var right = conditionProp.FindPropertyRelative("right");
            var opProp = conditionProp.FindPropertyRelative("op");
            var rightMember = right.FindPropertyRelative("member");

            PopupField<string> memberPopup = null;
            void EnsurePopup()
            {
                if (memberPopup == null)
                    memberPopup = rightBinding.Q<PopupField<string>>("MemberPopup");
            }

            void Refresh()
            {
                EnsurePopup();
                if (memberPopup == null) return;

                var op = (ConditionOperator)opProp.enumValueIndex;
                bool needsRight = !IsUnary(op);
                if (!needsRight || rightDynamicProp == null || !rightDynamicProp.boolValue)
                    return;

                var leftType = GetLeftValueType(left);
                var map = GetRightMembersMap(right);

                var names = new List<string> { InspectorLabels.None };
                foreach (var kv in map)
                {
                    if (kv.Key == InspectorLabels.None) continue;
                    if (RightTypeIsCompatible(leftType, op, kv.Value))
                        names.Add(kv.Key);
                }

                string current = rightMember?.stringValue ?? string.Empty;
                int idx = Mathf.Max(0, names.IndexOf(current));
                memberPopup.choices = names;
                memberPopup.index = idx;

                if (idx == 0 && !string.IsNullOrEmpty(current))
                {
                    rightMember.stringValue = string.Empty;
                    rightMember.serializedObject.ApplyModifiedProperties();
                }

                memberPopup.SetEnabled(names.Count > 1);
            }

            void Hook(SerializedProperty sp) { if (sp != null) root.TrackPropertyValue(sp, _ => Refresh()); }

            // left
            Hook(left.FindPropertyRelative("target"));
            Hook(left.FindPropertyRelative("source"));
            Hook(left.FindPropertyRelative("componentTypeName"));
            Hook(left.FindPropertyRelative("member"));

            // right
            Hook(right.FindPropertyRelative("target"));
            Hook(right.FindPropertyRelative("source"));
            Hook(right.FindPropertyRelative("componentTypeName"));
            Hook(right.FindPropertyRelative("member"));

            Hook(opProp);
            Hook(rightDynamicProp);

            root.RegisterCallback<AttachToPanelEvent>(_ => Refresh());
            root.schedule.Execute(Refresh);
            EditorApplication.delayCall += Refresh;
            root.RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.delayCall -= Refresh);

            Refresh();
        }

        #endregion

        #endregion
    }
}
#endif