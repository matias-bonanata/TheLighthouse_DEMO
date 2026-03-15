using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FastStudios
{
    public abstract class PressELerps : MonoBehaviour
    {
        public bool AffectOtherObject = false;
        public Transform ObjectToMove;
        public TransformApplyType WhatIsGoingToAffect = TransformApplyType.World;
        public LerpType LerpType = LerpType.Set;

        public bool WaitTillAnimationFinished = false;
        public bool ToggleWithOriginalState = false;
        public float Duration = 1f;
        public AnimationCurve animCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public UnityEvent OnStart;
        public UnityEvent OnComplete;

        object _originalWorld;
        bool _originalSet;
        bool _animationBlock;
        bool _toggleToOriginal;

        public bool ShowGizmos = true;
        public bool ShowGizmosOnlyWhenSelected = true;
        
        #if UNITY_EDITOR
        private const float GizmosPreviewOpacity = 0.7f;
        private bool UseCapsuleDottedLine = true;
        private const float GizmosDottedLineSize = 6f;
        private const float GizmosDottedLineThickness = 2.5f;
        private const float GizmosDottedLineMinDashWorld = 0.03f;
        private const float GizmosDottedLineMinGapWorld = 0.005f;
        private const float GizmosDottedLineGapMultiplier = 0.25f;
        private DottedSpace GizmosDottedLineThicknessSpace = DottedSpace.World;
        private DottedSpace GizmosDottedLineSizeSpace = DottedSpace.World;
        private const float GizmosDottedLineSizeWorldScale = 0.02f;
        private const float GizmosDottedLineThicknessWorldScale = 0.02f;
        private PreviewMaterialMode GizmosPreviewMaterialMode = PreviewMaterialMode.FastTexturedGhost;
        public enum DottedSpace { World, ScreenPixels }
        private enum PreviewMaterialMode { AccurateCloneTransparent, FastTexturedGhost }
        #endif

        public abstract void Interact();

        protected void InteractPosition(Vector3 configuredTarget)
        {
            var tr = ResolveTransform();
            if (!tr) return;

            Func<Transform, Vector3> getWorld = t => t.position;
            Func<Transform, Vector3> getBinding = (WhatIsGoingToAffect == TransformApplyType.World)
                                                  ? getWorld
                                                  : (Func<Transform, Vector3>)(t => t.localPosition);
            Action<Transform, Vector3> setBinding = (WhatIsGoingToAffect == TransformApplyType.World)
                                                    ? (t, v) => t.position = v
                                                    : (t, v) => t.localPosition = v;

            Vector3 WorldToBinding(Transform t, Vector3 worldVal)
            {
                if (WhatIsGoingToAffect == TransformApplyType.World) return worldVal;
                var p = t.parent;
                return p ? p.InverseTransformPoint(worldVal) : worldVal;
            }

            StartLerpSimple(
                tr,
                configuredTarget,
                captureOriginalWorld: getWorld,
                getter: getBinding,
                setter: setBinding,
                lerp: Vector3.Lerp,
                worldToBinding: (t, v) => WorldToBinding(t, v),
                addOp: (a, b) => a + b
            );
        }

        protected void InteractRotation(Quaternion configuredTarget)
        {
            var tr = ResolveTransform();
            if (!tr) return;

            Vector3 anchorLocal = ResolveAnchorLocal(tr);
            Vector3 anchorWorldStart = tr.TransformPoint(anchorLocal);

            Func<Transform, Quaternion> getWorld = t => t.rotation;
            Func<Transform, Quaternion> getBinding =
                (WhatIsGoingToAffect == TransformApplyType.World)
                ? getWorld
                : (Func<Transform, Quaternion>)(t => t.localRotation);

            Action<Transform, Quaternion> setBinding = (t, q) =>
            {
                if (WhatIsGoingToAffect == TransformApplyType.World)
                {
                    t.rotation = q;
                    t.position = anchorWorldStart - (t.rotation * anchorLocal);
                }
                else
                {
                    t.localRotation = q;
                    if (t.parent)
                    {
                        Vector3 parentSpaceAnchorWorld = t.parent.InverseTransformPoint(anchorWorldStart);
                        t.localPosition = parentSpaceAnchorWorld - (t.localRotation * anchorLocal);
                    }
                    else
                    {
                        t.position = anchorWorldStart - (t.rotation * anchorLocal);
                    }
                }
            };

            Quaternion WorldToBinding(Transform t, Quaternion worldRot)
            {
                if (WhatIsGoingToAffect == TransformApplyType.World) return worldRot;
                var p = t.parent;
                return p ? Quaternion.Inverse(p.rotation) * worldRot : worldRot;
            }

            StartLerpSimple(
                tr,
                configuredTarget,
                captureOriginalWorld: getWorld,
                getter: getBinding,
                setter: setBinding,
                lerp: Quaternion.Slerp,
                worldToBinding: (t, v) => WorldToBinding(t, v),
                addOp: (a, b) => a * b
            );
        }

        protected void InteractRotation(Vector3 configuredEuler)
        {
            var tr = ResolveTransform();
            if (!tr) return;

            Vector3 anchorLocal = ResolveAnchorLocal(tr);
            Vector3 anchorWorldStart = tr.TransformPoint(anchorLocal);

            Func<Transform, Vector3> getWorld = t => t.eulerAngles;
            Func<Transform, Vector3> getBinding =
                (WhatIsGoingToAffect == TransformApplyType.World)
                ? getWorld
                : (Func<Transform, Vector3>)(t => t.localEulerAngles);

            Action<Transform, Vector3> setBinding = (t, euler) =>
            {
                if (WhatIsGoingToAffect == TransformApplyType.World)
                {
                    t.rotation = Quaternion.Euler(euler);
                    t.position = anchorWorldStart - (t.rotation * anchorLocal);
                }
                else
                {
                    t.localRotation = Quaternion.Euler(euler);
                    if (t.parent)
                    {
                        Vector3 parentSpaceAnchorWorld = t.parent.InverseTransformPoint(anchorWorldStart);
                        t.localPosition = parentSpaceAnchorWorld - (t.localRotation * anchorLocal);
                    }
                    else
                    {
                        t.position = anchorWorldStart - (t.rotation * anchorLocal);
                    }
                }
            };

            Vector3 WorldToBinding(Transform t, Vector3 worldEuler)
            {
                if (WhatIsGoingToAffect == TransformApplyType.World) return worldEuler;
                var p = t.parent;
                if (!p) return worldEuler;
                var localQ = Quaternion.Inverse(p.rotation) * Quaternion.Euler(worldEuler);
                return localQ.eulerAngles;
            }

            StartLerpSimple(
                tr,
                configuredEuler,
                captureOriginalWorld: getWorld,
                getter: getBinding,
                setter: setBinding,
                lerp: Vector3.Lerp,
                worldToBinding: (t, v) => WorldToBinding(t, v),
                addOp: (a, b) => a + b
            );
        }

        protected void InteractScale(Vector3 configuredTarget)
        {
            var tr = ResolveTransform();
            if (!tr) return;

            Func<Transform, Vector3> getWorld = t => t.lossyScale;
            Func<Transform, Vector3> getBinding = (WhatIsGoingToAffect == TransformApplyType.World)
                                                     ? getWorld
                                                     : (Func<Transform, Vector3>)(t => t.localScale);
            Action<Transform, Vector3> setBinding = (WhatIsGoingToAffect == TransformApplyType.World)
                                                       ? (t, v) => t.localScale = v
                                                       : (t, v) => t.localScale = v;

            Vector3 WorldToBinding(Transform t, Vector3 worldVal)
            {
                if (WhatIsGoingToAffect == TransformApplyType.World)
                {
                    var p = t.parent;
                    if (!p) return worldVal;
                    var pls = p.lossyScale;
                    return new Vector3(
                        pls.x != 0 ? worldVal.x / pls.x : worldVal.x,
                        pls.y != 0 ? worldVal.y / pls.y : worldVal.y,
                        pls.z != 0 ? worldVal.z / pls.z : worldVal.z
                    );
                }
                return worldVal;
            }


            StartLerpSimple(
                tr,
                configuredTarget,
                captureOriginalWorld: getWorld,
                getter: getBinding,
                setter: setBinding,
                lerp: Vector3.Lerp,
                worldToBinding: (t, v) => WorldToBinding(t, v),
                addOp: (a, b) => a + b
            );
        }

        void StartLerpSimple<T>(
            Transform tr,
            T configuredTarget,
            Func<Transform, T> captureOriginalWorld,
            Func<Transform, T> getter,
            Action<Transform, T> setter,
            Func<T, T, float, T> lerp,
            Func<Transform, T, T> worldToBinding,
            Func<T, T, T> addOp = null
        )
        {
            if (!_originalSet)
            {
                _originalWorld = captureOriginalWorld(tr);
                _originalSet = true;
            }

            if (WaitTillAnimationFinished && _animationBlock)
                return;

            if (!WaitTillAnimationFinished)
                StopAllCoroutines();

            T start = getter(tr);

            T target;

            if (LerpType == LerpType.Add && addOp != null)
            {
                if (ToggleWithOriginalState)
                {
                    T baseTarget = worldToBinding(tr, (T)_originalWorld);
                    T addedTarget = addOp(baseTarget, configuredTarget);

                    target = _toggleToOriginal ? baseTarget : addedTarget;
                    _toggleToOriginal = !_toggleToOriginal;
                }
                else
                {
                    target = addOp(start, configuredTarget);
                }
            }
            else
            {
                target = configuredTarget;

                if (ToggleWithOriginalState)
                {
                    if (_toggleToOriginal)
                        target = worldToBinding(tr, (T)_originalWorld);

                    _toggleToOriginal = !_toggleToOriginal;
                }
            }

            if (WaitTillAnimationFinished) _animationBlock = true;
            OnStart?.Invoke();
            StartCoroutine(LerpRoutine(
                start, target, v => setter(tr, v), Duration, animCurve,
                onComplete: () =>
                {
                    OnComplete?.Invoke();
                    _animationBlock = false;
                },
                lerp: lerp
            ));
        }

        IEnumerator LerpRoutine<T>(
            T startValue,
            T endValue,
            Action<T> apply,
            float duration,
            AnimationCurve curve,
            Action onComplete,
            Func<T, T, float, T> lerp
        )
        {
            float t = 0f;
            while (t < duration)
            {
                float k = (curve != null) ? curve.Evaluate(t / duration) : (t / duration);
                apply(lerp(startValue, endValue, k));
                t += Time.deltaTime;
                yield return null;
            }
            apply(endValue);
            onComplete?.Invoke();
        }

        Transform ResolveTransform()
        {
            if (!AffectOtherObject || ObjectToMove == null) return transform;
            return ObjectToMove;
        }

        public void ChangeOriginalState(Vector3 newWorldPos)
        {
            _originalWorld = newWorldPos;
            _originalSet = true;
        }

        public bool IsInOriginalState()
        {
            return !_toggleToOriginal;
        }

        protected Vector3 ToLocalScale(Transform tx, Vector3 worldScale)
        {
            var p = tx.parent;
            if (!p) return worldScale;
            var pls = p.lossyScale;
            return new Vector3(
                pls.x != 0 ? worldScale.x / pls.x : worldScale.x,
                pls.y != 0 ? worldScale.y / pls.y : worldScale.y,
                pls.z != 0 ? worldScale.z / pls.z : worldScale.z
            );
        }

        protected Vector3 ResolveAnchorLocal(Transform target)
        {
            Interactable inter = target.GetComponent<Interactable>();
            if (inter != null && inter.WillOverrideAnchor)
                return inter.LocalPositionNewAnchor;

            return Vector3.zero;
        }

#if UNITY_EDITOR
        private struct TimelinePreviewState
        {
            public bool enabled;
            public float time;
        }

        private static readonly Dictionary<int, TimelinePreviewState> _timelinePreviewById = new();

        public static void __Editor_SetTimelinePreview(PressELerps lerp, bool enabled, float time)
        {
            if (lerp == null) return;

            int id = lerp.GetInstanceID();

            if (!enabled)
            {
                _timelinePreviewById.Remove(id);
                return;
            }

            _timelinePreviewById[id] = new TimelinePreviewState
            {
                enabled = true,
                time = Mathf.Max(0f, time)
            };
        }

        public static void __Editor_ClearTimelinePreview(PressELerps lerp)
        {
            if (lerp == null) return;
            _timelinePreviewById.Remove(lerp.GetInstanceID());
        }

        protected static bool __Editor_TryGetTimelinePreview(PressELerps lerp, out float time)
        {
            time = 0f;
            if (lerp == null) return false;

            if (_timelinePreviewById.TryGetValue(lerp.GetInstanceID(), out var st) && st.enabled)
            {
                time = st.time;
                return true;
            }
            return false;
        }

        protected float __Editor_GetTimelineU()
        {
            if (__Editor_TryGetTimelinePreview(this, out float time))
            {
                float dur = Mathf.Max(0.0001f, Duration);
                return Mathf.Clamp01(time / dur);
            }
            return 1f;
        }

        protected float __Editor_EvalBaseCurve(float u)
        {
            return (animCurve != null) ? animCurve.Evaluate(u) : u;
        }

        protected static Vector3 __Editor_LerpEulerAngles(Vector3 a, Vector3 b, float t)
        {
            return new Vector3(
                Mathf.LerpAngle(a.x, b.x, t),
                Mathf.LerpAngle(a.y, b.y, t),
                Mathf.LerpAngle(a.z, b.z, t)
            );
        }

        void OnDrawGizmos()
        {
            if (!ShowGizmos || ShowGizmosOnlyWhenSelected) return;
            DrawGizmosInternal();
        }

        void OnDrawGizmosSelected()
        {
            if (!ShowGizmos || !ShowGizmosOnlyWhenSelected) return;
            DrawGizmosInternal();
        }

        void DrawGizmosInternal()
        {
            var tr = ResolveTransform();
            if (!tr) return;
            DrawGizmoPreview(tr);
        }

        protected virtual void DrawGizmoPreview(Transform tr) { }

        protected void DrawPreviewDottedLine(Vector3 fromWorld, Vector3 toWorld, float screenSpaceSize = -1f)
        {
            if (screenSpaceSize <= 0f) screenSpaceSize = GizmosDottedLineSize;
            DrawPreviewDottedLineInternal(fromWorld, toWorld, screenSpaceSize, GizmosDottedLineThickness);
        }

        protected void DrawPreviewDottedPath(IList<Vector3> points, float screenSpaceSize = -1f)
        {
            if (points == null || points.Count < 2) return;

            if (screenSpaceSize <= 0f) screenSpaceSize = GizmosDottedLineSize;

            Vector3 mid = points[points.Count / 2];
            var cam = GetGizmosCamera();
            float wpp = WorldPerPixel(cam, mid);

            float radius = (GizmosDottedLineThicknessSpace == DottedSpace.ScreenPixels)
                ? Mathf.Max(1e-6f, wpp * (GizmosDottedLineThickness * 0.5f))
                : Mathf.Max(1e-6f, GizmosDottedLineThickness * GizmosDottedLineThicknessWorldScale * 0.5f);

            float sizeWorld = (GizmosDottedLineSizeSpace == DottedSpace.ScreenPixels)
                ? (wpp * screenSpaceSize)
                : (screenSpaceSize * GizmosDottedLineSizeWorldScale);

            float dash = Mathf.Max(GizmosDottedLineMinDashWorld, sizeWorld);

            float gapSize = sizeWorld * GizmosDottedLineGapMultiplier;
            float gap = Mathf.Max(GizmosDottedLineMinGapWorld, gapSize);

            dash = Mathf.Max(dash, radius * 2.4f);

            float total = 0f;
            for (int i = 0; i < points.Count - 1; i++)
                total += Vector3.Distance(points[i], points[i + 1]);

            if (total <= 0.0001f) return;

            for (float s = 0f; s < total; s += dash + gap)
            {
                float e = Mathf.Min(s + dash, total);

                Vector3 a = PointOnPath(points, s);
                Vector3 b = PointOnPath(points, e);

                Vector3 dir = b - a;
                float len = dir.magnitude;
                if (len <= 0.00001f) continue;
                dir /= len;

                if (UseCapsuleDottedLine) DrawCapsuleDottedDash(a, b, dir, radius);
                else DrawCrispThickLine(a, b, GizmosDottedLineThickness);
            }
        }

        protected Vector3 ResolveArcLocalPoint(Transform root, Vector3 anchorLocal)
        {
            var items = GetRenderItems(root);
            if (items == null || items.Count == 0)
                return anchorLocal + Vector3.right;

            bool hasAny = false;
            Bounds b = new Bounds();

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.mesh == null) continue;

                var mb = it.mesh.bounds;
                Vector3 c = mb.center;
                Vector3 e = mb.extents;

                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sy = -1; sy <= 1; sy += 2)
                        for (int sz = -1; sz <= 1; sz += 2)
                        {
                            Vector3 pMesh = c + new Vector3(e.x * sx, e.y * sy, e.z * sz);
                            Vector3 pRoot = it.localToRoot.MultiplyPoint3x4(pMesh);

                            if (!hasAny) { b = new Bounds(pRoot, Vector3.zero); hasAny = true; }
                            else b.Encapsulate(pRoot);
                        }
            }

            if (!hasAny)
                return anchorLocal + Vector3.right;

            float y = b.min.y;
            Vector3 center = b.center;
            Vector3 ext = b.extents;

            Vector3 best = new Vector3(center.x, y, center.z);
            float bestSqr = -1f;

            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 p = new Vector3(center.x + ext.x * sx, y, center.z + ext.z * sz);
                    float sqr = (p - anchorLocal).sqrMagnitude;
                    if (sqr > bestSqr) { bestSqr = sqr; best = p; }
                }

            return best;
        }


        Vector3 PointOnPath(IList<Vector3> points, float distance)
        {
            float d = 0f;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                float seg = Vector3.Distance(a, b);

                if (seg <= 0.000001f) continue;

                if (d + seg >= distance)
                {
                    float t = (distance - d) / seg;
                    return Vector3.Lerp(a, b, t);
                }

                d += seg;
            }

            return points[points.Count - 1];
        }

        Camera GetGizmosCamera()
        {
            var sv = SceneView.currentDrawingSceneView ?? SceneView.lastActiveSceneView;
            var cam = (sv != null) ? sv.camera : null;
            return cam ? cam : Camera.current;
        }

        float WorldPerPixel(Camera cam, Vector3 worldPos)
        {
            if (!cam) return 0.001f;

            if (cam.orthographic)
                return (cam.orthographicSize * 2f) / Mathf.Max(1f, cam.pixelHeight);

            float dist = Vector3.Dot(worldPos - cam.transform.position, cam.transform.forward);
            if (dist < 0f) dist = Vector3.Distance(cam.transform.position, worldPos);
            dist = Mathf.Max(0.01f, dist);

            float frustumHeight = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return frustumHeight / Mathf.Max(1f, cam.pixelHeight);
        }

        void DrawPreviewDottedLineInternal(Vector3 fromWorld, Vector3 toWorld, float screenSpaceSize, float thickness)
        {
            Vector3 d = toWorld - fromWorld;
            float dist = d.magnitude;
            if (dist <= 0.0001f) return;

            Vector3 dir = d / dist;

            Vector3 mid = (fromWorld + toWorld) * 0.5f;
            var cam = GetGizmosCamera();
            float wpp = WorldPerPixel(cam, mid);

            float radius;

            if (GizmosDottedLineThicknessSpace == DottedSpace.ScreenPixels)
            {
                radius = Mathf.Max(1e-6f, wpp * (thickness * 0.5f));
            }
            else
            {
                radius = Mathf.Max(1e-6f, thickness * GizmosDottedLineThicknessWorldScale * 0.5f);
            }


            float sizeWorld = (GizmosDottedLineSizeSpace == DottedSpace.ScreenPixels)
                            ? (wpp * screenSpaceSize)
                            : (screenSpaceSize * GizmosDottedLineSizeWorldScale);

            float dash = Mathf.Max(GizmosDottedLineMinDashWorld, sizeWorld);

            float gapSize = sizeWorld * GizmosDottedLineGapMultiplier;
            float gap = Mathf.Max(GizmosDottedLineMinGapWorld, gapSize);

            dash = Mathf.Max(dash, radius * 2.4f);

            float t = 0f;
            while (t < dist)
            {
                float tEnd = Mathf.Min(t + dash, dist);
                Vector3 a = fromWorld + dir * t;
                Vector3 b = fromWorld + dir * tEnd;

                if (UseCapsuleDottedLine) DrawCapsuleDottedDash(a, b, dir, radius);
                else DrawCrispThickLine(a, b, thickness);

                t += dash + gap;
            }
        }

        static Mesh s_LineSphereMesh;
        static Mesh s_LineCylinderMesh;
        static float s_SphereBaseRadius;
        static float s_CylinderBaseRadius;
        static float s_CylinderBaseHeight;

        static void EnsureLinePrimitiveMeshes()
        {
            if (s_LineSphereMesh != null && s_LineCylinderMesh != null) return;

            s_LineSphereMesh = GetPrimitiveMesh(PrimitiveType.Sphere, out s_SphereBaseRadius, out _);
            s_LineCylinderMesh = GetPrimitiveMesh(PrimitiveType.Cylinder, out s_CylinderBaseRadius, out s_CylinderBaseHeight);
        }

        static Mesh GetPrimitiveMesh(PrimitiveType type, out float baseRadius, out float baseHeight)
        {
            var go = GameObject.CreatePrimitive(type);
            go.hideFlags = HideFlags.HideAndDontSave;

            var mf = go.GetComponent<MeshFilter>();
            var mesh = mf ? mf.sharedMesh : null;

            var b = mesh ? mesh.bounds : new Bounds(Vector3.zero, Vector3.one);

            baseRadius = Mathf.Max(0.0001f, Mathf.Max(b.extents.x, b.extents.z));
            baseHeight = Mathf.Max(0.0001f, b.size.y);

            UnityEngine.Object.DestroyImmediate(go);
            return mesh;
        }

        void DrawCapsuleDottedDash(Vector3 a, Vector3 b, Vector3 dir, float radius)
        {
            EnsureLinePrimitiveMeshes();

            var mat = GetGhostMat(1f);
            mat.SetPass(0);

            float segLen = Vector3.Distance(a, b);
            if (segLen <= 0.0001f) return;

            radius = Mathf.Max(1e-6f, radius);
            radius = Mathf.Min(radius, segLen * 0.45f);

            if (segLen <= radius * 2.05f)
            {
                DrawLineSphere((a + b) * 0.5f, radius);
                return;
            }

            Vector3 startCap = a + dir * radius;
            Vector3 endCap = b - dir * radius;

            DrawLineSphere(startCap, radius);
            DrawLineSphere(endCap, radius);

            float cylLen = Vector3.Distance(startCap, endCap);
            if (cylLen <= 0.0001f) return;

            Quaternion rot = Quaternion.FromToRotation(Vector3.up, dir);
            Vector3 mid = (startCap + endCap) * 0.5f;

            Vector3 s = new Vector3(
                radius / s_CylinderBaseRadius,
                cylLen / s_CylinderBaseHeight,
                radius / s_CylinderBaseRadius
            );

            Graphics.DrawMeshNow(s_LineCylinderMesh, Matrix4x4.TRS(mid, rot, s));
        }

        void DrawLineSphere(Vector3 center, float radius)
        {
            Vector3 s = Vector3.one * (radius / s_SphereBaseRadius);
            Graphics.DrawMeshNow(s_LineSphereMesh, Matrix4x4.TRS(center, Quaternion.identity, s));
        }


        void DrawCrispThickLine(Vector3 a, Vector3 b, float thickness)
        {
            int lines = Mathf.Clamp(Mathf.RoundToInt(thickness), 1, 12);

            var sv = SceneView.currentDrawingSceneView ?? SceneView.lastActiveSceneView;
            var cam = (sv != null) ? sv.camera : Camera.current;

            Vector3 dir = b - a;
            float dist = dir.magnitude;
            if (dist <= 0.00001f)
            {
                Handles.DrawLine(a, b);
                return;
            }
            dir /= dist;

            Vector3 viewFwd = cam ? cam.transform.forward : Vector3.forward;

            Vector3 perp = Vector3.Cross(dir, viewFwd);
            if (perp.sqrMagnitude < 0.000001f)
                perp = cam ? cam.transform.right : Vector3.right;
            perp.Normalize();

            float handleSize = HandleUtility.GetHandleSize((a + b) * 0.5f);
            float step = handleSize * 0.0015f;

            float half = (lines - 1) * 0.5f;
            for (int i = 0; i < lines; i++)
            {
                float o = (i - half) * step;
                Handles.DrawLine(a + perp * o, b + perp * o);
            }
        }


        static Material s_GhostMat;

        static Material GetGhostMat(float alpha)
        {
            if (s_GhostMat == null)
            {
                var sh = Shader.Find("Hidden/Internal-Colored");
                s_GhostMat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };

                s_GhostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                s_GhostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                s_GhostMat.SetInt("_ZWrite", 0);
                s_GhostMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                s_GhostMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }

            s_GhostMat.SetColor("_Color", new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            return s_GhostMat;
        }

        static readonly Dictionary<int, Material> s_TransparentMatCache = new();

        static Material GetTransparentMat(Material src, float alpha)
        {
            if (src == null) return GetGhostMat(alpha);

            int id = src.GetInstanceID();
            if (!s_TransparentMatCache.TryGetValue(id, out var m) || m == null)
            {
                m = new Material(src) { hideFlags = HideFlags.HideAndDontSave };
                ForceTransparent(m);
                s_TransparentMatCache[id] = m;
            }

            ApplyAlpha(m, alpha);
            return m;
        }

        static Material s_TexturedGhostMat;

        static Material GetTexturedGhostMat()
        {
            if (s_TexturedGhostMat != null) return s_TexturedGhostMat;

            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (!sh) sh = Shader.Find("Unlit/Transparent");
            if (!sh) sh = Shader.Find("Hidden/Internal-Colored");

            s_TexturedGhostMat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };

            ForceTransparent(s_TexturedGhostMat);

            return s_TexturedGhostMat;
        }

        static Material GetFastTexturedGhost(Material src, float alpha)
        {
            var m = GetTexturedGhostMat();
            alpha = Mathf.Clamp01(alpha);

            if (m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", null);
                m.SetTextureScale("_BaseMap", Vector2.one);
                m.SetTextureOffset("_BaseMap", Vector2.zero);
            }
            if (m.HasProperty("_MainTex"))
            {
                m.SetTexture("_MainTex", null);
                m.SetTextureScale("_MainTex", Vector2.one);
                m.SetTextureOffset("_MainTex", Vector2.zero);
            }


            Texture tex = null;
            Vector2 scale = Vector2.one;
            Vector2 offset = Vector2.zero;

            if (src)
            {
                if (src.HasProperty("_BaseMap"))
                {
                    tex = src.GetTexture("_BaseMap");
                    if (tex)
                    {
                        scale = src.GetTextureScale("_BaseMap");
                        offset = src.GetTextureOffset("_BaseMap");
                    }
                }

                if (!tex && src.HasProperty("_MainTex"))
                {
                    tex = src.GetTexture("_MainTex");
                    if (tex)
                    {
                        scale = src.GetTextureScale("_MainTex");
                        offset = src.GetTextureOffset("_MainTex");
                    }
                }
            }

            if (tex)
            {
                if (m.HasProperty("_BaseMap"))
                {
                    m.SetTexture("_BaseMap", tex);
                    m.SetTextureScale("_BaseMap", scale);
                    m.SetTextureOffset("_BaseMap", offset);
                }

                if (m.HasProperty("_MainTex"))
                {
                    m.SetTexture("_MainTex", tex);
                    m.SetTextureScale("_MainTex", scale);
                    m.SetTextureOffset("_MainTex", offset);
                }
            }

            Color c = Color.white;
            if (src)
            {
                if (src.HasProperty("_BaseColor")) c = src.GetColor("_BaseColor");
                else if (src.HasProperty("_Color")) c = src.GetColor("_Color");
            }
            c.a = alpha;

            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);

            return m;
        }

        static void ForceTransparent(Material m)
        {
            if (m == null) return;

            m.SetOverrideTag("RenderType", "Transparent");

            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 2f);

            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);

            if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);

            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        static void ApplyAlpha(Material m, float alpha)
        {
            if (m == null) return;
            alpha = Mathf.Clamp01(alpha);

            if (m.HasProperty("_Color"))
            {
                var c = m.GetColor("_Color");
                c.a = alpha;
                m.SetColor("_Color", c);
            }

            if (m.HasProperty("_BaseColor"))
            {
                var c = m.GetColor("_BaseColor");
                c.a = alpha;
                m.SetColor("_BaseColor", c);
            }
        }

        struct RenderItem
        {
            public Mesh mesh;
            public Material[] mats;
            public Matrix4x4 localToRoot;
            public int subMeshCount;
        }

        static readonly Dictionary<int, List<RenderItem>> s_RenderCache = new();

        static void ClearRenderCacheAndRepaint()
        {
            s_RenderCache.Clear();

            SceneView.RepaintAll();
        }

        static void PrewarmAllPressELerpPreviews()
        {
            EnsureLinePrimitiveMeshes();

            var lerps = UnityEngine.Object.FindObjectsByType<PressELerps>(FindObjectsSortMode.None);
            for (int i = 0; i < lerps.Length; i++)
            {
                var l = lerps[i];
                if (!l) continue;

                var tr = l.ResolveTransform();
                if (!tr) continue;

                var items = GetRenderItems(tr);
                if (items == null) continue;

                for (int r = 0; r < items.Count; r++)
                {
                    var it = items[r];
                    if (it.mats == null) continue;

                    for (int m = 0; m < it.mats.Length; m++)
                    {
                        var src = it.mats[m];
                        if (!src) continue;

                        GetTransparentMat(src, GizmosPreviewOpacity);
                    }
                }
            }
        }

        [InitializeOnLoadMethod]
        static void InitRenderCacheHooks()
        {
            EditorApplication.hierarchyChanged -= ClearRenderCacheAndRepaint;
            EditorApplication.hierarchyChanged += ClearRenderCacheAndRepaint;

            Undo.undoRedoPerformed -= ClearRenderCacheAndRepaint;
            Undo.undoRedoPerformed += ClearRenderCacheAndRepaint;

            EditorApplication.delayCall -= PrewarmAllPressELerpPreviews;
            EditorApplication.delayCall += PrewarmAllPressELerpPreviews;
        }

        static List<RenderItem> GetRenderItems(Transform root)
        {
            if (!root) return null;

            int id = root.GetInstanceID();
            if (s_RenderCache.TryGetValue(id, out var cached) && cached != null) return cached;

            var list = new List<RenderItem>(16);
            var rootT = root;

            var mfs = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in mfs)
            {
                if (mf == null || mf.sharedMesh == null) continue;

                var mr = mf.GetComponent<MeshRenderer>();
                var mats = mr ? mr.sharedMaterials : null;

                Matrix4x4 localToRoot = rootT.worldToLocalMatrix * mf.transform.localToWorldMatrix;

                list.Add(new RenderItem
                {
                    mesh = mf.sharedMesh,
                    mats = mats,
                    localToRoot = localToRoot,
                    subMeshCount = mf.sharedMesh.subMeshCount
                });
            }

            var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var sr in skinned)
            {
                if (sr == null || sr.sharedMesh == null) continue;

                Matrix4x4 localToRoot = rootT.worldToLocalMatrix * sr.transform.localToWorldMatrix;

                list.Add(new RenderItem
                {
                    mesh = sr.sharedMesh,
                    mats = sr.sharedMaterials,
                    localToRoot = localToRoot,
                    subMeshCount = sr.sharedMesh.subMeshCount
                });
            }

            s_RenderCache[id] = list;
            return list;
        }

        protected void DrawPreviewModel(Transform root, Vector3 worldPos, Quaternion worldRot, Vector3 worldScale)
        {
            if (!root) return;

            Matrix4x4 rootPreview = Matrix4x4.TRS(worldPos, worldRot, worldScale);

            var renderItems = GetRenderItems(root);
            bool drew = false;

            if (renderItems != null)
            {
                for (int i = 0; i < renderItems.Count; i++)
                {
                    var it = renderItems[i];
                    if (it.mesh == null) continue;

                    Matrix4x4 m = rootPreview * it.localToRoot;

                    int subCount = Mathf.Max(1, it.subMeshCount);
                    for (int sm = 0; sm < subCount; sm++)
                    {
                        Material src = null;

                        if (it.mats != null && it.mats.Length > 0)
                            src = (sm < it.mats.Length) ? it.mats[sm] : it.mats[it.mats.Length - 1];

                        Material mat =
                                (GizmosPreviewMaterialMode == PreviewMaterialMode.FastTexturedGhost)
                                    ? GetFastTexturedGhost(src, GizmosPreviewOpacity)
                                    : GetTransparentMat(src, GizmosPreviewOpacity);

                        if (mat != null) mat.SetPass(0);
                        Graphics.DrawMeshNow(it.mesh, m, sm);

                    }

                    drew = true;
                }
            }


            if (!drew)
            {
                var old = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(worldPos, worldRot, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 0.25f);
                Gizmos.matrix = old;
            }
        }

        protected T PeekOriginalWorldOrCurrent<T>(Transform tr, Func<Transform, T> captureWorld)
        {
            if (_originalSet && _originalWorld is T ok) return ok;
            return captureWorld(tr);
        }

        protected Vector3 PositionWorldToBinding(Transform t, Vector3 worldVal)
        {
            if (WhatIsGoingToAffect == TransformApplyType.World) return worldVal;
            var p = t.parent;
            return p ? p.InverseTransformPoint(worldVal) : worldVal;
        }

        protected Vector3 PositionBindingToWorld(Transform t, Vector3 bindingVal)
        {
            if (WhatIsGoingToAffect == TransformApplyType.World) return bindingVal;
            var p = t.parent;
            return p ? p.TransformPoint(bindingVal) : bindingVal;
        }

        protected Quaternion RotationWorldToBinding(Transform t, Quaternion worldRot)
        {
            if (WhatIsGoingToAffect == TransformApplyType.World) return worldRot;
            var p = t.parent;
            return p ? Quaternion.Inverse(p.rotation) * worldRot : worldRot;
        }

        protected Quaternion RotationBindingToWorld(Transform t, Quaternion bindingRot)
        {
            if (WhatIsGoingToAffect == TransformApplyType.World) return bindingRot;
            var p = t.parent;
            return p ? p.rotation * bindingRot : bindingRot;
        }

        protected Vector3 EulerWorldToBinding(Transform t, Vector3 worldEuler)
        {
            if (WhatIsGoingToAffect == TransformApplyType.World) return worldEuler;
            var p = t.parent;
            if (!p) return worldEuler;
            var localQ = Quaternion.Inverse(p.rotation) * Quaternion.Euler(worldEuler);
            return localQ.eulerAngles;
        }

        protected Vector3 ScaleLocalToWorld(Transform t, Vector3 localScale)
        {
            var p = t.parent;
            if (!p) return localScale;
            var pls = p.lossyScale;
            return new Vector3(localScale.x * pls.x, localScale.y * pls.y, localScale.z * pls.z);
        }
#endif
    }

}
