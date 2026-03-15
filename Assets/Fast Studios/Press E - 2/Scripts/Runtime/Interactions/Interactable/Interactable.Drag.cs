using UnityEngine;
using UnityEngine.Events;
using System;

namespace FastStudios
{
    public partial class Interactable // Drag
    {
        #region Drag Public
        [HideInInspector] public bool isDragging;

        public DragType DragType = DragType.Position;
        public MoveAxis MoveAxis = MoveAxis.ForwardBack;
        public DragRotDirection DragRotationDirection = DragRotDirection.ClockWise;
        public bool invertDirection;
        public bool ConsiderHitPlace = false;
        public float DragRotOffsetDeg = 0f;

        public bool DragUseSteps = false;
        [Min(2)] public int DragStepCount = 3;

        [HideInInspector] public float DragStepGizmoTick = 0.08f;
        [HideInInspector] public Color DragStepGizmoColor = new Color(1f, 1f, 1f, 0.9f);

        public bool DragHasMaxDistance = true;
        [Min(0.01f)] public float DragMaxDistance = 2.5f;

        public bool DragIsToClamp = false;
        public Vector2 DragClampMinMax = new Vector2(0, 0.5f);
        public bool UseRotStartPosition = false;
        public RotClampStartType rotClampStartType = RotClampStartType.Percentage;
        [Range(0, 100)] public float startPercentage = 0;
        public float startDegrees = 0;
        public bool AlwaysReturnToStartPos = false;
        public bool ReturnByTheSameDirection = true;

        public bool DragLocked => dragLocked;
        [HideInInspector] [Min(0.01f)] public float DragSpring = 120f;
        [HideInInspector] [Min(0.01f)] public float DragDamping = 50;
        [HideInInspector] [Min(0.01f)] public float DragRbDamping = 2;
        [HideInInspector] [Range(0f, 1f)] public float DragBounciness = 0.35f;

        public bool AutomaticRotationRadius = true;
        [Min(0f)] public float RotationRadius = 0.5f;
        public bool DragRotIsToClamp = false;
        public Vector2 DragAngleClampMinMax = new Vector2(0, 360);
        [HideInInspector][Min(0.01f)] public float DragRotLimitSpring = 450f;
        [HideInInspector][Min(0.01f)] public float DragRotLimitDamping = 2f;
        [HideInInspector][Min(0f)] public float DragRotIdleDamping = 8f;
        [HideInInspector][Min(0.01f)] public float DragRotSpring = 90f;
        [HideInInspector][Min(0.01f)] public float DragRotDamping = 30f;
        [HideInInspector] public float DragRotBounciness = 0.75f;

        public bool DragUseSlider = false;
        public Vector2 DragPosSliderMinMax = new Vector2(0f, 100f);
        [Min(0f)] public float DragSliderMax = 100f;
        [Min(0.01f)] public float DragRotSliderMaxAngle = 360f;
        [NonSerialized] public float DragSliderValue;
        public UnityEvent<float> OnDragSliderChanged;


        public bool SeeDragClampGizmos = true;
        public Color DragRailGizmosColor = new Color(0f, 1f, 1f, 0.9f);
        public Color DragLimitGizmosColor = new Color(1f, 0.6f, 0f, 1f);
        public Color DragCurrentGizmosColor = Color.yellow;
        public float DragGizmoSphereRadius = 0.035f;
        public float DragGizmoCurrentRadius = 0.01f;
        public bool SeeDragDistanceGizmo = true;
        public Color DragDistanceEdgeColor = new Color(0.2f, 0.6f, 1f, 1f);

        public bool DragRotationFillArc = false;
        public Color DragRotationArcColor = new Color(0f, 1f, 1f, 0.9f);
        public Color DragRotationLimitColor = new Color(1f, 0.6f, 0f, 1f);
        public Color DragRotationCurrentColor = Color.yellow;

        #endregion

        #region Drag Private
        private bool dragLocked = false;

        private float JointProjectionDistance = 0.005f;
        private float JointProjectionAngle = 1f;

        private bool railWorldLocked;
        private bool _rbConstraintsSaved;
        private bool _dragRotHasStartTheta;
        private bool _dragReturnPending;
        private bool _dragReturningToStart;
        private bool _dragInvokeAfterReturn;
        private bool _dragUseHitPlace;
        private bool _dragHasHitPivot;
        private float DragRotGizmoRadiusScale = 1;
        private float DragRotationGizmoRadius = 0.5f;
        private float thetaAccum;
        private float _dragRotStartTheta;
        private float _dragHitThetaRawPrev;
        private float thetaUnwrapped;
        private float _dragHitDeltaAccum;
        private float _dragHitThetaStart;
        private float sTarget;
        private float _thetaTargetSmoothed;
        [NonSerialized] private float lastSliderValue = float.NaN;
        [Min(0f)] private float DragRotSleepVel = 0.03f;
        private float RotDirSign => (DragRotationDirection == DragRotDirection.ClockWise) ? -1f : +1f;
        private int _limitLatch = 0;
        private Vector3 startLocal;
        private Vector3 axisN;
        private Vector3 railOriginWorld;
        private Vector3 railAxisWorld;
        private Vector3 rotRefDirWorld;
        private Vector3 rotRefLocal;
        private Vector3 _dragHitPivotLocal;
        private Vector3 desiredLastWorld;
        private Transform railSpace;
        private Transform _dragHitPivotSpace;
        private RigidbodyConstraints _rbConstraintsBackup;
        private ConfigurableJoint railJoint;
        #endregion

        #region Drag Constants

        private const float ROT_LIMIT_EPS = 0.25f;
        private const float EDGE_EPS_NORM = 1e-3f;

        #endregion

        #region Public Methods
        public void DragBegin()
        {
            if (dragLocked) return;

            _dragUseHitPlace = false;
            _dragHasHitPivot = false;
            _dragHitPivotSpace = null;
            _dragHitDeltaAccum = 0f;

            SetupRailJoint();

            if (!interactableRb) AssignRB();

            if (transform.parent != null)
            {
                railWorldLocked = false;
                railSpace = transform.parent;
                axisN = ComputeAxisLocal();
            }
            else
            {
                railWorldLocked = true;
                railSpace = null;
                railAxisWorld = GetAxisWorldFromMoveAxis().normalized;
            }

            isDragging = true;

            _dragReturnPending = false;
            _dragReturningToStart = false;
            _dragInvokeAfterReturn = false;

            if (DragType == DragType.Rotation)
            {
                Vector3 axisW = railWorldLocked ? railAxisWorld : railSpace.TransformDirection(axisN).normalized;
                Vector3 hingeW = railWorldLocked ? railOriginWorld : railSpace.TransformPoint(startLocal);

                Vector3 r0 = _camT.position;
                Vector3 b = _camT.forward.normalized;
                Vector3 n = axisW;
                float denom = Vector3.Dot(n, b);
                Vector3 p;
                if (Mathf.Abs(denom) < 1e-5f)
                {
                    p = hingeW + Vector3.ProjectOnPlane(r0 - hingeW, n);
                }
                else
                {
                    float t = Vector3.Dot(n, hingeW - r0) / denom;
                    p = r0 + b * t;
                }

                desiredLastWorld = Vector3.ProjectOnPlane(p - hingeW, axisW);
                if (desiredLastWorld.sqrMagnitude < 1e-6f) desiredLastWorld = rotRefDirWorld;
                desiredLastWorld.Normalize();

                _thetaTargetSmoothed = thetaAccum;
            }
        }

        public void DragEnd()
        {
            isDragging = false;

            _dragUseHitPlace = false;
            _dragHasHitPivot = false;
            _dragHitDeltaAccum = 0f;

            if (DragType == DragType.Rotation && (AlwaysReturnToStartPos || _dragReturnPending))
            {
                _dragReturnPending = false;
                StartReturnToStartRotationInternal(true);
            }
        }

        public void SetDragLocked(bool isLocked) => dragLocked = isLocked;
        public float GetSliderValue() => DragSliderValue;
        public void GetSliderValue(out float sliderValue) => sliderValue = DragSliderValue;

        /// <summary>
        /// For Drag Rotation Only! Smoothly returns the object back to its start rotation.
        /// If called while dragging, it will return as soon as the drag ends.
        /// </summary>
        public void DragReturnToStartRotation()
        {
            if (interactMode != InteractMode.Drag) return;
            if (DragType != DragType.Rotation) return;

            if (isDragging)
            {
                _dragReturnPending = true;
                return;
            }

            StartReturnToStartRotationInternal(true);
        }

        #endregion
        #region Aux Methods

        bool TryGetLocalBounds(out Bounds localB)
        {
            var mf = GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh)
            {
                localB = mf.sharedMesh.bounds;
                return true;
            }

            var bc = GetComponent<BoxCollider>();
            if (bc)
            {
                localB = new Bounds(bc.center, bc.size);
                return true;
            }

            var r = GetComponent<Renderer>();
            if (r)
            {
                var wb = r.bounds;

                Vector3[] wc = new Vector3[8];
                var min = wb.min; var max = wb.max;
                wc[0] = new Vector3(min.x, min.y, min.z); wc[1] = new Vector3(max.x, min.y, min.z);
                wc[2] = new Vector3(min.x, max.y, min.z); wc[3] = new Vector3(max.x, max.y, min.z);
                wc[4] = new Vector3(min.x, min.y, max.z); wc[5] = new Vector3(max.x, min.y, max.z);
                wc[6] = new Vector3(min.x, max.y, max.z); wc[7] = new Vector3(max.x, max.y, max.z);

                var lc0 = transform.InverseTransformPoint(wc[0]);
                localB = new Bounds(lc0, Vector3.zero);
                for (int i = 1; i < 8; i++)
                    localB.Encapsulate(transform.InverseTransformPoint(wc[i]));
                return true;
            }

            localB = default;
            return false;
        }

        bool Diff(ref Vector3 a, Vector3 b, float eps = 1e-8f)
        {
            if ((a - b).sqrMagnitude > eps) { a = b; return true; }
            return false;
        }

        float ComputeAutoRotGizmoRadius(Vector3 axisWorld, Vector3 hingeWorld)
        {
            float maxD = 0f;
            bool found = false;

            var mfs = GetComponentsInChildren<MeshFilter>(false);
            foreach (var mf in mfs)
            {
                var mesh = mf.sharedMesh;
                if (!mesh) continue;

                var verts = mesh.vertices;
                var t = mf.transform;

                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 w = t.TransformPoint(verts[i]);
                    float d = Vector3.ProjectOnPlane(w - hingeWorld, axisWorld).magnitude;
                    if (d > maxD) maxD = d;
                }
                found = true;
            }

            if (!found && TryGetLocalBounds(out var lb))
            {
                Vector3 c = lb.center;
                Vector3 e = lb.extents;

                Vector3[] lc =
                {
                    c + new Vector3(+e.x, 0, 0), c + new Vector3(-e.x, 0, 0),
                    c + new Vector3(0, +e.y, 0), c + new Vector3(0, -e.y, 0),
                    c + new Vector3(0, 0, +e.z), c + new Vector3(0, 0, -e.z),
                };

                foreach (var p in lc)
                {
                    Vector3 w = transform.TransformPoint(p);
                    float d = Vector3.ProjectOnPlane(w - hingeWorld, axisWorld).magnitude;
                    if (d > maxD) maxD = d;
                }
                found = true;
            }

            float r = found ? maxD : DragRotationGizmoRadius;
            return r * DragRotGizmoRadiusScale;
        }

        float ComputeSClosestToCameraRay(out Vector3 axisWorldNorm, out Vector3 originWorld)
        {
            if (railWorldLocked)
            {
                axisWorldNorm = railAxisWorld;
                originWorld = railOriginWorld;
            }
            else
            {
                axisN = ComputeAxisLocal();
                axisWorldNorm = railSpace.TransformDirection(axisN).normalized;
                originWorld = railSpace.TransformPoint(startLocal);
            }

            Vector3 a = axisWorldNorm;
            Vector3 r0 = _cam.transform.position;
            Vector3 b = _cam.transform.forward.normalized;
            Vector3 w0 = originWorld - r0;

            float B = Vector3.Dot(a, b);
            float D = Vector3.Dot(a, w0);
            float E = Vector3.Dot(b, w0);
            float denom = 1f - (B * B);

            if (denom < 1e-4f) return -D;
            return (B * E - D) / denom;
        }

        float ResolveRotationRadius(Vector3 axisWorld, Vector3 hingeWorld)
        {
            if (AutomaticRotationRadius)
            {
                float r = ComputeAutoRotGizmoRadius(axisWorld, hingeWorld);

                DragRotationGizmoRadius = r;
                return Mathf.Max(0.01f, r);
            }

            DragRotationGizmoRadius = RotationRadius;
            return Mathf.Max(0.01f, RotationRadius);
        }

        float AxisWorldPerLocal()
        {
            if (railWorldLocked || railSpace == null) return 1f;
            return railSpace.TransformVector(axisN).magnitude;
        }

        float QuantizeToSteps(float v, float min, float max, int steps)
        {
            if (steps <= 1) return Mathf.Clamp(v, min, max);
            if (Mathf.Approximately(min, max)) return min;
            var t = Mathf.InverseLerp(min, max, v);
            var k = Mathf.Round(t * (steps - 1));
            return Mathf.Lerp(min, max, steps <= 1 ? 0f : k / (steps - 1));
        }

        [HideInInspector] public Vector3 ComputeDragUIWorldOnArc()
        {
            if (DragType != DragType.Rotation || _camT == null)
                return WillOverrideAnchor ? transform.TransformPoint(LocalPositionNewAnchor)
                                          : (interactableRb ? interactableRb.position : transform.position);

            Vector3 axisW = railWorldLocked ? railAxisWorld : (railSpace ? railSpace.TransformDirection(axisN).normalized
                                                                          : GetAxisWorldFromMoveAxis().normalized);
            Vector3 hingeW = railWorldLocked ? railOriginWorld : (railSpace ? railSpace.TransformPoint(startLocal)
                                                                             : transform.position);

            float R = ResolveRotationRadius(axisW, hingeW);

            Vector3 desired = DesiredDirFromRayPlaneHit(axisW, hingeW);

            float gMin = DragAngleClampMinMax.x;
            float gMax = DragAngleClampMinMax.y;
            OrderMinMax(ref gMin, ref gMax);

            float thetaRaw = RotDirSign * Vector3.SignedAngle(rotRefDirWorld, desired, axisW);
            float thetaT = thetaUnwrapped + Mathf.DeltaAngle(thetaUnwrapped, thetaRaw);
            if (DragRotIsToClamp) thetaT = Mathf.Clamp(thetaT, gMin, gMax);

            float thetaTPhys = RotDirSign * thetaT;

            Vector3 dirOnArc = Quaternion.AngleAxis(thetaTPhys, axisW) * rotRefDirWorld;
            return hingeW + Vector3.ProjectOnPlane(dirOnArc, axisW).normalized * Mathf.Max(0.01f, R);
        }

        [HideInInspector] public Vector3 DragGetAnchorWorldNow()
        {
            return WillOverrideAnchor
                ? transform.TransformPoint(LocalPositionNewAnchor)
                : (interactableRb ? interactableRb.position : transform.position);
        }

        [HideInInspector] public Vector3 DragGetOriginWorld()
        {
            if (railWorldLocked) return railOriginWorld;
            Transform space = (transform.parent != null) ? transform.parent : transform;
            return space.TransformPoint(startLocal);
        }
        
        Vector3 ComputeAxisLocal()
        {
            var space = railSpace != null ? railSpace : transform;
            var axisWorld = GetAxisWorldFromMoveAxis();
            return space.InverseTransformDirection(axisWorld).normalized;
        }

        Vector3 ComputeRotRefDirWorld(Vector3 axisWorld)
        {
            Vector3 refLocal;
            switch (MoveAxis)
            {
                case MoveAxis.UpAndDown: refLocal = !invertDirection ? Vector3.right : -Vector3.right; break;
                case MoveAxis.LeftRight: refLocal = !invertDirection ? Vector3.forward : -Vector3.forward; break;
                case MoveAxis.ForwardBack: refLocal = !invertDirection ? Vector3.right : -Vector3.right; break;
                default: refLocal = !invertDirection ? Vector3.right : -Vector3.right; break;
            }

            Vector3 refWorld = transform.TransformDirection(refLocal);
            refWorld = Vector3.ProjectOnPlane(refWorld, axisWorld);
            if (refWorld.sqrMagnitude < 1e-6f)
            {
                refWorld = Vector3.ProjectOnPlane(transform.right, axisWorld);
                if (refWorld.sqrMagnitude < 1e-6f)
                    refWorld = Vector3.ProjectOnPlane(transform.forward, axisWorld);
            }
            return refWorld.normalized;
        }

        Vector3 DesiredDirFromRayClosestOnPlane(Vector3 axisWorld, Vector3 hingeWorld)
        {
            Vector3 n = axisWorld.normalized;
            Vector3 r0 = _camT.transform.position;
            Vector3 b = _camT.transform.forward;

            Vector3 r0p = r0 - n * Vector3.Dot(n, r0 - hingeWorld);
            Vector3 bp = b - n * Vector3.Dot(n, b);

            if (bp.sqrMagnitude < 1e-6f)
                return (r0p - hingeWorld).normalized;

            float t = Vector3.Dot(hingeWorld - r0p, bp) / bp.sqrMagnitude;
            Vector3 pClosest = r0p + bp * t;

            Vector3 desired = pClosest - hingeWorld;
            if (desired.sqrMagnitude < 1e-6f) desired = Vector3.ProjectOnPlane(_cam.transform.right, n);
            return desired.normalized;
        }

        Vector3 DesiredDirFromRayPlaneHit(Vector3 axisWorld, Vector3 hingeWorld)
        {
            Vector3 n = axisWorld.normalized;
            Vector3 r0 = _camT.transform.position;
            Vector3 b = _camT.transform.forward;

            float denom = Vector3.Dot(n, b);
            if (Mathf.Abs(denom) < 1e-5f)
            {
                return DesiredDirFromRayClosestOnPlane(axisWorld, hingeWorld);
            }

            float t = Vector3.Dot(n, hingeWorld - r0) / denom;
            Vector3 p = r0 + b * t;

            Vector3 desired = Vector3.ProjectOnPlane(p - hingeWorld, axisWorld);
            if (desired.sqrMagnitude < 1e-6f) desired = rotRefDirWorld;
            return desired.normalized;
        }

        Vector3 ComputeAutoRotRefDirWorld(Vector3 axisWorld, Vector3 hingeWorld, out Vector3 refLocalOut)
        {
            bool TryProject(out Vector3 bestWorld, out Vector3 bestLocal)
            {
                bestWorld = Vector3.zero; bestLocal = Vector3.right;
                if (!TryGetLocalBounds(out var lb)) return false;

                Vector3 c = lb.center, e = lb.extents;
                Vector3[] cornersL = new Vector3[]
                {
                    c + new Vector3(+e.x,+e.y,+e.z),
                    c + new Vector3(+e.x,+e.y,-e.z),
                    c + new Vector3(+e.x,-e.y,+e.z),
                    c + new Vector3(+e.x,-e.y,-e.z),
                    c + new Vector3(-e.x,+e.y,+e.z),
                    c + new Vector3(-e.x,+e.y,-e.z),
                    c + new Vector3(-e.x,-e.y,+e.z),
                    c + new Vector3(-e.x,-e.y,-e.z),
                };

                float best = -1f;
                for (int i = 0; i < cornersL.Length; i++)
                {
                    Vector3 w = transform.TransformPoint(cornersL[i]);
                    Vector3 v = w - hingeWorld;
                    Vector3 vp = Vector3.ProjectOnPlane(v, axisWorld);
                    float d = vp.sqrMagnitude;
                    if (d > best)
                    {
                        best = d;
                        bestWorld = vp.normalized;
                    }
                }

                bestLocal = transform.InverseTransformDirection(bestWorld);
                return best > 0f;
            }

            if (!TryProject(out var refW, out var refL))
            {
                refW = ComputeRotRefDirWorld(axisWorld);
                refL = transform.InverseTransformDirection(refW);
            }

            refLocalOut = refL;
            return refW;
        }

        Vector3 GetAxisWorldFromMoveAxis()
        {
            switch (MoveAxis)
            {
                case MoveAxis.ForwardBack: return !invertDirection ? transform.forward : -transform.forward;
                case MoveAxis.LeftRight: return !invertDirection ? transform.right : -transform.right;
                case MoveAxis.UpAndDown: return !invertDirection ? transform.up : -transform.up;
                default: return !invertDirection ? transform.forward : -transform.forward;
            }
        }

        #endregion

        #region Helper Methods

        [HideInInspector] public void BeginDragAtHit(Vector3 hitPointWorld, Transform hitPivotSpace = null)
        {
            DragBegin();

            if (!isDragging) return;

            _dragHasHitPivot = true;

            _dragHitPivotSpace = hitPivotSpace != null
                ? hitPivotSpace
                : (interactableRb ? interactableRb.transform : transform);

            bool isHitPlaceRot = ConsiderHitPlace && DragType == DragType.Rotation;

            Vector3 axisW = default;
            Vector3 hingeW = default;

            Vector3 hitWorldForPivot = hitPointWorld;

            if (isHitPlaceRot)
            {
                axisW = railWorldLocked ? railAxisWorld : railSpace.TransformDirection(axisN).normalized;
                hingeW = railWorldLocked ? railOriginWorld : railSpace.TransformPoint(startLocal);

                if (_camT != null)
                {
                    Vector3 r0 = _camT.position;
                    Vector3 b = hitPointWorld - r0;

                    if (b.sqrMagnitude > 1e-6f)
                    {
                        b.Normalize();

                        float denom = Vector3.Dot(axisW, b);

                        if (Mathf.Abs(denom) < 1e-6f)
                        {
                            hitWorldForPivot = hingeW + Vector3.ProjectOnPlane(r0 - hingeW, axisW);
                        }
                        else
                        {
                            float t = Vector3.Dot(axisW, hingeW - r0) / denom;
                            hitWorldForPivot = r0 + b * t;
                        }
                    }
                    else
                    {
                        hitWorldForPivot = hingeW + Vector3.ProjectOnPlane(hitPointWorld - hingeW, axisW);
                    }
                }
                else
                {
                    hitWorldForPivot = hingeW + Vector3.ProjectOnPlane(hitPointWorld - hingeW, axisW);
                }
            }

            _dragHitPivotLocal = _dragHitPivotSpace.InverseTransformPoint(hitWorldForPivot);

            if (!isHitPlaceRot) return;

            Vector3 v = Vector3.ProjectOnPlane(hitWorldForPivot - hingeW, axisW);
            if (v.sqrMagnitude < 1e-6f)
            {
                _dragUseHitPlace = false;
                return;
            }

            v.Normalize();
            desiredLastWorld = v;

            _dragUseHitPlace = true;

            float thetaRawStart = RotDirSign * Vector3.SignedAngle(rotRefDirWorld, desiredLastWorld, axisW);
            _dragHitThetaRawPrev = thetaRawStart;
            _dragHitDeltaAccum = 0f;
            _dragHitThetaStart = thetaUnwrapped;

            _thetaTargetSmoothed = thetaAccum;
        }

        [HideInInspector] public bool TryGetDragHitPivot(out Vector3 world)
        {
            if (!_dragHasHitPivot)
            {
                world = default;
                return false;
            }

            Transform space = _dragHitPivotSpace != null
                ? _dragHitPivotSpace
                : (interactableRb ? interactableRb.transform : transform);

            world = space.TransformPoint(_dragHitPivotLocal);
            return true;
        }

        [HideInInspector] public void UpdateRailJointRuntime(Vector3 axisWorld)
        {
            if (railJoint == null) return;

            Vector3 originWorldForJoint = railWorldLocked
                                        ? railOriginWorld
                                        : railSpace.TransformPoint(startLocal);

            var ca = railJoint.connectedAnchor;
            if (Diff(ref ca, originWorldForJoint, 1e-6f))
                railJoint.connectedAnchor = ca;

            Vector3 anchorWorld = WillOverrideAnchor
                                ? transform.TransformPoint(LocalPositionNewAnchor)
                                : (interactableRb ? interactableRb.position : transform.position);

            var wantAnchorLocal = transform.InverseTransformPoint(anchorWorld);
            var an = railJoint.anchor;
            if (Diff(ref an, wantAnchorLocal, 1e-6f))
                railJoint.anchor = an;

            if (DragType == DragType.Position)
            {
                Vector3 axisLocal = transform.InverseTransformDirection(axisWorld).normalized;
                Vector3 secLocal = Vector3.Cross(axisLocal, Vector3.up);
                if (secLocal.sqrMagnitude < 1e-4f) secLocal = Vector3.Cross(axisLocal, Vector3.right);
                secLocal.Normalize();

                var ax = railJoint.axis;
                if (Diff(ref ax, axisLocal, 1e-6f)) railJoint.axis = ax;

                var sx = railJoint.secondaryAxis;
                if (Diff(ref sx, secLocal, 1e-6f)) railJoint.secondaryAxis = sx;
            }
            else if (DragType == DragType.Rotation && railJoint)
            {
                float jMin = DragAngleClampMinMax.x * RotDirSign;
                float jMax = DragAngleClampMinMax.y * RotDirSign;
                OrderMinMax(ref jMin, ref jMax);

                bool jointCanLimit = DragRotIsToClamp &&
                                     jMin >= -180f && jMax <= 180f &&
                                    (jMax - jMin) <= 180f;

                railJoint.angularXMotion = jointCanLimit ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;

                if (jointCanLimit)
                {
                    var spring = railJoint.angularXLimitSpring;
                    spring.spring = DragRotLimitSpring;
                    spring.damper = DragRotLimitDamping;
                    railJoint.angularXLimitSpring = spring;

                    var low = railJoint.lowAngularXLimit; low.limit = jMin; railJoint.lowAngularXLimit = low;
                    var high = railJoint.highAngularXLimit; high.limit = jMax; railJoint.highAngularXLimit = high;
                }
            }
        }
        
        private void OrderMinMax(ref float a, ref float b) { if (a > b) (a, b) = (b, a); }
        
        private void SetupRailJoint()
        {
            if (railJoint || interactableRb == null) return;

            railJoint = gameObject.GetComponent<ConfigurableJoint>();
            if (!railJoint) railJoint = gameObject.AddComponent<ConfigurableJoint>();

            railJoint.connectedBody = null;
            railJoint.autoConfigureConnectedAnchor = false;

            Vector3 originWorld = railWorldLocked
                ? railOriginWorld
                : (railSpace != null ? railSpace.TransformPoint(startLocal) : transform.position);
            railJoint.connectedAnchor = originWorld;

            Vector3 anchorWorld = WillOverrideAnchor
                ? transform.TransformPoint(LocalPositionNewAnchor)
                : (interactableRb ? interactableRb.position : transform.position);
            railJoint.anchor = transform.InverseTransformPoint(anchorWorld);

            Vector3 axisW = railWorldLocked
                ? railAxisWorld
                : (railSpace != null ? railSpace.TransformDirection(axisN).normalized : GetAxisWorldFromMoveAxis().normalized);

            Vector3 axisLocal = transform.InverseTransformDirection(axisW).normalized;
            Vector3 secLocal = Vector3.Cross(axisLocal, Vector3.up);
            if (secLocal.sqrMagnitude < 1e-4f) secLocal = Vector3.Cross(axisLocal, Vector3.right);
            secLocal.Normalize();

            railJoint.axis = axisLocal;
            railJoint.secondaryAxis = secLocal;

            if (DragType == DragType.Position)
            {
                railJoint.xMotion = ConfigurableJointMotion.Free;
                railJoint.yMotion = ConfigurableJointMotion.Locked;
                railJoint.zMotion = ConfigurableJointMotion.Locked;

                railJoint.angularXMotion = ConfigurableJointMotion.Locked;
                railJoint.angularYMotion = ConfigurableJointMotion.Locked;
                railJoint.angularZMotion = ConfigurableJointMotion.Locked;

                JointDrive yz = new JointDrive { positionSpring = DragSpring, positionDamper = DragDamping, maximumForce = float.MaxValue };
                railJoint.yDrive = yz;
                railJoint.zDrive = yz;
            }
            else
            {
                railJoint.xMotion = ConfigurableJointMotion.Locked;
                railJoint.yMotion = ConfigurableJointMotion.Locked;
                railJoint.zMotion = ConfigurableJointMotion.Locked;

                railJoint.angularYMotion = ConfigurableJointMotion.Locked;
                railJoint.angularZMotion = ConfigurableJointMotion.Locked;

                bool jointCanLimit =
                    DragRotIsToClamp &&
                    DragAngleClampMinMax.x >= -180f &&
                    DragAngleClampMinMax.y <= 180f;

                railJoint.angularXMotion = jointCanLimit
                    ? ConfigurableJointMotion.Limited
                    : ConfigurableJointMotion.Free;

                if (jointCanLimit)
                {
                    railJoint.angularXLimitSpring = new SoftJointLimitSpring
                    { spring = DragRotLimitSpring, damper = DragRotLimitDamping };

                    railJoint.lowAngularXLimit = new SoftJointLimit { limit = DragAngleClampMinMax.x };
                    railJoint.highAngularXLimit = new SoftJointLimit { limit = DragAngleClampMinMax.y };
                }

                railJoint.projectionMode = JointProjectionMode.PositionAndRotation;
                railJoint.projectionDistance = JointProjectionDistance;
                railJoint.projectionAngle = JointProjectionAngle;
            }
        }

        private void ApplyRotOffset(Vector3 axisWorld)
        {
            if (Mathf.Approximately(DragRotOffsetDeg, 0f)) return;

            float off = RotDirSign * DragRotOffsetDeg;

            Quaternion qW = Quaternion.AngleAxis(off, axisWorld);
            rotRefDirWorld = (qW * rotRefDirWorld).normalized;

            Vector3 axisLocal = transform.InverseTransformDirection(axisWorld).normalized;
            Quaternion qL = Quaternion.AngleAxis(off, axisLocal);
            rotRefLocal = (qL * rotRefLocal).normalized;
        }

        private void TryApplyRotStartState(Vector3 axisW, Vector3 hingeW)
        {
            if (!UseRotStartPosition) return;

            if (!(DragRotIsToClamp || DragUseSlider)) return;

            float min, max;
            if (DragRotIsToClamp) { min = DragAngleClampMinMax.x; max = DragAngleClampMinMax.y; }
            else { min = 0f; max = Mathf.Max(0f, DragRotSliderMaxAngle); }
            OrderMinMax(ref min, ref max);

            float targetDeg = (rotClampStartType == RotClampStartType.Percentage)
                ? Mathf.Lerp(min, max, Mathf.Clamp01(startPercentage / 100f))
                : Mathf.Clamp(startDegrees, min, max);

            Vector3 curDirW = Vector3.ProjectOnPlane(transform.TransformDirection(rotRefLocal), axisW).normalized;
            Vector3 desiredDirW = Quaternion.AngleAxis(RotDirSign * targetDeg, axisW) * rotRefDirWorld;
            desiredDirW = Vector3.ProjectOnPlane(desiredDirW, axisW).normalized;

            Quaternion dq = Quaternion.FromToRotation(curDirW, desiredDirW);

            if (interactableRb != null)
            {
                interactableRb.position = hingeW + dq * (interactableRb.position - hingeW);
                interactableRb.rotation = dq * interactableRb.rotation;
                interactableRb.angularVelocity = Vector3.zero;
            }
            else
            {
                transform.position = hingeW + dq * (transform.position - hingeW);
                transform.rotation = dq * transform.rotation;
            }

            thetaAccum = targetDeg;
            thetaUnwrapped = targetDeg;
            _thetaTargetSmoothed = targetDeg;
        }

        private void StartReturnToStartRotationInternal(bool invokeEvents)
        {
            if (dragLocked) return;
            if (interactMode != InteractMode.Drag) return;
            if (DragType != DragType.Rotation) return;
            if (interactableRb == null) return;

            if (!_dragRotHasStartTheta)
            {
                _dragRotStartTheta = thetaAccum;
                _dragRotHasStartTheta = true;
            }

            if (!_dragReturningToStart)
            {
                if (invokeEvents) BeforeReturnToPos?.Invoke();
                _dragInvokeAfterReturn = invokeEvents;
            }
            else
            {
                _dragInvokeAfterReturn |= invokeEvents;
            }

            _dragReturningToStart = true;
        }

        #endregion
    }
}