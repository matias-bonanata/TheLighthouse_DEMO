using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR

using FastStudios.EditorTools;
using System.Reflection;
using UnityEditor;

#endif

namespace FastStudios
{
    public class UIPrefab : MonoBehaviour
    {
        public bool hasNameText = false;
        public TMP_Text NameTmpText;

        public bool hasInteractionText = false;
        public TMP_Text InteractionTmpText;

        public bool hasBackground = false;
        public Image Background;

        public bool hasImage = false;
        public Image Image;

        public bool hasSlider = false;
        public Slider Slider;

        public bool hasInspectionDetails = false;
        public TMP_Text DetailsText;
        public Image DetailsImage;

        [HideInInspector] public int layer;
        [HideInInspector] public int lastSelectedTab;
        [HideInInspector] public Interactable interactedInteractable;
        [HideInInspector] public RectTransform rectTransform;

        void OnEnable()
        {
            if (rectTransform == null) CacheRectTransform();
            UniversalsRuntime.ApplyAllFor<UIPrefab>();
            UniversalsRuntime.ApplyLoadOnCreationForInstance(this);
        }

        public void ShowUI(Interactable interactable)
        {
            if (rectTransform == null) CacheRectTransform();
            interactedInteractable = interactable;

            if (hasNameText)
            {
                if (NameTmpText != null) NameTmpText.text = interactable.gameObject.name;
                else Debug.LogWarning("[Press E] UI Prefab trying to show a null name TMP Text");
            }
            if (hasInteractionText && InteractionTmpText != null) SetInteractionText(InteractionTmpText.text, interactedInteractable);

        }

        public void DeactivateAllChildren()
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }

        void CacheRectTransform()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        [HideInInspector] public bool CloseUI;
        [HideInInspector] public bool CloseUIWithoutReset;
        public void OpenInspectionUI(Interactable interactable)
        {
            interactedInteractable = interactable;

            if (interactable.hasDetailImage && DetailsImage != null && interactable.DetailImage != null && DetailsImage.sprite != interactable.DetailImage)
            {
                DetailsImage.color = interactable.DetailImageColor;
                DetailsImage.sprite = interactable.DetailImage;
            }

            if (interactable.hasDetailText && hasInspectionDetails && DetailsText != null && !String.IsNullOrEmpty(interactable.DetailText) && DetailsText.text != interactable.DetailText)
            {
                DetailsText.text = interactable.DetailText;
            }
            else if (interactable.hasDetailText && hasInspectionDetails && DetailsText == null && !String.IsNullOrEmpty(interactable.DetailText) && DetailsText.text != interactable.DetailText)
            {
                Debug.LogWarning("[Press E] UI Prefab trying to show a null detail text");
            }

            InteractionManager singleton = InteractionManager.singleton;

            PressEInputBind imageBind = InputHandler.ResolveBind(singleton.InspectionDetailsImage, interactable.OverrideDetailImageKey, interactable.NewDetailImage);
            PressEInputBind textBind = InputHandler.ResolveBind(singleton.InspectionDetailsText, interactable.OverrideDetailTextKey, interactable.NewDetailText);

            bool GetImageKey = InputHandler.GeneralInputDown(imageBind);
            bool GetTextKey = InputHandler.GeneralInputDown(textBind);

            GetImageKey |= imageBind.UIButtonDown;
            GetTextKey |= textBind.UIButtonDown;

            bool GetTextAndKey = GetTextKey && interactable.hasDetailText;
            bool GetImageAndKey = GetImageKey && interactable.hasDetailImage;

            if (interactable.hasDetailBackground && Background != null && hasBackground && (GetImageAndKey || GetTextAndKey))
            {
                CloseUIWithoutReset = false;
                if (interactable.DetailBackground != null) Background.sprite = interactable.DetailBackground;
                Background.color = interactable.DetailBackgroundColor;
                Background.gameObject.SetActive(true);
            }
            else if (interactable.hasDetailBackground && Background == null && hasBackground && (GetImageAndKey || GetTextAndKey))
            {
                Debug.LogWarning("[Press E] UI Prefab trying to show a null background image");
            }

            if (interactable.hasDetailImage)
            {
                singleton.InspectionDetailsImage.CanShow = true;
            }

            if (interactable.hasDetailText)
            {
                singleton.InspectionDetailsText.CanShow = true;
            }

            if (interactable.hasDetailImage && interactable.hasDetailText && ((interactable.DetailTextFirst && GetTextKey) || (interactable.DetailImageFirst && GetImageKey)))
            {
                if (interactable.DetailTextFirst)
                {
                    Toggle(DetailsText.gameObject, DetailsImage.gameObject);
                }
                else if (interactable.DetailImageFirst)
                {
                    Toggle(DetailsImage.gameObject, DetailsText.gameObject);
                }
                else if (interactable.DetailTextFirst == false && interactable.DetailImageFirst == false)
                {
                    if (!DetailsImage.gameObject.activeInHierarchy || !DetailsText.gameObject.activeInHierarchy)
                    {
                        DetailsImage.gameObject.SetActive(true);
                        DetailsText.gameObject.SetActive(true);
                    }
                    else
                    {
                        CloseInspectionUI(interactable);
                    }
                }
            }
            else
            {
                if (GetTextAndKey && GetImageAndKey)
                {
                    bool active = DetailsText.gameObject.activeInHierarchy && DetailsImage.gameObject.activeInHierarchy;
                    if (!active)
                    {
                        DetailsText.gameObject.SetActive(true);
                        DetailsImage.gameObject.SetActive(true);
                    }
                    else CloseInspectionUI(interactable);
                }
                else if (GetTextAndKey)
                {
                    DetailsImage.gameObject.SetActive(false);

                    if (!DetailsText.gameObject.activeInHierarchy) DetailsText.gameObject.SetActive(true);
                    else CloseInspectionUI(interactable);
                }
                else if (GetImageAndKey)
                {
                    DetailsText.gameObject.SetActive(false);

                    if (!DetailsImage.gameObject.activeInHierarchy) DetailsImage.gameObject.SetActive(true);
                    else CloseInspectionUI(interactable);
                }
            }

            void Toggle(GameObject FirstObject, GameObject SecondObject)
            {
                if (FirstObject.activeInHierarchy == false && SecondObject.activeInHierarchy == false)
                {
                    FirstObject.SetActive(true);
                    SecondObject.SetActive(false);
                }
                else if (FirstObject.activeInHierarchy == true && SecondObject.activeInHierarchy == false)
                {
                    FirstObject.SetActive(false);
                    SecondObject.SetActive(true);
                }
                else if (FirstObject.activeInHierarchy == false && SecondObject.activeInHierarchy == true)
                {
                    CloseInspectionUI(interactable);
                }
            }
        }

        public void CloseInspectionUI(Interactable interactable)
        {
            interactedInteractable = null;

            if (Background != null)
            {
                Background.gameObject.SetActive(false);
            }

            if (DetailsText != null)
            {
                DetailsText.gameObject.SetActive(false);
            }

            if (DetailsImage != null)
            {
                DetailsImage.gameObject.SetActive(false);
            }

            CloseUI = true;
            CloseUIWithoutReset = true;
            Invoke(nameof(ResetCloseUI), 0.5f);
        }

        void ResetCloseUI()
        {
            CloseUI = false;
        }

        private string _lastTemplate;
        private string _lastResult;
        private Interactable _lastInteractable;
        private string _lastInteractionKey;
        private string _lastThrowableKey;
        private string _interactionTemplateCache;
        private int _lastToggleIndex = int.MinValue;
        public void SetInteractionText(string text, Interactable interactable = null)
        {
            InteractionManager singleton = InteractionManager.singleton;

            string template = text;

            if (!string.IsNullOrEmpty(text))
            {
                if (!string.Equals(text, _lastResult, StringComparison.Ordinal) || string.IsNullOrEmpty(_interactionTemplateCache))
                    _interactionTemplateCache = text;
                else
                    template = _interactionTemplateCache;
            }

            int toggleIndex = 0;
            if (interactable != null) toggleIndex = interactable.interactionTimes & 1;

            string interactionKey = "Error";
            string throwableKey = "Error";

            if (singleton == null)
            {
                Debug.LogWarning("[PressE] Trying to get Interaction Manager, but it's null. Did you add a Interaction Manager to the scene?");
            }
            else
            {
                if (interactable == null) interactionKey = InputHandler.GetKeyOrMouse(singleton.Interaction);
                else
                    interactionKey = InputHandler.GetKeyOrMouse(singleton.Interaction, interactable.OverrideInteractionKey, interactable.NewInteraction);

                throwableKey = InputHandler.GetKeyOrMouse(singleton.GrabThrow);
            }

            if (_lastTemplate == template &&
                _lastInteractable == interactable &&
                _lastInteractionKey == interactionKey &&
                _lastThrowableKey == throwableKey &&
                _lastToggleIndex == toggleIndex)
            {
                if (InteractionTmpText.text != _lastResult)
                    InteractionTmpText.text = _lastResult;
                return;
            }

            _lastTemplate = template;
            _lastInteractable = interactable;
            _lastInteractionKey = interactionKey;
            _lastThrowableKey = throwableKey;
            _lastToggleIndex = toggleIndex;

            string realDisplayText = template;

            if (template.Contains("{key}") || template.Contains("{interactionKey}"))
            {
                string replaceInteractionKey = template.Contains("{key}") ? "{key}" : "{interactionKey}";
                realDisplayText = realDisplayText.Replace(replaceInteractionKey, interactionKey);
            }

            if (template.Contains("{throwableKey}"))
                realDisplayText = realDisplayText.Replace("{throwableKey}", throwableKey);

            if (realDisplayText.IndexOf("toggle", StringComparison.OrdinalIgnoreCase) >= 0 && realDisplayText.IndexOf('{') >= 0)
                realDisplayText = ResolveToggleTokens(realDisplayText, toggleIndex);

            _lastResult = realDisplayText;
            InteractionTmpText.text = realDisplayText;
        }

        private string ResolveToggleTokens(string text, int toggleIndex)
        {
            if (string.IsNullOrEmpty(text)) return text;

            int searchIndex = 0;
            System.Text.StringBuilder sb = null;

            while (true)
            {
                int open = text.IndexOf('{', searchIndex);
                if (open < 0) break;

                int close = text.IndexOf('}', open + 1);
                if (close < 0) break;

                if (TryGetToggleReplacement(text, open, close, toggleIndex, out string replacement))
                {
                    sb ??= new System.Text.StringBuilder(text.Length);
                    sb.Append(text, searchIndex, open - searchIndex);
                    sb.Append(replacement);
                    searchIndex = close + 1;
                }
                else
                {
                    searchIndex = open + 1;
                }
            }

            if (sb == null) return text;

            sb.Append(text, searchIndex, text.Length - searchIndex);
            return sb.ToString();
        }

        private bool TryGetToggleReplacement(string full, int openBraceIndex, int closeBraceIndex, int toggleIndex, out string replacement)
        {
            replacement = null;

            int i = openBraceIndex + 1;
            while (i < closeBraceIndex && char.IsWhiteSpace(full[i])) i++;

            const string token = "toggle";
            if (i + token.Length >= closeBraceIndex) return false;

            for (int k = 0; k < token.Length; k++)
            {
                if (char.ToLowerInvariant(full[i + k]) != token[k])
                    return false;
            }
            i += token.Length;

            while (i < closeBraceIndex && char.IsWhiteSpace(full[i])) i++;
            if (i >= closeBraceIndex || full[i] != '?') return false;

            int q = i;

            int colon = full.IndexOf(':', q + 1);
            if (colon < 0 || colon >= closeBraceIndex) return false;

            string rawA = full.Substring(q + 1, colon - (q + 1));
            string rawB = full.Substring(colon + 1, closeBraceIndex - (colon + 1));

            string optA = NormalizeToggleOption(rawA);
            string optB = NormalizeToggleOption(rawB);

            replacement = (toggleIndex == 0) ? optA : optB;
            return true;
        }

        private string NormalizeToggleOption(string raw)
        {
            if (raw == null) return string.Empty;

            string s = raw;

            bool keepLead = HasLeadingSpaceMarker(s);
            bool keepTrail = HasTrailingSpaceMarker(s);

            if (keepLead)
            {
                int i = 0;
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
                i += 2; // "\s"
                s = (i <= s.Length) ? s.Substring(i) : string.Empty;
            }
            else
            {
                s = s.TrimStart();
            }

            if (keepTrail)
            {
                int r = s.Length;
                while (r > 0 && char.IsWhiteSpace(s[r - 1])) r--;

                int markerStart = r - 2;
                string trailing = s.Substring(r);

                string before = markerStart > 0 ? s.Substring(0, markerStart).TrimEnd() : string.Empty;
                s = before + trailing;
            }
            else
            {
                s = s.TrimEnd();
            }

            return s;
        }

        private bool HasLeadingSpaceMarker(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;

            int i = 0;
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            return i + 1 < s.Length && s[i] == '\\' && s[i + 1] == 's';
        }

        private bool HasTrailingSpaceMarker(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;

            int r = s.Length;
            while (r > 0 && char.IsWhiteSpace(s[r - 1])) r--;
            return r >= 2 && s[r - 2] == '\\' && s[r - 1] == 's';
        }

#if UNITY_EDITOR

        [MenuItem("CONTEXT/UIPrefab/Open Documentation", false)]
        static void OpenDoc()
        {
            InteractionManager.OpenDoc();
        }

        void Reset()
        {
            InteractableUniversalsSO.ApplyLoadOnCreation(this);
        }

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

        #region Universals

        [SerializeField, HideInInspector] private List<string> _boundUniversals = new List<string>();
        [SerializeField, HideInInspector] private List<UniversalBackup> _universalBackups = new List<UniversalBackup>();

        public bool IsUniversalBound(string path) => _boundUniversals != null && _boundUniversals.Contains(path);
        public void BindUniversal(string path)
        {
            if (_boundUniversals == null) _boundUniversals = new List<string>();
            if (!_boundUniversals.Contains(path)) _boundUniversals.Add(path);

            if (Application.isPlaying) EditorTools.InteractableUniversalsSO.ApplySingle(this, path);

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

        public void UnbindUniversal(string path)
        {
            if (_boundUniversals == null) return;
            _boundUniversals.Remove(path);
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

        [HideInInspector]
        public void __Editor_SaveUniversalBackup(SerializedObject so, string path)
        {
            if (_universalBackups == null) _universalBackups = new List<UniversalBackup>();
            if (_universalBackups.Exists(b => b.path == path)) return;

            var sp = so.FindProperty(path);
            if (sp == null) return;

            if (!InteractableUniversalsSO.TryMapType(sp.propertyType, out var vt)) return;

            var b = new UniversalBackup { path = path, vt = vt };

            switch (vt)
            {
                case UniversalValueType.Bool: b.b = sp.boolValue; break;
                case UniversalValueType.Int: b.i = sp.intValue; break;
                case UniversalValueType.Float: b.f = sp.floatValue; break;
                case UniversalValueType.String: b.s = sp.stringValue; break;
                case UniversalValueType.Color: b.col = sp.colorValue; break;
                case UniversalValueType.Vector2: b.v2 = sp.vector2Value; break;
                case UniversalValueType.Vector3: b.v3 = sp.vector3Value; break;
                case UniversalValueType.Vector2Int: b.v2i = sp.vector2IntValue; break;
                case UniversalValueType.Vector3Int: b.v3i = sp.vector3IntValue; break;
                case UniversalValueType.Enum: b.enumIndex = sp.enumValueIndex; break;
                case UniversalValueType.AnimationCurve: b.curve = sp.animationCurveValue; break;
                case UniversalValueType.Quaternion: b.quat = sp.quaternionValue; break;
                case UniversalValueType.LayerMask: b.layerMask = sp.intValue; break;
            }

            _universalBackups.Add(b);
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

        public bool __Editor_RestoreUniversalBackup(SerializedObject so, string path)
        {
            if (_universalBackups == null || _universalBackups.Count == 0) return false;

            int idx = _universalBackups.FindIndex(x => x.path == path);
            if (idx < 0) return false;

            so.UpdateIfRequiredOrScript();

            var b = _universalBackups[idx];
            var sp = so.FindProperty(path);
            if (sp == null) return false;

            switch (b.vt)
            {
                case UniversalValueType.Bool: sp.boolValue = b.b; break;
                case UniversalValueType.Int: sp.intValue = b.i; break;
                case UniversalValueType.Float: sp.floatValue = b.f; break;
                case UniversalValueType.String: sp.stringValue = b.s; break;
                case UniversalValueType.Color: sp.colorValue = b.col; break;
                case UniversalValueType.Vector2: sp.vector2Value = b.v2; break;
                case UniversalValueType.Vector3: sp.vector3Value = b.v3; break;
                case UniversalValueType.Vector2Int: sp.vector2IntValue = b.v2i; break;
                case UniversalValueType.Vector3Int: sp.vector3IntValue = b.v3i; break;
                case UniversalValueType.Enum: sp.enumValueIndex = b.enumIndex; break;
                case UniversalValueType.AnimationCurve: sp.animationCurveValue = b.curve; break;
                case UniversalValueType.Quaternion: sp.quaternionValue = b.quat; break;
                case UniversalValueType.LayerMask: sp.intValue = b.layerMask; break;
            }

            so.ApplyModifiedProperties();

            _universalBackups.RemoveAt(idx);
            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            return true;
        }

        [Serializable]
        public class UniversalBackup
        {
            public string path;
            public UniversalValueType vt;
            public bool b; public int i; public float f; public string s;
            public Color col = Color.white;
            public Vector2 v2; public Vector3 v3;
            public Vector2Int v2i; public Vector3Int v3i;
            public int enumIndex;
            public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
            public Quaternion quat = Quaternion.identity;
            public int layerMask;
        }


        #endregion
#endif
    }
}
