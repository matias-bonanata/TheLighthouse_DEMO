using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace FastStudios
{
    public partial class Interactable : MonoBehaviour
    {
        #region Variables

        #region GeneralVariables
        public bool CanInteract => canInteract;
        [Tooltip("While player are interacting with this object, does the player can interact with others?")] public bool CanInteractWithOthers = false;
        public bool AllowJustSpecificInteractions = false;
        public List<Interactable> SpecificOthersInteractions;
        [Tooltip("While player are interacting with this object, does the UI of other objects should be shown?")] public bool CanShowOthersUI = false;
        public bool AllowJustSpecificUIToShow = false;
        public List<Interactable> SpecificOthersUI;

        public bool WillOverrideAnchor;
        public Vector3 LocalPositionNewAnchor;
        public bool SeeGizmos = true;
        public bool SeeGizmosOnlySelected = true;
        public bool OverrideInteractionKey;
        public PressEInputBind NewInteraction = new PressEInputBind()
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.E,
            MouseButton = MouseMethod.Left,
        };


        #region Conditions
        public bool UseConditions = false;
        public ConditionGroup Conditions;
        public UnityEvent OnConditionAccept;
        public UnityEvent OnConditionDecline;

        #endregion

        #endregion

        #region Main
        public InteractMode interactMode = InteractMode.UnityEvent;
        #endregion

        #region UI

        #region Screen Space
        public bool ScreenSpacePrompt;
        public Vector2 ScreenSpaceOffset;
        public bool overrideScreenSpacePrefab;
        public GameObject ScreenSpacePrefab;
        #endregion

        #region World Prompt

        public bool WorldSpacePrompt;
        public bool AlignWorldSpaceToAnchor = false;
        public Vector2 WorldSpaceOffset;

        public bool overrideWorldSpacePrefab;
        public GameObject WorldSpacePrefab;

        [Tooltip("When enabled, you will be able to see more than one prompt at the screen. When disabled, this will override the current not additional prompt")]
        public bool AdditionalWorldPrompt = false;
        public int WorldPromptLayer = 0;
        public bool WorldSize = true;
        [Min(0.01f)] public float ReferencePromptDistance = 3f;
        public bool ReferencePrompHasScaleMinMax = false;
        public Vector2 PromptScaleMinMax = new Vector2(0.75f, 1.75f);
        public bool WorldUIOverrideAnchor = false;
        public Vector3 LocalPositionWorldUIAnchor;

        public bool OverrideWorldPromptMessage;
        public string WorldPromptMessage;
        public bool HasDeclinedConditionMessage;
        public string DeclinedWorldPromptMessage;

        #endregion

        #region Grab UI
        public bool GrabUIEnabled = false;
        public bool GrabUIControlSprite = false;
        public Sprite GrabUISprite;
        public bool GrabUIControlColor = false;
        public Color GrabUIColor = Color.white;
        public bool GrabUIControlSize = false;
        public Vector2 GrabUISize = new Vector2(64, 64);

        public bool overrideGrabUIPrefab = false;
        public GameObject GrabUIPrefab;

        #endregion

        #region Drag UI
        public bool DragUIEnabled = false;
        public bool DragUIControlSprite = false;
        public Sprite DragUISprite;
        public bool DragUIControlColor = false;
        public Color DragUIColor = Color.white;
        public bool DragUIControlSize = false;
        public Vector2 DragUISize = new Vector2(64, 64);
        public bool DragUIOverrideAnchor = false;
        public Vector3 LocalPositionDragUIAnchor;
        public bool DragUIOverrideOnArc = false;
        public Vector2 DragUIOverrideScreenOffset = Vector2.zero;

        public bool overrideDragUIPrefab = false;
        public GameObject DragUIPrefab;

        #endregion

        #region Hold UI

        public bool HoldUIEnabled = false;
        public bool ActAsWorldPrompt;
        public bool DragHasSlider;
        public bool applySliderValue;

        public bool HoldUIOverrideAnchor = false;
        public Vector3 LocalPositionHoldUIAnchor;

        public bool overrideHoldPrefab = false;
        public GameObject HoldUIPrefab;

        #endregion
        #endregion

        #region Events
        public bool AddOnInteractEvent;
        public bool AddEndEvent;
        public bool OnRayCastEnterAndExit;
        public UnityEvent onInteract;
        public UnityEvent onInteractEnd;
        public UnityEvent onRaycastEnter;
        public UnityEvent onRaycastExit;

        public bool hasOnSensorExit;
        public UnityEvent onSensorExit;

        public bool hasOnConditionAttended;
        public UnityEvent onConditionAttended;

        public bool hasGrabReturnToPosEvent;
        public UnityEvent BeforeReturnToPos;
        public UnityEvent AfterReturnToPos;

        #endregion

        #region Settings
        public bool hasMaxInteractions;
        public int maxInteractions;
        [HideInInspector] public int interactionTimes;
        public bool HasSensor;
        public Vector3 SensorOffset;
        [Tooltip("For more performance, use Radius (Distance uses Vector3.Distance)")] public SensorType SensorType = SensorType.Radius;
        public float SensorRadius = 5;
        public float SensorDistance = 5;
        public bool OnlyShowUI;
        public bool DrawRadiusInEditor = true;
        public bool HasAutoInteract;
        #endregion
        
        #region Privates
        private bool canInteract = true;
        private bool SmoothRigidbodyPhysics = true;
        private bool sensorEventBool = false;
        private bool wasInside = false;
        Camera _cam;
        Transform _camT;
        private InteractionManager singleton;
        [HideInInspector] public bool isOverridingAnchorPosition;
        [HideInInspector] public bool isOverridingWorldUIAnchorPosition;
        [HideInInspector] public bool isOverridingDragUIAnchorPosition;
        [HideInInspector] public bool isOverridingHoldUIAnchorPosition;
        [HideInInspector] public bool wasKinematic;
        [HideInInspector] public bool JustGotIt;
        [HideInInspector] public int lastSelectedTab;
        [HideInInspector] public Rigidbody interactableRb;
        [HideInInspector] public GrabDeposit grabDeposit;
        [HideInInspector] public Collider Collider;

        #endregion

        #endregion

        #region Default Methods
        void Awake()
        {
            AssignRB();

            throwForceBackup = throwForce;
            Collider = gameObject.GetNoMatterWhat<Collider>(true);

            if (interactMode == InteractMode.Drag)
            {
                Vector3 originWorld = WillOverrideAnchor
                        ? transform.TransformPoint(LocalPositionNewAnchor)
                        : (interactableRb ? interactableRb.position : transform.position);

                if (transform.parent != null)
                {
                    railWorldLocked = false;
                    railSpace = transform.parent;
                    axisN = ComputeAxisLocal();
                    startLocal = railSpace.InverseTransformPoint(originWorld);
                }
                else
                {
                    railWorldLocked = true;
                    railSpace = null;
                    railAxisWorld = GetAxisWorldFromMoveAxis().normalized;
                    railOriginWorld = originWorld;
                }

                if (DragType == DragType.Rotation)
                {
                    Vector3 axisW = railWorldLocked
                                ? railAxisWorld
                                : railSpace.TransformDirection(axisN).normalized;

                    railAxisWorld = axisW;

                    Vector3 hingeW = railWorldLocked ? railOriginWorld : railSpace.TransformPoint(startLocal);
                    rotRefDirWorld = ComputeAutoRotRefDirWorld(axisW, hingeW, out rotRefLocal);

                    ApplyRotOffset(axisW);

                    Vector3 curDir0 = Vector3.ProjectOnPlane(transform.TransformDirection(rotRefLocal), axisW).normalized;
                    thetaAccum = RotDirSign * Vector3.SignedAngle(rotRefDirWorld, curDir0, axisW);
                    thetaUnwrapped = thetaAccum;

                    TryApplyRotStartState(axisW, hingeW);

                    _dragRotStartTheta = thetaAccum;
                    _dragRotHasStartTheta = true;
                }
                else
                {
#if UNITY_6000_0_OR_NEWER
                    interactableRb.linearDamping = DragRbDamping;
#elif UNITY_2022_3_OR_NEWER
                    interactableRb.drag = DragRbDamping;
#endif
                }

                SetupRailJoint();

                if (interactableRb == null)
                {
                    Debug.LogWarning($"[Press E] {gameObject} (Drag) dont have a required rigidbody. Adding automatically...");

                    Rigidbody rb = gameObject.AddComponent<Rigidbody>();

                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }

                if (interactableRb != null && !_rbConstraintsSaved)
                {
                    _rbConstraintsBackup = interactableRb.constraints;
                    _rbConstraintsSaved = true;
                }
            }

            else if (interactMode == InteractMode.Grab)
            {
                int thisLayer = gameObject.layer;

                for (int i = 0; i < 32; i++)
                {
                    if (!Physics.GetIgnoreLayerCollision(thisLayer, i))
                    {
                        throwableLayerMask |= 1 << i;
                    }
                }
            }
        }

        void Start()
        {
            if (singleton == null) singleton = InteractionManager.singleton;
            if (singleton.playerObject == null) singleton.GetPlayerObject();

            WarmupConditionBindings();
        }

        void OnEnable()
        {
            if (singleton == null) singleton = InteractionManager.singleton;
            
            _cam = singleton ? singleton.Cam : Camera.main;
            _camT = _cam ? _cam.transform : null;

            UniversalsRuntime.ApplyAllFor<Interactable>();
            UniversalsRuntime.ApplyLoadOnCreationForInstance(this);
        }

        void FixedUpdate()
        {
            if (interactMode == InteractMode.Drag)
            {
                if (dragLocked)
                {
                    if (interactableRb != null)
                    {
#if UNITY_6000_0_OR_NEWER
                        interactableRb.linearVelocity = Vector3.zero;
#elif UNITY_2022_3_OR_NEWER
                        interactableRb.velocity = Vector3.zero;
#endif
                        interactableRb.angularVelocity = Vector3.zero;

                        if (interactableRb.constraints != RigidbodyConstraints.FreezeAll)
                            interactableRb.constraints = RigidbodyConstraints.FreezeAll;
                    }

                    if (railJoint != null)
                    {
                        railJoint.xMotion = ConfigurableJointMotion.Locked;
                        railJoint.yMotion = ConfigurableJointMotion.Locked;
                        railJoint.zMotion = ConfigurableJointMotion.Locked;
                        railJoint.angularXMotion = ConfigurableJointMotion.Locked;
                        railJoint.angularYMotion = ConfigurableJointMotion.Locked;
                        railJoint.angularZMotion = ConfigurableJointMotion.Locked;
                    }

                    return;
                }

                if (interactableRb != null)
                {
                    if (DragType == DragType.Position)
                    {
                        var wanted = _rbConstraintsBackup | RigidbodyConstraints.FreezeRotation;
                        if (interactableRb.constraints != wanted)
                            interactableRb.constraints = wanted;

#if UNITY_6000_0_OR_NEWER
                        if (interactableRb.linearDamping != DragRbDamping) interactableRb.linearDamping = DragRbDamping;
#elif UNITY_2022_3_OR_NEWER
                        if (interactableRb.drag != DragRbDamping) interactableRb.drag = DragRbDamping;
#endif
                    }
                    else
                    {
                        if (interactableRb.constraints != _rbConstraintsBackup)
                            interactableRb.constraints = _rbConstraintsBackup;
                    }
                }
                else if (interactableRb != null && interactableRb.constraints != _rbConstraintsBackup)
                {
                    interactableRb.constraints = _rbConstraintsBackup;
                }

                if (!railWorldLocked && !railSpace)
                    railSpace = (transform.parent != null) ? transform.parent : null;

                Vector3 axisWorld = railWorldLocked
                    ? railAxisWorld
                    : railSpace.TransformDirection(axisN).normalized;

                UpdateRailJointRuntime(axisWorld);

                if (DragType == DragType.Position && interactableRb != null)
                {
                    if (interactableRb.useGravity)
                    {
                        Vector3 g = Physics.gravity;
                        Vector3 gPerp = g - Vector3.Dot(g, axisWorld) * axisWorld;
                        if (gPerp.sqrMagnitude > 0f) interactableRb.AddForce(-gPerp, ForceMode.Acceleration);
                    }

#if UNITY_6000_0_OR_NEWER
                    float vAxis = Vector3.Dot(interactableRb.linearVelocity, axisWorld);
                    interactableRb.linearVelocity = axisWorld * vAxis;
#elif UNITY_2022_3_OR_NEWER
                    float vAxis = Vector3.Dot(interactableRb.velocity, axisWorld);
                    interactableRb.velocity = axisWorld * vAxis;
#endif
                    interactableRb.angularVelocity = Vector3.zero;

                    Vector3 refWorldNow = WillOverrideAnchor
                        ? transform.TransformPoint(LocalPositionNewAnchor)
                        : interactableRb.position;

                    float sRaw, sNow;
                    if (railWorldLocked) sRaw = Vector3.Dot(refWorldNow - railOriginWorld, axisWorld);
                    else
                    {
                        Vector3 refLocalNow = railSpace.InverseTransformPoint(refWorldNow);
                        sRaw = Vector3.Dot(refLocalNow - startLocal, axisN);
                    }
                    sNow = sRaw;

                    bool hitMin = false, hitMax = false;
                    if (DragIsToClamp)
                    {
                        if (sNow < DragClampMinMax.x) { sNow = DragClampMinMax.x; hitMin = true; }
                        else if (sNow > DragClampMinMax.y) { sNow = DragClampMinMax.y; hitMax = true; }

                        if (DragUseSlider)
                        {
                            float sMin = DragClampMinMax.x;
                            float sMax = DragClampMinMax.y;
                            OrderMinMax(ref sMin, ref sMax);

                            float span = Mathf.Max(1e-4f, sMax - sMin);
                            float s = Mathf.Clamp(sNow, sMin, sMax);

                            float t01 = (s - sMin) / span;
                            t01 = Mathf.Clamp01(t01);

                            if (t01 <= EDGE_EPS_NORM) t01 = 0f;
                            else if (t01 >= 1f - EDGE_EPS_NORM) t01 = 1f;

                            float value = Mathf.Lerp(DragPosSliderMinMax.x, DragPosSliderMinMax.y, t01);

                            DragSliderValue = value;

                            if (!Mathf.Approximately(DragSliderValue, lastSliderValue) && OnDragSliderChanged != null)
                                OnDragSliderChanged.Invoke(DragSliderValue);

                            lastSliderValue = DragSliderValue;
                        }
                    }

                    if (DragIsToClamp && ((hitMax && vAxis > 0f) || (hitMin && vAxis < 0f)))
                    {
                        vAxis = -vAxis * DragBounciness;
#if UNITY_6000_0_OR_NEWER
                        interactableRb.linearVelocity = axisWorld * vAxis;
#elif UNITY_2022_3_OR_NEWER
                        interactableRb.velocity = axisWorld * vAxis;
#endif
                    }

                    if (DragIsToClamp && !isDragging)
                    {
                        float delta = sNow - sRaw;

                        if (Mathf.Abs(delta) > 1e-4f)
                        {
                            Vector3 newRefWorld;

                            if (railWorldLocked) newRefWorld = refWorldNow + axisWorld * delta;
                            else if (railSpace != null)
                            {
                                Vector3 refLocalNow = railSpace.InverseTransformPoint(refWorldNow);
                                refLocalNow += axisN * delta;
                                newRefWorld = railSpace.TransformPoint(refLocalNow);
                            }
                            else newRefWorld = refWorldNow;

                            Vector3 moveDelta = newRefWorld - refWorldNow;
                            interactableRb.MovePosition(interactableRb.position + moveDelta);
                        }
                    }

                    if (isDragging)
                    {
                        float sClosestW = ComputeSClosestToCameraRay(out axisWorld, out var _);
                        float k = AxisWorldPerLocal();

                        float sClosestLocal = sClosestW / Mathf.Max(1e-6f, k);
                        sTarget = DragIsToClamp
                            ? Mathf.Clamp(sClosestLocal, DragClampMinMax.x, DragClampMinMax.y)
                            : sClosestLocal;

                        if (DragUseSteps)
                        {
                            float sMin, sMax;
                            if (DragIsToClamp)
                            {
                                sMin = DragClampMinMax.x;
                                sMax = DragClampMinMax.y;
                                OrderMinMax(ref sMin, ref sMax);
                            }
                            else
                            {
                                if (!DragUseSlider) goto NOSNAP_POS;
                                sMin = DragPosSliderMinMax.x;
                                sMax = DragPosSliderMinMax.y;
                                OrderMinMax(ref sMin, ref sMax);
                            }

                            sTarget = QuantizeToSteps(sTarget, sMin, sMax, DragStepCount);
                        }
                    NOSNAP_POS:;

                        float errorLocal = sTarget - sNow;
                        float errWorld = errorLocal * k;
                        float kp = DragSpring, kd = DragDamping, dt = Time.fixedDeltaTime;
                        float denom = 1f + kd * dt + kp * dt * dt;
                        float acc = (kp * errWorld - kd * vAxis) / denom;
                        interactableRb.AddForce(axisWorld * acc, ForceMode.Acceleration);
                    }
                }
                else if (DragType == DragType.Rotation && interactableRb != null)
                {
                    float wAxis = Vector3.Dot(interactableRb.angularVelocity, axisWorld);

                    Vector3 curDir = Vector3.ProjectOnPlane(transform.TransformDirection(rotRefLocal), axisWorld).normalized;
                    float thetaMeas = RotDirSign * Vector3.SignedAngle(rotRefDirWorld, curDir, axisWorld);

                    thetaUnwrapped += Mathf.DeltaAngle(thetaUnwrapped, thetaMeas);

                    thetaAccum += Mathf.DeltaAngle(thetaAccum, thetaUnwrapped) * 0.4f;
                    float theta = thetaAccum;
                    float thUnwrap = thetaUnwrapped;

                    Vector3 hingeWorld = railWorldLocked ? railOriginWorld : railSpace.TransformPoint(startLocal);

                    if (DragUseSlider)
                    {
                        float t01;
                        if (DragRotIsToClamp)
                        {
                            float min = DragAngleClampMinMax.x;
                            float max = DragAngleClampMinMax.y;
                            OrderMinMax(ref min, ref max);

                            float span = Mathf.Max(1e-4f, max - min);
                            float th = Mathf.Clamp(thUnwrap, min, max);
                            t01 = Mathf.Clamp01((th - min) / span);
                        }
                        else
                        {
                            float span = Mathf.Max(1e-4f, DragRotSliderMaxAngle);
                            float th = Mathf.Clamp(thUnwrap, 0f, DragRotSliderMaxAngle);
                            t01 = th / span;
                        }

                        t01 = Mathf.Clamp01(t01);
                        if (t01 <= EDGE_EPS_NORM) t01 = 0f;
                        else if (t01 >= 1f - EDGE_EPS_NORM) t01 = 1f;

                        DragSliderValue = t01 * DragSliderMax;

                        if (!Mathf.Approximately(DragSliderValue, lastSliderValue) && OnDragSliderChanged != null)
                            OnDragSliderChanged.Invoke(DragSliderValue);

                        lastSliderValue = DragSliderValue;
                    }

                    if (DragRotIsToClamp)
                    {
                        float min = DragAngleClampMinMax.x;
                        float max = DragAngleClampMinMax.y;
                        OrderMinMax(ref min, ref max);

                        float over = 0f;
                        if (theta > max) over = theta - max;
                        else if (theta < min) over = theta - min;

                        if (Mathf.Abs(over) > 0f)
                        {
                            float wAxisNow = Vector3.Dot(interactableRb.angularVelocity, axisWorld);
                            float torqueWall = RotDirSign * (-(over * Mathf.Deg2Rad) * DragRotLimitSpring) - DragRotLimitDamping * wAxisNow;
                            interactableRb.AddTorque(axisWorld * torqueWall, ForceMode.Acceleration);
                        }

                        int side = 0;
                        if (thetaUnwrapped >= max - ROT_LIMIT_EPS) side = +1;
                        else if (thetaUnwrapped <= min + ROT_LIMIT_EPS) side = -1;

                        if (side != 0 && _limitLatch == 0)
                        {
                            float w = Vector3.Dot(interactableRb.angularVelocity, axisWorld);
                            float wd = w * RotDirSign;

                            if ((side == +1 && wd > 0f) || (side == -1 && wd < 0f))
                            {
                                interactableRb.angularVelocity = axisWorld * (-w * DragRotBounciness);
                                _limitLatch = side;
                            }
                        }

                        if (side == 0) _limitLatch = 0;
                    }

                    if (isDragging)
                    {
                        hingeWorld = railWorldLocked ? railOriginWorld : railSpace.TransformPoint(startLocal);
                        Vector3 desired = DesiredDirFromRayPlaneHit(axisWorld, hingeWorld);

                        float min = DragAngleClampMinMax.x;
                        float max = DragAngleClampMinMax.y;
                        OrderMinMax(ref min, ref max);

                        float thetaRaw = RotDirSign * Vector3.SignedAngle(rotRefDirWorld, desired, axisWorld);
                        float thetaTarget;

                        if (ConsiderHitPlace && _dragUseHitPlace)
                        {
                            float d = Mathf.DeltaAngle(_dragHitThetaRawPrev, thetaRaw);
                            _dragHitThetaRawPrev = thetaRaw;
                            _dragHitDeltaAccum += d;

                            thetaTarget = _dragHitThetaStart + _dragHitDeltaAccum;
                        }
                        else
                        {
                            thetaTarget = thUnwrap + Mathf.DeltaAngle(thUnwrap, thetaRaw);
                        }

                        if (DragRotIsToClamp)
                        {
                            OrderMinMax(ref min, ref max);
                            thetaTarget = Mathf.Clamp(thetaTarget, min, max);
                        }

                        if (DragUseSteps)
                        {
                            float aMin, aMax;
                            if (DragRotIsToClamp)
                            {
                                aMin = DragAngleClampMinMax.x; aMax = DragAngleClampMinMax.y;
                                OrderMinMax(ref aMin, ref aMax);
                            }
                            else
                            {
                                aMin = 0f; aMax = Mathf.Max(1e-4f, DragRotSliderMaxAngle);
                            }
                            thetaTarget = QuantizeToSteps(thetaTarget, aMin, aMax, DragStepCount);
                        }

                        _thetaTargetSmoothed += Mathf.DeltaAngle(_thetaTargetSmoothed, thetaTarget) * 0.15f;

                        float errDeg = _thetaTargetSmoothed - theta;

                        if (DragRotIsToClamp)
                        {
                            const float STICK_DEG = 2.0f;
                            const float eps = 0.1f;

                            if (thetaUnwrapped >= max - eps && errDeg > 0f)
                            {
                                errDeg = 0f;
                                if (thetaTarget > max - STICK_DEG) thetaTarget = max;
                            }
                            if (thetaUnwrapped <= min + eps && errDeg < 0f)
                            {
                                errDeg = 0f;
                                if (thetaTarget < min + STICK_DEG) thetaTarget = min;
                            }
                        }

                        wAxis = Vector3.Dot(interactableRb.angularVelocity, axisWorld);
                        float errRad = errDeg * Mathf.Deg2Rad;
                        float torque = RotDirSign * (DragRotSpring * errRad) - DragRotDamping * wAxis;
                        interactableRb.AddTorque(axisWorld * torque, ForceMode.Acceleration);
                    }

                    if (_dragReturningToStart)
                    {
                        float errDeg;

                        if (ReturnByTheSameDirection && DragRotIsToClamp)
                            errDeg = _dragRotStartTheta - theta;
                        else
                            errDeg = Mathf.DeltaAngle(theta, _dragRotStartTheta);

                        float errRad = errDeg * Mathf.Deg2Rad;

                        float torqueReturn = RotDirSign * (DragRotSpring * errRad) - DragRotDamping * wAxis;
                        interactableRb.AddTorque(axisWorld * torqueReturn, ForceMode.Acceleration);

                        if (Mathf.Abs(errDeg) <= ROT_LIMIT_EPS && Mathf.Abs(wAxis) < DragRotSleepVel)
                        {
                            float deltaPhys;

                            if (ReturnByTheSameDirection && DragRotIsToClamp)
                                deltaPhys = RotDirSign * (_dragRotStartTheta - thetaUnwrapped);
                            else
                                deltaPhys = RotDirSign * Mathf.DeltaAngle(thetaUnwrapped, _dragRotStartTheta);

                            Quaternion dq = Quaternion.AngleAxis(deltaPhys, axisWorld);

                            Vector3 p0 = interactableRb.position;
                            interactableRb.MovePosition(hingeWorld + dq * (p0 - hingeWorld));
                            interactableRb.MoveRotation(dq * interactableRb.rotation);

                            interactableRb.angularVelocity = Vector3.zero;

                            thetaUnwrapped = _dragRotStartTheta;
                            thetaAccum = _dragRotStartTheta;
                            _thetaTargetSmoothed = _dragRotStartTheta;

                            _dragReturningToStart = false;

                            if (_dragInvokeAfterReturn) AfterReturnToPos?.Invoke();
                            _dragInvokeAfterReturn = false;
                        }
                    }
                    else
                    {
                        if (!isDragging)
                        {
                            float min = DragAngleClampMinMax.x;
                            float max = DragAngleClampMinMax.y;
                            OrderMinMax(ref min, ref max);

                            float damping = DragRotIdleDamping;
                            wAxis = Vector3.Dot(interactableRb.angularVelocity, axisWorld);
                            if (damping > 0f)
                                interactableRb.AddTorque(-axisWorld * (damping * wAxis), ForceMode.Acceleration);

                            bool insideWalls = !DragRotIsToClamp || (theta > min && theta < max);

                            if (insideWalls && Mathf.Abs(wAxis) < DragRotSleepVel)
                                interactableRb.angularVelocity = Vector3.zero;
                        }
                    }
                }
            }
        }

        void Update()
        {
            if (HasSensor)
            {
                Vector3 playerPos = singleton.playerObject.transform.position;

                bool insideNow = false;

                if (SensorType == SensorType.Radius)
                {
                    Vector3 center = transform.position + SensorOffset;

                    Vector3 delta = playerPos - center;
                    delta.y = 0f;

                    insideNow = delta.sqrMagnitude <= (SensorRadius * SensorRadius);
                }
                else if (SensorType == SensorType.Distance) insideNow = Vector3.Distance(singleton.playerObject.transform.position, transform.position) <= SensorDistance;

                if (insideNow)
                {
                    if (!wasInside)
                    {
                        wasInside = true;

                        if (sensorEventBool) sensorEventBool = false;

                        singleton.AddCheckSensorList(gameObject);
                    }

                    singleton.HandleUI(this, true);

                    if (!OnlyShowUI &&
                        ((!HasAutoInteract && singleton.raycastUpdatedPoint == null) || HasAutoInteract) &&
                        singleton.IsNearestSensorObject(gameObject))
                    {
                        singleton.HandleInteractionMode(this, transform);
                    }
                }
                else if (wasInside)
                {
                    wasInside = false;
                    oneTimeUnityEventBool = false;

                    singleton.RemoveFromSensorList(gameObject);
                    singleton.ResetUI(this);

                    if (!sensorEventBool)
                    {
                        onSensorExit.Invoke();
                        sensorEventBool = true;
                    }
                }
            }

            if (UseConditions && hasOnConditionAttended)
            {
                bool attended = ConditionRuntime.Evaluate(Conditions);

                if (attended)
                {
                    if (!oneTimeConditionEvent)
                    {
                        onConditionAttended.Invoke();
                        oneTimeConditionEvent = true;
                    }
                }
                else if (oneTimeConditionEvent)
                {
                    oneTimeConditionEvent = false;
                }
            }
        }
        #endregion

        #region Aux Methods

        [HideInInspector]
        public Vector3 GetWorldUIAnchorWorld()
        {
            if (WorldUIOverrideAnchor)
                return transform.TransformPoint(LocalPositionWorldUIAnchor);

            if (AlignWorldSpaceToAnchor && WillOverrideAnchor)
                return transform.TransformPoint(LocalPositionNewAnchor);

            return transform.position;
        }

        #endregion

        #region Helper Methods

        void WarmupConditionBindings()
        {
            if (!UseConditions || Conditions == null || Conditions.items == null) return;

            for (int i = 0; i < Conditions.items.Count; i++)
            {
                var c = Conditions.items[i];
                if (c == null || c.UseKey) continue;

                if (c.left != null) c.left.Resolve();
                if (c.RightSideDynamic && c.right != null) c.right.Resolve();
            }
        }

        void EnsureConditionGroup()
        {
            if (Conditions == null) Conditions = new ConditionGroup();
            if (Conditions.items == null) Conditions.items = new List<Condition>();
            if (Conditions.joins == null) Conditions.joins = new List<LogicalJoin>();
        }

        void SetConstForLeftType(Condition c, object constant)
        {
            var lt = c.left?.ValueType;

            if (lt == typeof(string))
            {
                c.str = constant?.ToString() ?? string.Empty;
                return;
            }

            if (lt == typeof(bool))
            {
                c.boolean = Convert.ToBoolean(constant ?? false);
                return;
            }

            if (lt == typeof(Vector2))
            {
                if (c.op == ConditionOperator.Equal || c.op == ConditionOperator.NotEqual)
                    c.vector2 = constant is Vector2 v2 ? v2 :
                                constant is Vector3 v3 ? new Vector2(v3.x, v3.y) : default;
                else
                    c.number = Convert.ToSingle(constant ?? 0f);
                return;
            }
            if (lt == typeof(Vector3))
            {
                if (c.op == ConditionOperator.Equal || c.op == ConditionOperator.NotEqual)
                    c.vector3 = constant is Vector3 v3 ? v3 :
                                constant is Vector2 v2 ? new Vector3(v2.x, v2.y, 0f) : default;
                else
                    c.number = Convert.ToSingle(constant ?? 0f);
                return;
            }

            if (lt == typeof(Quaternion))
            {
                c.quaternionConst = constant is Quaternion q ? q : Quaternion.identity;
                return;
            }

            if (lt != null && typeof(GameObject).IsAssignableFrom(lt))
            {
                c.GameObject = constant as GameObject;
                return;
            }
            if (lt != null && typeof(UnityEngine.Object).IsAssignableFrom(lt))
            {
                c.obj = constant as UnityEngine.Object;
                return;
            }

            c.number = Convert.ToSingle(constant ?? 0f);
        }

        [HideInInspector] public void AssignRB()
        {
            interactableRb = GetComponent<Rigidbody>();

            if (interactableRb == null && (interactMode == InteractMode.Grab || interactMode == InteractMode.Drag)) interactableRb = gameObject.AddComponent<Rigidbody>();

            if (interactableRb != null && SmoothRigidbodyPhysics)
            {
                interactableRb.interpolation = RigidbodyInterpolation.Interpolate;
                interactableRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        #endregion

        #region For Event Functions

        public void DebugMessage(string message)
        {
            Debug.Log(message);
        }

        #endregion

        #region Public Methods

        public void SetCanInteract(bool enabled) => canInteract = enabled;

        public void AddConditionConst(
            Component leftComponent,
            string leftMember,
            ConditionOperator op,
            object constantValue,
            LogicalJoin joinWithPrevious = LogicalJoin.And)
        {
            var go = leftComponent != null ? leftComponent.gameObject : null;
            var leftType = leftComponent != null ? leftComponent.GetType() : null;

            AddConditionConst(go, leftType, leftMember, op, constantValue, joinWithPrevious);
        }

        public void AddConditionConst(
            GameObject leftTarget,
            Type leftComponentType,
            string leftMember,
            ConditionOperator op,
            object constantValue,
            LogicalJoin joinWithPrevious = LogicalJoin.And)
        {
            EnsureConditionGroup();

            var cond = new Condition
            {
                UseKey = false,
                op = op,
                RightSideDynamic = false,
                left = new ValueBinding()
            };

            cond.left.target = leftTarget;
            cond.left.SetComponentType(leftComponentType);
            cond.left.MemberName = leftMember;

            SetConstForLeftType(cond, constantValue);

            if (Conditions.items.Count > 0)
                Conditions.joins.Add(joinWithPrevious);

            Conditions.items.Add(cond);
            UseConditions = true;
        }

        public void AddConditionDynamic(
            Component leftComponent, string leftMember,
            ConditionOperator op,
            Component rightComponent, string rightMember,
            LogicalJoin joinWithPrevious = LogicalJoin.And)
        {
            var leftGo = leftComponent != null ? leftComponent.gameObject : null;
            var rightGo = rightComponent != null ? rightComponent.gameObject : null;

            AddConditionDynamic(
                leftGo, leftComponent?.GetType(), leftMember,
                op,
                rightGo, rightComponent?.GetType(), rightMember,
                joinWithPrevious);
        }

        public void AddConditionDynamic(
            GameObject leftTarget, Type leftComponentType, string leftMember,
            ConditionOperator op,
            GameObject rightTarget, Type rightComponentType, string rightMember,
            LogicalJoin joinWithPrevious = LogicalJoin.And)
        {
            EnsureConditionGroup();

            var cond = new Condition
            {
                UseKey = false,
                op = op,
                RightSideDynamic = true,
                left = new ValueBinding(),
                right = new ValueBinding()
            };

            cond.left.target = leftTarget;
            cond.left.SetComponentType(leftComponentType);
            cond.left.MemberName = leftMember;

            cond.right.target = rightTarget;
            cond.right.SetComponentType(rightComponentType);
            cond.right.MemberName = rightMember;

            if (Conditions.items.Count > 0)
                Conditions.joins.Add(joinWithPrevious);

            Conditions.items.Add(cond);
            UseConditions = true;
        }

        public void AddKeyCondition(
            string keyName,
            LogicalJoin joinWithPrevious = LogicalJoin.And)
        {
            EnsureConditionGroup();
            var cond = new Condition
            {
                UseKey = true,
                KeyCheckMethod = KeyAccept.KeyName,
                KeyName = keyName
            };

            if (Conditions.items.Count > 0)
                Conditions.joins.Add(joinWithPrevious);

            Conditions.items.Add(cond);
            UseConditions = true;
        }

        public void AddKeyCondition(
            Key specificKey,
            LogicalJoin joinWithPrevious = LogicalJoin.And)
        {
            EnsureConditionGroup();
            var cond = new Condition
            {
                UseKey = true,
                KeyCheckMethod = KeyAccept.SpecificKey,
                SpecificKey = specificKey
            };

            if (Conditions.items.Count > 0)
                Conditions.joins.Add(joinWithPrevious);

            Conditions.items.Add(cond);
            UseConditions = true;
        }

        #endregion

    }
}