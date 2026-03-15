#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;
using FastStudios.EditorTools;

namespace FastStudios
{
    public partial class Interactable // Editor
    {
        bool deactivatedPhysics = false;
        bool instantWasActivated = false;
        void OnValidate()
        {
            if (GrabId == null) GrabId = gameObject.name;

            if (TransformGrabFollowSharpness >= 70) TransformGrabFollowSharpness = 70;
            if (TransformGrabRotationSharpness >= 70) TransformGrabRotationSharpness = 70;

            if (DragUIOverrideOnArc && DragUIOverrideAnchor) DragUIOverrideOnArc = false;

            if (deactivatedPhysics)
            {
                TransformGrabInstantFollow = instantWasActivated;
                deactivatedPhysics = false;
            }

            if (PhysicsGrabMode && TransformGrabInstantFollow)
            {
                instantWasActivated = TransformGrabInstantFollow;
                TransformGrabInstantFollow = false;
                deactivatedPhysics = true;
            }

            interactableRb = GetComponent<Rigidbody>();

#if UNITY_EDITOR
            if (!Application.isPlaying &&
                (interactMode == InteractMode.Drag || interactMode == InteractMode.Grab) &&
                interactableRb == null)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this && !GetComponent<Rigidbody>())
                        Undo.AddComponent<Rigidbody>(gameObject);
                };
            }
#endif

            if (DragRotGizmoRadiusScale != 1) DragRotGizmoRadiusScale = 1;

            startPercentage = Mathf.Clamp(startPercentage, 0f, 100f);

            float min, max;
            if (DragRotIsToClamp)
            {
                min = DragAngleClampMinMax.x;
                max = DragAngleClampMinMax.y;
            }
            else
            {
                min = 0f;
                max = Mathf.Max(0f, DragRotSliderMaxAngle);
            }
            OrderMinMax(ref min, ref max);

            if (rotClampStartType == RotClampStartType.Degree)
                startDegrees = Mathf.Clamp(startDegrees, min, max);
        }

        void Reset()
        {
            InteractableUniversalsSO.ApplyLoadOnCreation(this);
        }

        private void OnDrawGizmos()
        {
            if (!SeeGizmos || SeeGizmosOnlySelected) return;

            InternalDrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (!SeeGizmos || !SeeGizmosOnlySelected) return;

            InternalDrawGizmos();
        }

        private void InternalDrawGizmos()
        {
            if (interactMode == InteractMode.Drag)
            {
                if (DragHasMaxDistance && SeeDragDistanceGizmo)
                {
                    Transform space = transform.parent != null ? transform.parent : transform;

                    Vector3 centerWorld;
                    if (WillOverrideAnchor)
                    {
                        Vector3 originLocal = (Application.isPlaying && railSpace != null)
                            ? startLocal
                            : space.InverseTransformPoint(
                                  WillOverrideAnchor
                                      ? transform.TransformPoint(LocalPositionNewAnchor)
                                      : (interactableRb ? interactableRb.position : transform.position)
                              );
                        centerWorld = space.TransformPoint(originLocal);
                    }
                    else
                    {
                        centerWorld = WillOverrideAnchor
                            ? transform.TransformPoint(LocalPositionNewAnchor)
                            : (interactableRb ? interactableRb.position : transform.position);
                    }

                    Gizmos.color = DragDistanceEdgeColor;
                    Gizmos.DrawWireSphere(centerWorld, DragMaxDistance);
                }

                float gizmoR = DragRotationGizmoRadius;

                if (DragType == DragType.Position && SeeDragClampGizmos)
                {
                    Transform space = transform.parent != null ? transform.parent : transform;

                    Vector3 axisWorld, originWorld;
                    Vector3 axisLocalForK;

                    if (Application.isPlaying && (interactMode == InteractMode.Drag))
                    {
                        if (railWorldLocked)
                        {
                            axisWorld = railAxisWorld;
                            originWorld = railOriginWorld;
                            axisLocalForK = Vector3.right;
                        }
                        else
                        {
                            space = (transform.parent != null) ? transform.parent : transform;
                            axisLocalForK = (axisN.sqrMagnitude > 1e-8f)
                                ? axisN
                                : space.InverseTransformDirection(GetAxisWorldFromMoveAxis()).normalized;

                            axisWorld = space.TransformDirection(axisLocalForK).normalized;
                            originWorld = space.TransformPoint(startLocal);
                        }
                    }
                    else
                    {
                        space = (transform.parent != null) ? transform.parent : transform;
                        axisLocalForK = space.InverseTransformDirection(GetAxisWorldFromMoveAxis()).normalized;
                        axisWorld = space.TransformDirection(axisLocalForK).normalized;

                        originWorld = WillOverrideAnchor
                            ? transform.TransformPoint(LocalPositionNewAnchor)
                            : (interactableRb ? interactableRb.position : transform.position);
                    }

                    float k = railWorldLocked ? 1f : space.TransformVector(axisLocalForK).magnitude;
                    if (k < 1e-6f) k = 1f;

                    Gizmos.color = DragRailGizmosColor;

                    if (DragIsToClamp)
                    {
                        float sMinLocal = DragClampMinMax.x;
                        float sMaxLocal = DragClampMinMax.y;

                        Vector3 minWorld = originWorld + axisWorld * (sMinLocal * k);
                        Vector3 maxWorld = originWorld + axisWorld * (sMaxLocal * k);
                        Gizmos.DrawLine(minWorld, maxWorld);

                        Gizmos.color = DragLimitGizmosColor;
                        Gizmos.DrawSphere(minWorld, DragGizmoSphereRadius);
                        Gizmos.DrawSphere(maxWorld, DragGizmoSphereRadius);

                        Vector3 refWorld = WillOverrideAnchor
                            ? transform.TransformPoint(LocalPositionNewAnchor)
                            : (interactableRb ? interactableRb.position : transform.position);

                        float sNowW = Vector3.Dot(refWorld - originWorld, axisWorld);
                        float sNowL = sNowW / Mathf.Max(1e-6f, k);
                        float sNowClampedL = Mathf.Clamp(sNowL, sMinLocal, sMaxLocal);
                        float sNowClampedW = sNowClampedL * k;

                        Vector3 curWorld = originWorld + axisWorld * sNowClampedW;
                        Gizmos.color = DragCurrentGizmosColor;
                        Gizmos.DrawSphere(curWorld, DragGizmoCurrentRadius);
                    }
                    else
                    {
                        Gizmos.DrawLine(originWorld + axisWorld * -8192f,
                                        originWorld + axisWorld * 8192f);
                    }

                    if (DragUseSteps && DragStepCount >= 2)
                    {
                        float k2;
                        Vector3 axisLocalForK2;
                        if (railWorldLocked) { k2 = 1f; }
                        else
                        {
                            axisLocalForK2 = (axisN.sqrMagnitude > 1e-8f) ? axisN : Vector3.right;
                            k2 = (transform.parent ? transform.parent : transform).TransformVector(axisLocalForK2).magnitude;
                            if (k2 < 1e-6f) k2 = 1f;
                        }

                        float sMin, sMax;
                        if (DragIsToClamp)
                        {
                            sMin = DragClampMinMax.x; sMax = DragClampMinMax.y; OrderMinMax(ref sMin, ref sMax);
                        }
                        else
                        {
                            sMin = DragPosSliderMinMax.x; sMax = DragPosSliderMinMax.y; OrderMinMax(ref sMin, ref sMax);
                        }

                        Handles.color = DragStepGizmoColor;

                        for (int i = 0; i < DragStepCount; i++)
                        {
                            float t = (DragStepCount == 1) ? 0.5f : (float)i / (DragStepCount - 1);
                            float sLocal = Mathf.Lerp(sMin, sMax, t);
                            Vector3 p = originWorld + axisWorld * (sLocal * k2);

                            Vector3 n = Vector3.Cross(axisWorld, Vector3.up);
                            if (n.sqrMagnitude < 1e-6f) n = Vector3.Cross(axisWorld, Vector3.right);
                            n.Normalize();
                            Handles.DrawLine(p - n * DragStepGizmoTick, p + n * DragStepGizmoTick);
                        }
                    }
                }
                else if (DragType == DragType.Rotation && SeeGizmos)
                {
                    Vector3 axisWorld, originWorld, refDir;
                    Vector3 refLocalForCur;

                    if (Application.isPlaying)
                    {
                        axisWorld = railAxisWorld;
                        originWorld = railWorldLocked ? railOriginWorld
                                                      : ((transform.parent != null) ? transform.parent.TransformPoint(startLocal) : transform.position);
                        refDir = rotRefDirWorld;
                        refLocalForCur = rotRefLocal;
                    }
                    else
                    {
                        Transform space = (transform.parent != null) ? transform.parent : transform;
                        axisWorld = space.TransformDirection(space.InverseTransformDirection(GetAxisWorldFromMoveAxis())).normalized;
                        originWorld = WillOverrideAnchor ? transform.TransformPoint(LocalPositionNewAnchor)
                                                         : (interactableRb ? interactableRb.position : transform.position);

                        Vector3 hingeW = WillOverrideAnchor
                                            ? transform.TransformPoint(LocalPositionNewAnchor)
                                            : (interactableRb ? interactableRb.position : transform.position);

                        Vector3 tmpLocal;
                        refDir = ComputeAutoRotRefDirWorld(axisWorld, hingeW, out tmpLocal).normalized;
                        refLocalForCur = tmpLocal;

                        if (!Mathf.Approximately(DragRotOffsetDeg, 0f))
                        {
                            float off = RotDirSign * DragRotOffsetDeg;
                            refDir = Quaternion.AngleAxis(off, axisWorld) * refDir;

                            Vector3 axisLocal = transform.InverseTransformDirection(axisWorld).normalized;
                            refLocalForCur = Quaternion.AngleAxis(off, axisLocal) * refLocalForCur;
                        }
                    }

                    gizmoR = ResolveRotationRadius(axisWorld, originWorld);

                    if (!DragRotIsToClamp)
                    {
                        Handles.color = DragRotationArcColor;
                        Handles.DrawWireDisc(originWorld, axisWorld, gizmoR);
                    }
                    else
                    {
                        float gMin = DragAngleClampMinMax.x;
                        float gMax = DragAngleClampMinMax.y;
                        OrderMinMax(ref gMin, ref gMax);

                        float gMinVis = (RotDirSign > 0f) ? gMin : (360f - gMax);
                        float gMaxVis = (RotDirSign > 0f) ? gMax : (360f - gMin);

                        Vector3 startDir = Quaternion.AngleAxis(gMinVis, axisWorld) * refDir;
                        float sweep = gMaxVis - gMinVis; if (sweep < 0f) sweep += 360f;

                        Handles.color = DragRotationArcColor;
                        if (DragRotationFillArc)
                            Handles.DrawSolidArc(originWorld, axisWorld, startDir, sweep, gizmoR);
                        else
                            Handles.DrawWireArc(originWorld, axisWorld, startDir, sweep, gizmoR);

                        Handles.color = DragRotationLimitColor;
                        Vector3 minDir = Quaternion.AngleAxis(gMinVis, axisWorld) * refDir;
                        Vector3 maxDir = Quaternion.AngleAxis(gMaxVis, axisWorld) * refDir;
                        Handles.DrawLine(originWorld, originWorld + minDir.normalized * gizmoR);
                        Handles.DrawLine(originWorld, originWorld + maxDir.normalized * gizmoR);
                    }

                    if (DragUseSteps && DragStepCount >= 2)
                    {
                        float gMin = DragAngleClampMinMax.x, gMax = DragAngleClampMinMax.y;
                        OrderMinMax(ref gMin, ref gMax);

                        float gMinVis = (RotDirSign > 0f) ? gMin : (360f - gMax);
                        float gMaxVis = (RotDirSign > 0f) ? gMax : (360f - gMin);
                        float sweep = gMaxVis - gMinVis; if (sweep < 0f) sweep += 360f;

                        Handles.color = DragStepGizmoColor;

                        for (int i = 0; i < DragStepCount; i++)
                        {
                            float t = (DragStepCount == 1) ? 0.5f : (float)i / (DragStepCount - 1);
                            float a = gMinVis + sweep * t;
                            Vector3 dir = Quaternion.AngleAxis(a, axisWorld) * refDir;
                            Vector3 p0 = originWorld + dir.normalized * (gizmoR - DragStepGizmoTick);
                            Vector3 p1 = originWorld + dir.normalized * (gizmoR + DragStepGizmoTick);
                            Handles.DrawLine(p0, p1);
                        }
                    }

                    Vector3 curDir = Vector3.ProjectOnPlane(transform.TransformDirection(refLocalForCur), axisWorld);
                    if (curDir.sqrMagnitude < 1e-6f) curDir = Vector3.ProjectOnPlane(transform.right, axisWorld);
                    curDir.Normalize();

                    float theta = Vector3.SignedAngle(refDir, curDir, axisWorld);
                    Vector3 point = originWorld + (Quaternion.AngleAxis(theta, axisWorld) * refDir).normalized * gizmoR;
                    Handles.color = DragRotationCurrentColor;
                    Handles.DrawSolidDisc(point, axisWorld, DragGizmoCurrentRadius);
                }
            }
            else if (interactMode == InteractMode.Inspection)
            {
                DrawInspectionCameraPreviewGizmos();
            }

            if (SeeAnchorPointGizmos)
            {
                Gizmos.color = AnchorPointGizmosColor;
                if (WillOverrideAnchor == false) Gizmos.DrawSphere(transform.position, PointsRadius);
                else Gizmos.DrawSphere(transform.TransformPoint(LocalPositionNewAnchor), PointsRadius);
            }

            if (SeeCenterOfMassGizmos && interactableRb != null)
            {
                Gizmos.color = SeeCenterOfMassGizmosColor;
                Gizmos.DrawSphere(transform.TransformPoint(interactableRb.centerOfMass), PointsRadius);
            }

            if (HasSensor && DrawRadiusInEditor && SensorType == SensorType.Radius)
            {
                Vector3 center = transform.position + SensorOffset;

                Vector3 normal = Vector3.up;

                UnityEditor.Handles.color = new Color(1f, 0f, 0f, 1f);
                UnityEditor.Handles.DrawWireDisc(center, normal, SensorRadius);
                Gizmos.DrawSphere(center, PointsRadius);
            }
        }

        [ContextMenu("Collapse All")] void CtxCollapseAllFoldouts() => SetAllFoldoutsGeneric(false);
        [ContextMenu("Expand All")] void CtxExpandAllFoldouts() => SetAllFoldoutsGeneric(true);
        [MenuItem("CONTEXT/Interactable/Open Documentation", false)] static void OpenDoc() => InteractionManager.OpenDoc();

        public void SetNewAnchorButton()
        {
            GameObject overrideAnchorPosition = new GameObject("Temporary Anchor Adjuster");
            overrideAnchorPosition.AddComponent<AnchorChanger>().Setup(this, AnchorChangerType.AnchorPosition, ref WillOverrideAnchor, ref LocalPositionNewAnchor, ref isOverridingAnchorPosition);
        }

        public void SetNewWorldUIAnchorButton()
        {
            GameObject overrideWorldUIAnchorPosition = new GameObject("Temporary World UI Anchor Adjuster");
            overrideWorldUIAnchorPosition.AddComponent<AnchorChanger>().Setup(this, AnchorChangerType.WorldUIAnchor, ref WorldUIOverrideAnchor, ref LocalPositionWorldUIAnchor, ref isOverridingWorldUIAnchorPosition);
        }

        public void SetNewDragUIAnchorButton()
        {
            GameObject overrideDragUIAnchorPosition = new GameObject("Temporary Drag UI Anchor Adjuster");
            overrideDragUIAnchorPosition.AddComponent<AnchorChanger>().Setup(this, AnchorChangerType.DragUIAnchor, ref DragUIOverrideAnchor, ref LocalPositionDragUIAnchor, ref isOverridingDragUIAnchorPosition);

            if (DragUIOverrideOnArc) DragUIOverrideOnArc = false;
        }

        public void SetNewHoldUIAnchorButton()
        {
            GameObject overrideHoldUIAnchorPosition = new GameObject("Temporary Hold UI Anchor Adjuster");
            overrideHoldUIAnchorPosition.AddComponent<AnchorChanger>().Setup(this, AnchorChangerType.HoldUIAnchor, ref HoldUIOverrideAnchor, ref LocalPositionHoldUIAnchor, ref isOverridingHoldUIAnchorPosition);
        }

        #region Foldouts

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

        #endregion

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

        #region Camera Inspection Preview
        

        #endregion
    }
}

#endif