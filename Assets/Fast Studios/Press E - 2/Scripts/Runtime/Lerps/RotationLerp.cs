using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using FastStudios.EditorTools;
using System.Reflection;
using UnityEditor;
#endif

namespace FastStudios
{
    public class RotationLerp : PressELerps
    {
        public Quaternion NewRot;
        public bool useEuler;
        public Vector3 EulerAngles;

        public override void Interact() { if (useEuler) InteractRotation(EulerAngles); else InteractRotation(NewRot); }

        void OnEnable()
        {
            UniversalsRuntime.ApplyAllFor<RotationLerp>();
            UniversalsRuntime.ApplyLoadOnCreationForInstance(this);
        }

#if UNITY_EDITOR
        protected override void DrawGizmoPreview(Transform tr)
        {
            Vector3 anchorLocal = ResolveAnchorLocal(tr);
            Vector3 anchorWorldStart = tr.TransformPoint(anchorLocal);

            Quaternion targetBindingRot;
            Vector3 targetEulerBinding = Vector3.zero;

            if (useEuler)
            {
                Vector3 configured = EulerAngles;
                targetEulerBinding = configured;

                if (LerpType == LerpType.Add)
                {
                    if (ToggleWithOriginalState)
                    {
                        Vector3 origWorldEuler = PeekOriginalWorldOrCurrent(tr, t => t.eulerAngles);
                        Vector3 baseBindingEuler = EulerWorldToBinding(tr, origWorldEuler);
                        targetEulerBinding = baseBindingEuler + configured;
                    }
                    else
                    {
                        Vector3 startBindingEuler = (WhatIsGoingToAffect == TransformApplyType.World) ? tr.eulerAngles : tr.localEulerAngles;
                        targetEulerBinding = startBindingEuler + configured;
                    }
                }

                targetBindingRot = Quaternion.Euler(targetEulerBinding);
            }
            else
            {
                Quaternion configured = NewRot;
                targetBindingRot = configured;

                if (LerpType == LerpType.Add)
                {
                    if (ToggleWithOriginalState)
                    {
                        Quaternion origWorld = PeekOriginalWorldOrCurrent(tr, t => t.rotation);
                        Quaternion baseBinding = RotationWorldToBinding(tr, origWorld);
                        targetBindingRot = baseBinding * configured;
                    }
                    else
                    {
                        Quaternion startBinding = (WhatIsGoingToAffect == TransformApplyType.World) ? tr.rotation : tr.localRotation;
                        targetBindingRot = startBinding * configured;
                    }
                }
            }

            Quaternion finalWorldRot = RotationBindingToWorld(tr, targetBindingRot);

            Vector3 finalWorldPos;
            if (WhatIsGoingToAffect == TransformApplyType.World)
            {
                finalWorldPos = anchorWorldStart - (finalWorldRot * anchorLocal);
            }
            else
            {
                var p = tr.parent;
                if (p)
                {
                    Vector3 parentSpaceAnchorWorld = p.InverseTransformPoint(anchorWorldStart);
                    Vector3 finalLocalPos = parentSpaceAnchorWorld - (targetBindingRot * anchorLocal);
                    finalWorldPos = p.TransformPoint(finalLocalPos);
                }
                else
                {
                    finalWorldPos = anchorWorldStart - (finalWorldRot * anchorLocal);
                }
            }

            float u = __Editor_GetTimelineU();
            float k = __Editor_EvalBaseCurve(u);

            Quaternion previewBindingRot;
            if (useEuler)
            {
                Vector3 startEulerBinding = (WhatIsGoingToAffect == TransformApplyType.World) ? tr.eulerAngles : tr.localEulerAngles;
                Vector3 previewEulerBinding = __Editor_LerpEulerAngles(startEulerBinding, targetEulerBinding, k);
                previewBindingRot = Quaternion.Euler(previewEulerBinding);
            }
            else
            {
                Quaternion startBinding = (WhatIsGoingToAffect == TransformApplyType.World) ? tr.rotation : tr.localRotation;
                previewBindingRot = Quaternion.Slerp(startBinding, targetBindingRot, k);
            }

            Quaternion previewWorldRot = RotationBindingToWorld(tr, previewBindingRot);

            Vector3 previewWorldPos;
            if (WhatIsGoingToAffect == TransformApplyType.World)
            {
                previewWorldPos = anchorWorldStart - (previewWorldRot * anchorLocal);
            }
            else
            {
                var p = tr.parent;
                if (p)
                {
                    Vector3 parentSpaceAnchorWorld = p.InverseTransformPoint(anchorWorldStart);
                    Vector3 previewLocalPos = parentSpaceAnchorWorld - (previewBindingRot * anchorLocal);
                    previewWorldPos = p.TransformPoint(previewLocalPos);
                }
                else
                {
                    previewWorldPos = anchorWorldStart - (previewWorldRot * anchorLocal);
                }
            }

            DrawPreviewModel(tr, previewWorldPos, previewWorldRot, tr.lossyScale);

            Quaternion startWorldRot = tr.rotation;

            float ang = Quaternion.Angle(startWorldRot, finalWorldRot);
            if (ang > 0.5f)
            {
                Vector3 arcLocalPoint = ResolveArcLocalPoint(tr, anchorLocal);
                Vector3 localOffset = arcLocalPoint - anchorLocal;

                int steps = Mathf.Clamp(Mathf.CeilToInt(ang / 5f), 8, 64);

                var pts = new List<Vector3>(steps + 1);
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Quaternion r = Quaternion.Slerp(startWorldRot, finalWorldRot, t);

                    Vector3 p = anchorWorldStart + (r * localOffset);
                    pts.Add(p);
                }

                DrawPreviewDottedPath(pts);
            }

        }

        [MenuItem("CONTEXT/RotationLerp/Open Documentation", false)]
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
