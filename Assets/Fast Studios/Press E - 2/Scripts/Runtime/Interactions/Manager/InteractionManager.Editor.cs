#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Reflection;
using UnityEditor;

namespace FastStudios
{
    public partial class InteractionManager
    {
        [MenuItem("CONTEXT/InteractionManager/Open Documentation", false)]
        public static void OpenDoc()
        {
            Application.OpenURL("https://fast-studios.gitbook.io/press-e");
        }

        [MenuItem("Tools/Fast Studios/Press E PRO/Open Documentation", priority = 99)]
        static void OpenDoc2()
        {
            OpenDoc();
        }

        [MenuItem("Tools/Fast Studios/Press E PRO/Support", priority = 100)]
        public static void ContactUs()
        {
            Application.OpenURL("https://docs.google.com/forms/d/e/1FAIpQLSe-2eQqGK8EcqvDHfrtLb738UE9rWYtRcWqtc3ePBL_0az_1Q/viewform?usp=sharing&ouid=101683304915259794740");
        }

        [MenuItem("Tools/Fast Studios/Press E PRO/Leave a Review :D (Helps a lot! ;D)", priority = 101)]
        public static void LeaveAReview()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/templates/systems/presse-pro-a-complete-interaction-system-for-unity-340168#reviews");
        }

        [MenuItem("Tools/Fast Studios/Press E PRO/Other Assets...", priority = 102)]
        public static void SeeOtherAssets()
        {
            Application.OpenURL("https://assetstore.unity.com/publishers/95938");
        }

        [MenuItem("Tools/Fast Studios/Press E PRO/Create Manager", priority = 1)]
        public static void CreateManager()
        {
            InteractionManager maybeManager = GameObject.FindAnyObjectByType<InteractionManager>();

            if (maybeManager != null)
            {
                Debug.LogWarning("There is already a manager in the scene");

                Selection.activeObject = maybeManager;
                EditorGUIUtility.PingObject(maybeManager);

                return;
            }

            if (SceneManager.GetActiveScene().buildIndex != 0 && EditorPrefs.GetBool("DontShowManagerAgain") == false)
            {
                int awnser = EditorUtility.DisplayDialogComplex(
                    "PressE Singleton Warning",
                    "The manager is a singleton dont destroy on load object, if you dont know what you are doing read the documentation to understand",

                    "I know what im doing",
                    "Open Documentation",
                    "Dont Show Me Again");

                if (awnser == 0)
                {
                    // OK
                }
                else if (awnser == 1)
                {
                    // Cancel
                    OpenDoc();
                    return;
                }
                else if (awnser == 2)
                {
                    // Alt
                    EditorPrefs.SetBool("DontShowManagerAgain", true);
                }
            }

            CreateManagerVoid(false);
        }

        [MenuItem("Tools/Fast Studios/Press E PRO/Create Temporary Manager", priority = 2)]
        public static void CreateTemporaryManager()
        {
            InteractionManager maybeManager = GameObject.FindAnyObjectByType<InteractionManager>();

            if (maybeManager != null)
            {
                Debug.LogWarning("There is already a manager in the scene");

                Selection.activeObject = maybeManager;
                EditorGUIUtility.PingObject(maybeManager);

                return;
            }

            CreateManagerVoid(true);
        }

        static void CreateManagerVoid(bool temporary)
        {
            GameObject prefab = new GameObject(temporary ? "Temporary PressE Manager" : "PressE Manager");
            prefab.AddComponent<InteractionManager>();
            if (temporary) prefab.AddComponent<TemporaryManager>();
            Canvas canvas = prefab.AddComponent<Canvas>();
            CanvasScaler canvasScaler = prefab.AddComponent<CanvasScaler>();
            GraphicRaycaster graphicRaycaster = prefab.AddComponent<GraphicRaycaster>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasScaler.referencePixelsPerUnit = 100;

            Selection.activeObject = prefab;

            prefab.transform.SetAsLastSibling();
        }

        void OnValidate()
        {
            CacheProjectSettings();

            if (playerObject == null) GetPlayerObject();
        }

        [Serializable]
        public struct FoldoutRecord { public string key; public bool open; }
        [SerializeField, HideInInspector] private List<FoldoutRecord> _foldouts = new();
        [NonSerialized] private Dictionary<string, bool> _foldMap;

        public bool GetFoldout(string key, bool @default = false)
        {
            if (_foldMap == null) RebuildFoldMap();
            if (_foldMap.TryGetValue(key, out var v)) return v;
            _foldMap[key] = @default;
            return @default;
        }

        public void SetFoldout(string key, bool open)
        {
            if (_foldMap == null) RebuildFoldMap();
            _foldMap[key] = open;

            int idx = _foldouts.FindIndex(r => r.key == key);
            var rec = new FoldoutRecord { key = key, open = open };
            if (idx >= 0) _foldouts[idx] = rec;
            else _foldouts.Add(rec);

            Undo.RecordObject(this, open ? "Expand Foldout" : "Collapse Foldout");
            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

        void RebuildFoldMap()
        {
            _foldMap = new Dictionary<string, bool>(_foldouts.Count);
            foreach (var r in _foldouts)
                if (!string.IsNullOrEmpty(r.key))
                    _foldMap[r.key] = r.open;
        }

        private void SetAllFoldoutsGeneric(bool open)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var f in GetType().GetFields(flags))
                if (f.FieldType == typeof(bool) && f.Name.EndsWith("Foldout"))
                    f.SetValue(this, open);

            if (_foldMap != null && _foldMap.Count > 0)
                foreach (var k in new List<string>(_foldMap.Keys))
                    SetFoldout(k, open);

            Undo.RecordObject(this, open ? "Expand All Foldouts" : "Collapse All Foldouts");
            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        public void OnBeforeSerialize()
        {
            if (_foldMap == null) return;
            _foldouts.Clear();
            foreach (var kv in _foldMap) _foldouts.Add(new FoldoutRecord { key = kv.Key, open = kv.Value });
        }
        public void OnAfterDeserialize() { }

    }
}
#endif