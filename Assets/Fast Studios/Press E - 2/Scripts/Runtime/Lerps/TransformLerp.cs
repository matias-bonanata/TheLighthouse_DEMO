using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using FastStudios.EditorTools;
using System.Reflection;
using UnityEditor;
#endif

namespace FastStudios
{
    public class TransformLerp : PressELerps
    {
        public Transform Destination;

        public bool changePosition = true;
        public bool changeRotation = true;
        public bool changeScale = true;

        public bool overridePositionCurve;
        public AnimationCurve PosCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public bool overrideRotationCurve;
        public AnimationCurve RotCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);        
        public bool overrideScaleCurve;
        public AnimationCurve ScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);        

        bool _isRunning;
        bool _originalCaptured;
        bool _toggleState = true;
        Vector3 _origPosWorld;
        Quaternion _origRotWorld;
        Vector3 _origScaleWorld;

        public override void Interact()
        {
            if (!Destination) return;

            Transform tr = (AffectOtherObject && ObjectToMove != null) ? ObjectToMove : transform;

            if (!_originalCaptured)
            {
                _origPosWorld = tr.position;
                _origRotWorld = tr.rotation;
                _origScaleWorld = tr.lossyScale;
                _originalCaptured = true;
            }


            if (WaitTillAnimationFinished && _isRunning) return;
            if (!WaitTillAnimationFinished && _isRunning)
            {
                StopAllCoroutines();
                _isRunning = false;
            }

            if (ToggleWithOriginalState) _toggleState = !_toggleState;

            Vector3 posTargetWorld = tr.position;
            Quaternion rotTargetWorld = tr.rotation;
            Vector3 scaleTargetWorld = tr.lossyScale;

            if (!_toggleState)
            {
                if (changePosition) posTargetWorld = Destination.position;
                if (changeRotation) rotTargetWorld = Destination.rotation;
                if (changeScale) scaleTargetWorld = Destination.lossyScale;
            }
            else
            {
                if (changePosition) posTargetWorld = _origPosWorld;
                if (changeRotation) rotTargetWorld = _origRotWorld;
                if (changeScale) scaleTargetWorld = _origScaleWorld;
            }

            OnStart?.Invoke();
            StartCoroutine(BlendAllWorld(tr, posTargetWorld, rotTargetWorld, scaleTargetWorld));
        }

        IEnumerator BlendAllWorld(Transform tr, Vector3 posWorld, Quaternion rotWorld, Vector3 scaleWorld)
        {
            _isRunning = true;

            float dur = Mathf.Max(0.0001f, Duration);
            float t = 0f;

            Vector3 startPosWorld = tr.position;
            Quaternion startRotWorld = tr.rotation;
            Vector3 startScaleWorld = tr.lossyScale;

            Vector3 startLocalScale = tr.localScale;
            Vector3 targetLocalScale = ToLocalScale(tr, scaleWorld);

            while (t < dur)
            {
                float k = (animCurve != null) ? animCurve.Evaluate(t / dur) : (t / dur);
                float kPos = k;
                float kRot = k;
                float kScal = k;

                if (changePosition && overridePositionCurve) kPos = (PosCurve != null) ? PosCurve.Evaluate(t / dur) : (t / dur);
                if (changeRotation && overrideRotationCurve) kRot = (RotCurve != null) ? RotCurve.Evaluate(t / dur) : (t / dur);
                if (changeScale && overrideScaleCurve) kScal = (ScaleCurve != null) ? ScaleCurve.Evaluate(t / dur) : (t / dur);

                if (changePosition) tr.position = Vector3.Slerp(startPosWorld, posWorld, kPos);
                if (changeRotation) tr.rotation = Quaternion.Slerp(startRotWorld, rotWorld, kRot);
                if (changeScale) tr.localScale = Vector3.Slerp(startLocalScale, targetLocalScale, kScal);

                t += Time.deltaTime;
                yield return null;
            }

            if (changePosition) tr.position = posWorld;
            if (changeRotation) tr.rotation = rotWorld;
            if (changeScale) tr.localScale = targetLocalScale;

            _isRunning = false;
            OnComplete?.Invoke();
        }

        void OnEnable()
        {
            UniversalsRuntime.ApplyAllFor<TransformLerp>();
            UniversalsRuntime.ApplyLoadOnCreationForInstance(this);
        }

        void OnValidate()
        {
            if (LerpType == LerpType.Add) LerpType = LerpType.Set;
        }

#if UNITY_EDITOR

        protected override void DrawGizmoPreview(Transform tr)
        {
            if (!Destination) return;

            Vector3 targetWorldPos = changePosition ? Destination.position : tr.position;
            Quaternion targetWorldRot = changeRotation ? Destination.rotation : tr.rotation;
            Vector3 targetWorldScale = changeScale ? Destination.lossyScale : tr.lossyScale;

            float u = __Editor_GetTimelineU();
            float baseK = __Editor_EvalBaseCurve(u);

            float kPos = baseK;
            float kRot = baseK;
            float kScal = baseK;

            if (changePosition && overridePositionCurve)
                kPos = (PosCurve != null) ? PosCurve.Evaluate(u) : u;

            if (changeRotation && overrideRotationCurve)
                kRot = (RotCurve != null) ? RotCurve.Evaluate(u) : u;

            if (changeScale && overrideScaleCurve)
                kScal = (ScaleCurve != null) ? ScaleCurve.Evaluate(u) : u;

            Vector3 startPosWorld = tr.position;
            Quaternion startRotWorld = tr.rotation;

            Vector3 startLocalScale = tr.localScale;
            Vector3 targetLocalScale = ToLocalScale(tr, targetWorldScale);

            Vector3 previewWorldPos = changePosition ? Vector3.Slerp(startPosWorld, targetWorldPos, kPos) : startPosWorld;
            Quaternion previewWorldRot = changeRotation ? Quaternion.Slerp(startRotWorld, targetWorldRot, kRot) : startRotWorld;

            Vector3 previewLocalScale = changeScale ? Vector3.Slerp(startLocalScale, targetLocalScale, kScal) : startLocalScale;
            Vector3 previewWorldScale = changeScale ? ScaleLocalToWorld(tr, previewLocalScale) : tr.lossyScale;

            DrawPreviewModel(tr, previewWorldPos, previewWorldRot, previewWorldScale);

            if (changePosition)
                DrawPreviewDottedLine(tr.position, targetWorldPos);

        }


        [MenuItem("CONTEXT/TransformLerp/Open Documentation", false)]
        static void OpenDoc()
        {
            InteractionManager.OpenDoc();
        }

        void Reset()
        {
            InteractableUniversalsSO.ApplyLoadOnCreation(this);
        }

        [HideInInspector] public int lastSelectedTab;

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
