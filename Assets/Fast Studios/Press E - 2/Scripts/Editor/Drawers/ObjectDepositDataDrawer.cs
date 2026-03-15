#if UNITY_EDITOR
using System.Collections.Generic;
using FastStudios.EditorTools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using Action = System.Action;

namespace FastStudios
{
    [CustomPropertyDrawer(typeof(ObjectDepositData))]
    public class ObjectDepositDataDrawer : PropertyDrawer
    {
        public VisualTreeAsset UXML;
        public StyleSheet USS;

        const string NotSelectedName = "NotSelected";
        const string SelectedName = "Selected";
        const string PreviewName = "PreviewImage";

        SerializedProperty _prop;
        public bool IsOnTransformSection = true;

        public override VisualElement CreatePropertyGUI(SerializedProperty prop)
        {
            var root = new VisualElement();
            _prop = prop;

            if (UXML != null) UXML.CloneTree(root);
            if (USS != null) root.styleSheets.Add(USS);

            root.Bind(prop.serializedObject);

            root.style.unityFontDefinition = new StyleFontDefinition(FSEditorUI.mainFont);

            var goProp = prop.FindPropertyRelative("gameObject");

            bool fixing = false;
            GameObject lastValid = goProp.objectReferenceValue as GameObject;

            void ValidateAssignedGO()
            {
                if (fixing) return;

                var go = goProp.objectReferenceValue as GameObject;

                if (go == null)
                {
                    lastValid = null;
                    return;
                }

                if (!GrabDeposit.TryGetValidGrabInteractable(go, out _))
                {
                    fixing = true;
                    goProp.objectReferenceValue = lastValid;
                    goProp.serializedObject.ApplyModifiedProperties();
                    fixing = false;

                    UnityEditor.EditorWindow.focusedWindow?.ShowNotification(
                        new GUIContent("GrabDeposit only accepts Interactable objects in Grab mode.")
                    );
                    return;
                }

                lastValid = go;
            }

            var notSelected = root.Q<VisualElement>(NotSelectedName);
            var selected = root.Q<VisualElement>(SelectedName);
            var previewVE = root.Q<VisualElement>(PreviewName);
            var removeButton = root.Q<VisualElement>("RemoveObjectButton");
            var settingsButton = root.Q<VisualElement>("SettingsButton");
            var TransformSection = root.Q<VisualElement>("TransformSection");
            var IndividualSetting = root.Q<VisualElement>("IndividualSetting");
            var HasStackLimit = root.Q<VisualElement>("HasStackLimit");
            var HasVisualStackLimit = root.Q<VisualElement>("HasVisualStackLimit");
            var ActualStackLimit = root.Q<VisualElement>("ActualStackLimit");
            var VisualStackLimit = root.Q<VisualElement>("VisualStackLimit");
            var DistanceBetween = root.Q<VisualElement>("DistanceBetween");

            var canStackProp = prop.FindPropertyRelative(nameof(ObjectDepositData.CanStack));
            var hasStackLimitProp = prop.FindPropertyRelative(nameof(ObjectDepositData.HasStackLimit));
            var hasVisualStackLimitProp = prop.FindPropertyRelative(nameof(ObjectDepositData.HasVisualStackLimit));
            FSEditorUI.ShowIfBool(root, canStackProp, HasStackLimit, HasVisualStackLimit, ActualStackLimit, VisualStackLimit, DistanceBetween);
            FSEditorUI.ShowIfBool(root, hasStackLimitProp, "HideOpacity", ActualStackLimit);
            FSEditorUI.ShowIfBool(root, hasVisualStackLimitProp, "HideOpacity", VisualStackLimit);

            IndividualSetting.AddToClassList("Hide");

            if (removeButton != null)
            {
                removeButton.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
                removeButton.RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
            }

            if (previewVE != null && goProp != null)
            {
                previewVE.AddManipulator(new Clickable(() =>
                {
                    OpenGameObjectPicker(goProp, Refresh, allowSceneObjects: true);
                }));
            }

            if (previewVE != null && goProp != null)
            {
                previewVE.RegisterCallback<DragUpdatedEvent>(e =>
                {
                    var refs = DragAndDrop.objectReferences;
                    if (refs != null && refs.Length > 0 && refs[0] is GameObject)
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    else
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

                    e.StopPropagation();
                });

                previewVE.RegisterCallback<DragPerformEvent>(e =>
                {
                    var refs = DragAndDrop.objectReferences;
                    if (refs != null && refs.Length > 0 && refs[0] is GameObject go)
                    {
                        DragAndDrop.AcceptDrag();
                        goProp.objectReferenceValue = go;
                        goProp.serializedObject.ApplyModifiedProperties();
                        Refresh();
                    }

                    e.StopPropagation();
                });
            }

            removeButton.AddManipulator(new Clickable(() =>
            {
                goProp.objectReferenceValue = null;
                goProp.serializedObject.ApplyModifiedProperties();
            }));

            var setButton = root.Q<Button>("Set");
            setButton.clicked += () => CreateOrMoveAdjustment(prop);

            int previewRequestId = 0;

            void SetPreview(Texture2D tex)
            {
                if (previewVE == null) return;

                if (tex == null)
                {
                    previewVE.style.backgroundImage = StyleKeyword.None;
                    return;
                }

                previewVE.style.backgroundImage = new StyleBackground(tex);
            }

            void RequestPreview(GameObject go)
            {
                if (previewVE == null) return;

                if (go == null)
                {
                    SetPreview(null);
                    return;
                }

                var thumb = AssetPreview.GetMiniThumbnail(go);
                if (thumb != null) SetPreview(thumb);

                var custom = GetCustomPreview(go);
                if (custom != null) SetPreview(custom);
            }

            void Refresh()
            {
                prop.serializedObject.UpdateIfRequiredOrScript();

                var go = goProp != null ? goProp.objectReferenceValue as GameObject : null;
                bool hasGO = go != null;

                if (notSelected != null) FSEditorUI.SetVisible(!hasGO, FSEditorUI.HiddenClass, notSelected);
                if (selected != null) FSEditorUI.SetVisible(hasGO, FSEditorUI.HiddenClass, selected);

                if (hasGO) RequestPreview(go);
                else SetPreview(null);
            }

            if (goProp != null) root.TrackPropertyValue(goProp, _ =>
            {
                ValidateAssignedGO();
                Refresh();
            });

            root.RegisterCallback<DetachFromPanelEvent>(_ => previewRequestId++);

            BindBtn(settingsButton, () =>
            {
                if (IsOnTransformSection == false)
                {
                    TransformSection.RemoveFromClassList("Hide");
                    IndividualSetting.AddToClassList("Hide");
                    settingsButton.RemoveFromClassList("toggled");
                    IsOnTransformSection = true;
                }
                else
                {
                    TransformSection.AddToClassList("Hide");
                    IndividualSetting.RemoveFromClassList("Hide");
                    settingsButton.AddToClassList("toggled");
                    IsOnTransformSection = false;
                }
            });

            Refresh();
            return root;
        }

        static readonly Dictionary<int, ItemPositionAdjustment> s_HelperPerDeposit = new();

        static void CreateOrMoveAdjustment(SerializedProperty prop)
        {
            if (prop == null) return;

            var deposit = prop.serializedObject.targetObject as GrabDeposit;
            if (deposit == null) return;

            if (!TryGetListIndex(prop.propertyPath, out int index)) return;

            if (deposit.SpecificObjects == null) return;
            if (index < 0 || index >= deposit.SpecificObjects.Count) return;

            var data = deposit.SpecificObjects[index];
            if (data == null) return;

            var box = deposit.GetComponent<BoxCollider>();
            if (!box) return;

            Vector3 originWorld = deposit.transform.TransformPoint(box.center);
            Quaternion baseRot = deposit.transform.rotation;

            Vector3 worldPos = originWorld + (baseRot * data.Position);
            Quaternion worldRot = baseRot * Quaternion.Euler(data.Rotation);
            Vector3 worldScale = data.Scale;

            int depId = deposit.GetInstanceID();
            if (!s_HelperPerDeposit.TryGetValue(depId, out var helper) || helper == null)
            {
                var go = new GameObject("Item Position Adjustment");
                Undo.RegisterCreatedObjectUndo(go, "Create Item Position Adjustment");

                go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

                helper = go.AddComponent<ItemPositionAdjustment>();
                s_HelperPerDeposit[depId] = helper;

                EditorSceneManager.MoveGameObjectToScene(go, deposit.gameObject.scene);
            }

            helper.Bind(deposit, index);

            Undo.RecordObject(helper.transform, "Move Item Position Adjustment");
            helper.transform.SetPositionAndRotation(worldPos, worldRot);
            helper.transform.localScale = worldScale;
            helper.transform.hasChanged = false;

            Selection.activeObject = helper.gameObject;
            EditorGUIUtility.PingObject(helper.gameObject);
        }

        static bool TryGetListIndex(string propertyPath, out int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(propertyPath)) return false;

            int lb = propertyPath.LastIndexOf('[');
            int rb = propertyPath.LastIndexOf(']');
            if (lb < 0 || rb < 0 || rb <= lb) return false;

            string number = propertyPath.Substring(lb + 1, rb - lb - 1);
            return int.TryParse(number, out index);
        }

        static int s_nextPickerId = 200000;

        struct PickerRequest
        {
            public int controlId;
            public Object targetObject;
            public string goPropertyPath;
            public System.Action onChanged;
        }

        static PickerRequest s_picker;
        static bool s_pickerHooked;

        static void OpenGameObjectPicker(SerializedProperty goProp, System.Action onChanged, bool allowSceneObjects = false)
        {
            if (goProp == null) return;

            s_picker = new PickerRequest
            {
                controlId = ++s_nextPickerId,
                targetObject = goProp.serializedObject.targetObject,
                goPropertyPath = goProp.propertyPath,
                onChanged = onChanged
            };

            var current = goProp.objectReferenceValue as GameObject;
            EditorGUIUtility.ShowObjectPicker<GameObject>(current, allowSceneObjects, "", s_picker.controlId);

            if (!s_pickerHooked)
            {
                EditorApplication.update += PollObjectPicker;
                s_pickerHooked = true;
            }
        }

        static void PollObjectPicker()
        {
            if (s_picker.targetObject == null)
            {
                CleanupPicker();
                return;
            }

            int activeId = EditorGUIUtility.GetObjectPickerControlID();

            if (activeId == 0)
            {
                CleanupPicker();
                return;
            }

            if (activeId != s_picker.controlId)
                return;

            var picked = EditorGUIUtility.GetObjectPickerObject() as GameObject;

            var so = new SerializedObject(s_picker.targetObject);
            var p = so.FindProperty(s_picker.goPropertyPath);
            if (p == null)
                return;

            if (p.objectReferenceValue != picked)
            {
                p.objectReferenceValue = picked;
                so.ApplyModifiedProperties();

                s_picker.onChanged?.Invoke();
            }
        }

        static void CleanupPicker()
        {
            if (s_pickerHooked)
            {
                EditorApplication.update -= PollObjectPicker;
                s_pickerHooked = false;
            }

            s_picker = default;
        }

        static readonly Color kPreviewBG = new Color(0.3843137254901961f, 0.27058823529411763f, 0.47058823529411764f, 1f);
        const int kPreviewSize = 64;

        static readonly Dictionary<int, Texture2D> s_PreviewCache = new();

        static Texture2D GetCustomPreview(GameObject go)
        {
            if (go == null) return null;

            int key = go.GetInstanceID();
            if (s_PreviewCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            Texture2D tex = RenderPreview(go, kPreviewSize, kPreviewBG);

            if (tex == null)
                tex = AssetPreview.GetMiniThumbnail(go);

            s_PreviewCache[key] = tex;
            return tex;
        }

        static Texture2D RenderPreview(GameObject go, int size, Color bg)
        {
            var preview = new PreviewRenderUtility();
            preview.camera.clearFlags = CameraClearFlags.Color;
            preview.camera.backgroundColor = bg;
            preview.camera.nearClipPlane = 0.01f;

            Light light1 = preview.lights[0];
            light1.intensity = 2f;
            light1.transform.rotation = Quaternion.Euler(35f, 0, 0f);

            Light light2 = preview.lights[1];
            light2.intensity = 1.2f;
            light2.transform.rotation = Quaternion.Euler(-35f, 270, 0f);

            GameObject inst = null;

            try
            {
                inst = PrefabUtility.InstantiatePrefab(go) as GameObject;
                if (inst == null) inst = Object.Instantiate(go);

                inst.hideFlags = HideFlags.HideAndDontSave;
                inst.transform.position = Vector3.zero;
                inst.transform.rotation = Quaternion.identity;

                preview.AddSingleGO(inst);

                Bounds b = new Bounds(inst.transform.position, Vector3.one * 0.1f);
                var renderers = inst.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                        b.Encapsulate(renderers[i].bounds);
                }

                preview.camera.transform.rotation = Quaternion.Euler(20f, -30f, 0f);
                float radius = b.extents.magnitude;
                float dist = Mathf.Max(0.5f, radius / Mathf.Sin(preview.camera.fieldOfView * Mathf.Deg2Rad * 0.5f));
                preview.camera.transform.position = b.center - preview.camera.transform.forward * dist;
                preview.camera.farClipPlane = dist * 4f;

                var r = new Rect(0, 0, size, size);

                preview.BeginPreview(r, GUIStyle.none);
                preview.Render(true);

                var rt = preview.EndPreview() as RenderTexture;
                if (rt == null) return null;

                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(r, 0, 0);
                tex.Apply(false, false);
                RenderTexture.active = prev;

                return tex;
            }
            finally
            {
                if (inst != null) Object.DestroyImmediate(inst);
                preview.Cleanup();
            }
        }

        void BindBtn(VisualElement ve, Action action)
        {
            if (ve == null) return;
            ve.pickingMode = PickingMode.Position;
            ve.focusable = true;
            ve.AddManipulator(new Clickable(ToggleMode));

            void ToggleMode()
            {
                action.Invoke();
            }
        }

    }
}
#endif
