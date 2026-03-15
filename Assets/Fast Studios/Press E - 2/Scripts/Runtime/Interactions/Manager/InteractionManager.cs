using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FastStudios
{
    public partial class InteractionManager : MonoBehaviour
    {
        #region Variables
        public bool ThisSceneOnly = false;
        [SerializeField] private float InteractionDistance = 2f;
        public float PlacementDistance = 3f;

#region Player
        public PlayerDetection PlayerDetection = PlayerDetection.GameObject;
        public GameObject playerObject;
        public string playerTag;
        public int playerLayer;
        public string playerObjectName;
        public MonoBehaviour playerScript;
        #endregion

#region Input
        public InputSystemEnum inputSystem = InputSystemEnum.Old;
        public bool CaptureUIButtonsInteraction = true;
        public bool OnlyGetUIButtonsInput = false;

        public PressEInputBind Interaction = new PressEInputBind
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.E,
            MouseButton = MouseMethod.Left
        };

        #endregion

#region Events

        public UnityEvent blockPlayerMovement;
        public UnityEvent unblockPlayerMovement;
        public UnityEvent OnSceneChangeEvent;
        #endregion Events

#region UI

        public GameObject ScreenPromptPrefab;
        public GameObject WorldPromptPrefab;
        public GameObject InspectionPrefab;
        public GameObject InteractionUIPrefab;

        #endregion

#region Static Fields
        public static InteractionManager singleton { get; private set; }
        public static InputSystemEnum ProjectInputSystem { get; private set; } = InputSystemEnum.Old;
        public static bool ProjectCaptureUIButtons { get; private set; } = true;
        public static bool ProjectOnlyGetUIButtonsInput { get; private set; } = false;

        #endregion

#region Private Fields

        #region General Privates
        [SerializeField] private Camera overrideCamera;
        public Camera Cam { get; private set; }
        public Transform CamT { get; private set; }
        [HideInInspector] public Collider playerCollider;

        private int _lastFindPlayerFrame = -1;
        private int _nearestSensorFrame = -1;
        private GameObject _nearestSensorGO;
        [HideInInspector] public bool SettedPlayer;

        #endregion

        #region Basic Interaction Privates
        [HideInInspector] public LayerMask interactableLayer = ~0;
        private GameObject hitCollider;
        private GameObject actualCollider;
        private Interactable actualInteractable;
        private GameObject oldHitCollider;
        private Interactable oldHitInteractable;
        private GameObject exclusiveOwner;
        private Interactable exclusiveInteractable;

        private Vector3 originalPos;
        private Quaternion originalRot;

        private bool oneTimeInteraction = false;
        private bool raycastOneTime;
        private bool interactionBlockInteraction;
        private float interactionDistanceBackup;
        [HideInInspector] public GameObject raycastUpdatedPoint = null;
        private RaycastHit _lastRaycastHit;
        private bool _hasLastRaycastHit;
        private List<GameObject> SensorsObjects = new List<GameObject>();
        private Dictionary<Interactable, UIPrefab> CloneUIS = new Dictionary<Interactable, UIPrefab>();
        private readonly List<Interactable> _cloneUiRemoveBuffer = new List<Interactable>();
        GameObject worldOwner;
        int worldOwnerLayer = int.MinValue;
        bool worldOwnerFromSensor = false;

        #endregion

        #region UI
        [SerializeField] bool prewarmUIOnAwake = true;
        private GameObject worldPromptObject;
        private UIPrefab worldPromptUIPrefab;
        private GameObject screenPromptObject;
        private UIPrefab screenPromptUIPrefab;
        private GameObject inspectionDetails;
        private UIPrefab inspectionDetailsUIPrefab;
        private GameObject grabUI;
        private UIPrefab grabUIprefab;
        private GameObject DragUI;
        private UIPrefab dragUIprefab;
        private GameObject holdUI;
        private UIPrefab holdUIprefab;
        #endregion

        #endregion

        #endregion

        #region Default Methods

        void Awake()
        {
            if (singleton != null && singleton != this)
            {
                Destroy(gameObject);
                return;
            }

            singleton = this;

            CacheProjectSettings();

            if (transform.parent != null) transform.SetParent(null);
            if (!ThisSceneOnly) DontDestroyOnLoad(gameObject);

            Setup();

            ReferenceCatch();

            _ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            _maskAllButIgnoreRaycast = ~(1 << _ignoreRaycastLayer);
        }

        void Start()
        {
            if (prewarmUIOnAwake) PrewarmUI();

            SceneManager.activeSceneChanged += OnSceneChange;
        }

        void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            if (inputSystem != InputSystemEnum.Old)
            {
                EnableAction(Interaction.Action);
                EnableAction(GrabDrop.Action);
                EnableAction(GrabPlace.Action);
                EnableAction(GrabThrow.Action);
                EnableAction(GrabTurnLeft.Action);
                EnableAction(GrabTurnRight.Action);
                EnableAction(GrabRotation.Action);
                EnableAction(InspectionRotation.Action);
                EnableAction(InspectionDetailsText.Action);
                EnableAction(InspectionDetailsImage.Action);
            }
#endif
        }

        void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            DisableAction(Interaction.Action);
            DisableAction(GrabDrop.Action);
            DisableAction(GrabPlace.Action);
            DisableAction(GrabThrow.Action);
            DisableAction(GrabTurnLeft.Action);
            DisableAction(GrabTurnRight.Action);
            DisableAction(GrabRotation.Action);
            DisableAction(InspectionRotation.Action);
            DisableAction(InspectionDetailsText.Action);
            DisableAction(InspectionDetailsImage.Action);
#endif
        }

        void Update()
        {
            dragKeyConsumedThisFrame = false;
            inspectionKeyConsumedThisFrame = false;
            RaycastHit hit;
            RaycastHit depositHit = default;
            Vector3 rayStartPos = CamT.position;
            Vector3 rayDirection = CamT.forward;

            bool canPlaceInside = false;
            ObjectDepositData placeInsideData = null;
            GrabDeposit insideDeposit = null;

            bool DepositRayHitSomething = Physics.Raycast(rayStartPos, rayDirection, out depositHit, InteractionDistance, Physics.AllLayers);
            bool InteractionRayHitSomething = Physics.Raycast(rayStartPos, rayDirection, out hit, InteractionDistance, _maskAllButIgnoreRaycast);

            bool interactionDown = InputHandler.GeneralInputDown(Interaction);
            interactionDown |= Interaction.UIButtonDown;

            if (DepositRayHitSomething)
            {
                if (depositHit.collider.TryGetComponent<GrabDeposit>(out var deposit) && deposit.depositMethod != DepositMethod.TriggerCollider)
                {
                    if (isHelding)
                    {
                        if (interactionDown)
                        {
                            deposit.DepositObject(heldingInteractable);
                            goto RETURN;
                        }

                        if (heldingInteractable.CanPlaceGrabDown)
                        {
                            if (deposit.IsOnSpecificList(heldingInteractable, out ObjectDepositData data))
                            {
                                canPlaceInside = true;
                                placeInsideData = data;
                                insideDeposit = deposit;
                            }
                            else if (deposit.GetOnSpecificListNullObject(out ObjectDepositData element))
                            {
                                canPlaceInside = true;
                                placeInsideData = element;
                                insideDeposit = deposit;
                            }

                            if (deposit.CanPlaceDownFreely)
                            {
                                canPlaceInside = true;
                            }

                            if (canPlaceInside)
                            {
                                PressEInputBind bind = InputHandler.ResolveBind(GrabPlace, deposit.isToOverrideInput, deposit.NewInput);
                                bool pressed = InputHandler.GeneralInputDown(bind);
                                pressed |= bind.UIButtonDown;

                                if (pressed)
                                {
                                    if (_placeRotOwner != heldingObject)
                                    {
                                        _placeRotOwner = heldingObject;
                                        _placeRotAngleDeg = 0f;
                                        if (heldingObject != null) _placeBaseRot = heldingObject.transform.rotation;
                                        _placeLastAlignNormals = heldingInteractable.AlignNormals;
                                    }
                                    else if (_placeLastAlignNormals != heldingInteractable.AlignNormals)
                                    {
                                        _placeLastAlignNormals = heldingInteractable.AlignNormals;
                                        if (heldingObject != null) _placeBaseRot = heldingObject.transform.rotation;
                                        _placeRotAngleDeg = 0f;
                                    }

                                    Ray depositPlaceRay = new Ray(rayStartPos, rayDirection);
                                    RaycastHit depositPlaceHit;
                                    bool depositPlaceHitSomething = RaycastFiltered(
                                        depositPlaceRay,
                                        PlacementDistance,
                                        _maskAllButIgnoreRaycast,
                                        QueryTriggerInteraction.Ignore,
                                        out depositPlaceHit
                                    );

                                    Quaternion lPlaceBaseRot;

                                    if (heldingInteractable.AlignNormals && depositPlaceHitSomething)
                                        lPlaceBaseRot = Quaternion.FromToRotation(Vector3.up, depositPlaceHit.normal);
                                    else
                                        lPlaceBaseRot = _placeBaseRot;

                                    Quaternion placeDesiredRot = lPlaceBaseRot;

                                    if (heldingInteractable.CanRotatePlaceObject)
                                    {
                                        Vector3 axis = AxisToVector(heldingInteractable.RotationAxis);

                                        if (heldingInteractable.CalculateOnLocal) placeDesiredRot = lPlaceBaseRot * Quaternion.AngleAxis(_placeRotAngleDeg, axis);
                                        else placeDesiredRot = Quaternion.AngleAxis(_placeRotAngleDeg, axis) * lPlaceBaseRot;
                                    }

                                    Vector3 pointThatHitted;

                                    if (depositPlaceHitSomething)
                                    {
                                        pointThatHitted = GetPlacePointPushedOut(heldingObject, heldingCols, heldingRends, depositPlaceHit.point, depositPlaceHit.normal, placeDesiredRot);
                                    }
                                    else
                                    {
                                        float d = Mathf.Min(PlacementDistance, depositHit.distance + 0.1f);
                                        pointThatHitted = depositPlaceRay.GetPoint(d);
                                    }

                                    if (deposit.CanPlaceDownFreely)
                                    {
                                        BoxCollider depBox = deposit.boxCollider;

                                        Vector3 local = deposit.transform.InverseTransformPoint(pointThatHitted);
                                        Vector3 delta = local - depBox.center;
                                        Vector3 half = depBox.size * 0.5f;

                                        const float eps = 0.02f;
                                        delta.x = Mathf.Clamp(delta.x, -half.x + eps, half.x - eps);
                                        delta.y = Mathf.Clamp(delta.y, -half.y + eps, half.y - eps);
                                        delta.z = Mathf.Clamp(delta.z, -half.z + eps, half.z - eps);

                                        pointThatHitted = deposit.transform.TransformPoint(depBox.center + delta);
                                    }

                                    if (isHelding && WouldCollideBeforePlace(heldingObject, heldingCols, heldingRends, pointThatHitted, placeDesiredRot, depositPlaceHit.collider) == false
                                     && WouldOverlapBlockedPlacementArea(heldingObject, heldingCols, heldingRends, pointThatHitted, placeDesiredRot) == false)
                                        deposit.DepositObject(heldingInteractable, pointThatHitted, placeDesiredRot);
                                }
                            }
                        }
                    }
                }
            }

            RETURN:;

            if (InteractionRayHitSomething)
            {
                bool hasInter = hit.collider.TryGetComponent(out Interactable hitInter);
                bool hasKey = hit.collider.TryGetComponent(out Key key);

                if (hasInter || hasKey)
                {
                    if (hasInter)
                    {
                        if (!(hitInter.hasMaxInteractions && hitInter.interactionTimes >= hitInter.maxInteractions) && hitInter.CanInteract)
                        {
                            HandleInteractable(hit, hitInter);
                        }
                        else if (hitInter.hasMaxInteractions && hitInter.interactionTimes >= hitInter.maxInteractions)
                        {
                            ResetUI(hitInter);
                        }
                    }

                    if (hasKey && key.CanInteract)
                    {
                        if (interactionDown)
                        {
                            key.Interact(key);
                        }
                    }
                }
                else
                {
                    RayHitNothing();
                }
            }
            else RayHitNothing();

            if (isHelding) UpdateIsGrabbing(rayStartPos, rayDirection, canPlaceInside, placeInsideData, insideDeposit);
            else UpdateNotGrabbing();

            if (isDragging) UpdateIsDragging();
            else UpdateNotDragging();

            if (isInspecting) UpdateIsInspecting();
            else UpdateNotInspecting();

            if (isHolding) UpdateIsHolding();
            else UpdateNotHolding();

            if (hasObtainedKeys && ObtainedKeysParent.transform.childCount == 0)
            {
                Destroy(ObtainedKeysParent);
                hasObtainedKeys = false;
            }
        }

        void LateUpdate()
        {
            if (isHelding)
            {
                var rb = heldingInteractable.interactableRb;

                if (rb != null)
                {
                    if (heldingInteractable.UseTransformToPosition && heldingInteractable.TransformGrabInstantFollow)
                    {
                        Vector3 target = ComputeGrabTarget(heldingInteractable);
                        Quaternion refRot = GetReferenceRotation(heldingInteractable);
                        Quaternion desired = refRot * _grabRotOffset;

                        heldingInteractable.transform.position = target;
                        heldingInteractable.transform.rotation = desired;
                    }
                }
            }

            ResetUIButtons();
        }

        void FixedUpdate()
        {
            if (isHelding)
            {
                var rb = heldingInteractable.interactableRb;

                if (rb != null)
                {
                    if (heldingInteractable.PhysicsGrabMode)
                    {
                        const float force = 1000;

                        Vector3 anchor = heldingInteractable.WillOverrideAnchor
                            ? heldingInteractable.transform.TransformPoint(heldingInteractable.LocalPositionNewAnchor)
                            : heldingInteractable.transform.position;

                        Vector3 target = ComputeGrabTarget(heldingInteractable);

                        Vector3 finalForce = target - anchor;

#if UNITY_6000_0_OR_NEWER
                        rb.linearDamping = heldingInteractable.linearDamping;
                        rb.angularDamping = heldingInteractable.angularDamping;
#elif UNITY_2022_3_OR_NEWER
                        rb.drag = heldingInteractable.linearDamping;
                        rb.angularDrag = heldingInteractable.angularDamping;
#endif

                        rb.AddForceAtPosition(
                            finalForce * Time.fixedDeltaTime * force * Mathf.Max(1, heldingInteractable.PlayerHelperForce),
                            anchor
                        );

                        Debug.DrawLine(target, anchor);
                        Debug.DrawRay(anchor, finalForce, Color.red);

                        ApplyGrabFollowRotation(heldingInteractable);
                    }
                    else if (heldingInteractable.UseTransformToPosition == false || heldingInteractable.TransformGrabInstantFollow == false)
                    {
                        Vector3 target = ComputeGrabTarget(heldingInteractable);
                        Vector3 newPos;

                        if (heldingInteractable.UseTransformToPosition)
                        {
                            float sharpness = heldingInteractable.TransformGrabFollowSharpness;

                            float a = Helper.ExpLerp01(sharpness, Time.fixedDeltaTime);
                            newPos = Vector3.Lerp(rb.position, target, a);
                        }
                        else
                        {
                            newPos = Vector3.Lerp
                            (
                                heldingObject.transform.position,
                                target,
                                Time.deltaTime * 20f
                            );
                        }

                        rb.MovePosition(newPos);

                        ApplyGrabFollowRotation(heldingInteractable);
                    }
                }
            }
            else if (grabDistance != grabDistanceBackup && heldingObject == null)
            {
                grabDistance = grabDistanceBackup;
            }
        }

        #endregion

        #region Helper Methods 
        private void Setup()
        {
            interactableLayer = ~0;

            interactionDistanceBackup = InteractionDistance;
            grabDistanceBackup = grabDistance;
        }

        private void ReferenceCatch()
        {
            Cam = overrideCamera != null ? overrideCamera : Camera.main;
            CamT = Cam.transform;
        }

        private void OnSceneChange(Scene old, Scene newScene)
        {
            OnSceneChangeEvent.Invoke();

            GetPlayerObject();

            ReferenceCatch();
        }

        private void PrewarmUI()
        {
            if (WorldPromptPrefab != null)  WarmupPrefab(WorldPromptPrefab);
            if (ScreenPromptPrefab != null) WarmupPrefab(ScreenPromptPrefab);
            if (InspectionPrefab != null)   WarmupPrefab(InspectionPrefab);
            if (InteractionUIPrefab != null) WarmupPrefab(InteractionUIPrefab);
        }

        private void WarmupPrefab(GameObject prefab)
        {
            var go = Instantiate(prefab, transform);
            go.SetActive(true);

            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
                t.ForceMeshUpdate();

            Canvas.ForceUpdateCanvases();

            go.SetActive(false);
            Destroy(go);
        }

        private void CacheProjectSettings()
        {
            ProjectInputSystem = inputSystem;
            ProjectCaptureUIButtons = CaptureUIButtonsInteraction;
            ProjectOnlyGetUIButtonsInput = OnlyGetUIButtonsInput;
        }

        private void RebuildNearestSensorCache()
        {
            _nearestSensorFrame = Time.frameCount;
            _nearestSensorGO = null;

            if (playerObject == null) GetPlayerObject();
            if (playerObject == null || SensorsObjects == null || SensorsObjects.Count == 0)
                return;

            Vector3 p = playerObject.transform.position;

            float best = float.MaxValue;

            for (int i = SensorsObjects.Count - 1; i >= 0; i--)
            {
                GameObject go = SensorsObjects[i];
                if (go == null)
                {
                    SensorsObjects.RemoveAt(i);
                    continue;
                }

                float d = (go.transform.position - p).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    _nearestSensorGO = go;
                }
            }
        }

        private void HandleInteractable(RaycastHit hit, Interactable interactable)
        {
            bool gotonow = false;

            if (isHelding)
            {
                if (heldingInteractable.CanInteractWithOthers == false) { gotonow = true; goto UI; }
                else if (heldingInteractable.AllowJustSpecificInteractions && heldingInteractable.SpecificOthersInteractions.Contains(interactable) == false) { gotonow = true; goto UI; }
            }

            if (isDragging)
            {
                if (draggingInteractable.CanInteractWithOthers == false) { gotonow = true; goto UI; }
                else if (draggingInteractable.AllowJustSpecificInteractions && draggingInteractable.SpecificOthersInteractions.Contains(interactable) == false) { gotonow = true; goto UI; }
            }

            if (isInspecting)
            {
                if (inspectionInteractable.CanInteractWithOthers == false) { gotonow = true; goto UI; }
                else if (inspectionInteractable.AllowJustSpecificInteractions && inspectionInteractable.SpecificOthersInteractions.Contains(interactable) == false) { gotonow = true; goto UI; }
            }

            hitCollider = hit.collider.gameObject;
            raycastUpdatedPoint = hitCollider;
            _lastRaycastHit = hit;
            _hasLastRaycastHit = true;

            if (!actualCollider)
            {
                actualCollider = hitCollider;
                actualInteractable = interactable;
            }
            else if (actualCollider != hitCollider)
            {
                oldHitCollider = actualCollider;
                oldHitInteractable = actualInteractable;

                actualCollider = hitCollider;
                actualInteractable = interactable;
            }
            else
            {
                actualInteractable = interactable;
            }

            if (interactable.OnRayCastEnterAndExit && !raycastOneTime)
            {
                interactable.onRaycastEnter.Invoke();

                raycastOneTime = true;
            }

            hasMaxHoldInteractions = interactable.hasMaxHoldInteractions;

            UI:;
            HandleUI(interactable);
            if (gotonow) return;

            if (!(interactable.HasSensor && interactable.HasAutoInteract))
            {
                HandleInteractionMode(interactable, hitCollider.transform);
            }
        }

        private void RayHitNothing()
        {
            Interaction.CanShow = false;

            if (oldHitCollider != null && oldHitInteractable != null && oldHitInteractable.OnRayCastEnterAndExit && raycastOneTime)
            {
                oldHitInteractable.onRaycastExit.Invoke();

                raycastOneTime = false;
            }

            if (isHelding == false && isInspecting == false) oneTimeInteraction = false;

            if (isHolding && !holdingInteractable.HasSensor)
            {
                holdingObject = null;
                isHolding = false;
                holdingInteractable = null;
                oneTimeHold = false;
                holdStartTime = 0;
            }

            if (worldPromptObject != null && worldOwner != null && !worldOwnerFromSensor)
            {
                Destroy(worldPromptObject);
                Destroy(worldPromptUIPrefab);
                worldPromptObject = null;
                worldOwner = null;
                worldOwnerLayer = int.MinValue;
            }

            ResetUI();
            raycastUpdatedPoint = null;
            _hasLastRaycastHit = false;
        }

        private void ResetUIButtons()
        {
            Interaction.UIButtonDown = false;
            Interaction.UIButtonUp = false;

            GrabDrop.UIButtonDown = false;
            GrabDrop.UIButtonUp = false;

            GrabPlace.UIButtonDown = false;
            GrabPlace.UIButtonUp = false;

            GrabTurnLeft.UIButtonDown = false;
            GrabTurnLeft.UIButtonUp = false;

            GrabTurnRight.UIButtonDown = false;
            GrabTurnRight.UIButtonUp = false;

            GrabThrow.UIButtonDown = false;
            GrabThrow.UIButtonUp = false;

            GrabRotation.UIButtonDown = false;
            GrabRotation.UIButtonUp = false;

            InspectionRotation.UIButtonDown = false;
            InspectionRotation.UIButtonUp = false;

            InspectionDetailsImage.UIButtonDown = false;
            InspectionDetailsImage.UIButtonUp = false;

            InspectionDetailsText.UIButtonDown = false;
            InspectionDetailsText.UIButtonUp = false;
        }

        private void SetExclusiveOwner(GameObject go, Interactable it)
        {
            exclusiveOwner = go;
            exclusiveInteractable = it;
        }

        private void ClearExclusiveOwner(GameObject go)
        {
            if (exclusiveOwner == go)
            {
                exclusiveOwner = null;
                exclusiveInteractable = null;
            }
        }

        public void HandleInteractionMode(Interactable interactable, Transform hitTransform)
        {
            if (interactable.UseConditions)
            {
                PressEInputBind bind = InputHandler.ResolveBind(Interaction, interactable.OverrideInteractionKey, interactable.NewInteraction);
                bool interactionDown = InputHandler.GeneralInputDown(bind);
                interactionDown |= bind.UIButtonDown;

                if (!ConditionRuntime.Evaluate(interactable.Conditions))
                {
                    if (interactionDown) interactable.OnConditionDecline.Invoke();

                    return;
                }

                if (interactionDown)
                    interactable.OnConditionAccept.Invoke();
            }

            if (draggingObject != null && interactable != draggingInteractable)
            {
                return;
            }

            switch (interactable.interactMode)
            {
                case InteractMode.UnityEvent:
                    HandleUnityEvent(interactable);
                    break;

                case InteractMode.Grab:
                    HandleGrab(hitTransform, interactable);
                    break;

                case InteractMode.Hold:
                    HandleHold(interactable);
                    break;

                case InteractMode.Drag:
                    HandleDrag(hitTransform, interactable);
                    break;

                case InteractMode.Inspection:
                    if (!interactionBlockInteraction) HandleInspection(hitTransform.gameObject, interactable);
                    break;
            }
        }

        public void SetInteractionDistance(float newDistance)
        {
            interactionDistanceBackup = InteractionDistance;
            InteractionDistance = newDistance;
        }

        public void TryGetPlayerCollider()
        {
            if (playerCollider == null && playerObject != null) playerCollider = playerObject.GetNoMatterWhat<Collider>(true);
        }

        [HideInInspector] public void HandleUI(Interactable interactable, bool fromSensor = false)
        {
            bool WorldUI = interactable.OverrideWorldPromptMessage && interactable.HasDeclinedConditionMessage;

            bool conditionsPassed = true;
            if (interactable.UseConditions && !WorldUI) conditionsPassed = ConditionRuntime.Evaluate(interactable.Conditions);
            if (!fromSensor && worldPromptObject is not null && worldOwner is not null && !worldOwnerFromSensor)
            {
                bool willShowWorldPromptHere =
                    conditionsPassed &&
                    interactable.WorldSpacePrompt &&
                    !(interactable.AdditionalWorldPrompt && interactable.HasSensor);

                if (!willShowWorldPromptHere)
                {
                    Destroy(worldPromptObject);
                    Destroy(worldPromptUIPrefab);
                    worldPromptObject = null;
                    worldOwner = null;
                    worldOwnerLayer = int.MinValue;
                }
            }
            if (interactable.UseConditions && !WorldUI && !conditionsPassed)
                return;


            if (IsExclusiveActive() && interactable.gameObject != exclusiveOwner)
            {
                if (exclusiveInteractable.CanShowOthersUI == false)
                {
                    ResetUI(interactable);
                    return;
                }
                else if (exclusiveInteractable.AllowJustSpecificUIToShow && exclusiveInteractable.SpecificOthersUI.Contains(interactable) == false)
                {
                    ResetUI(interactable);
                    return;
                }
            }

            if (isHelding)
            {
                if (heldingInteractable.CanShowOthersUI == false)
                {
                    ResetUI(interactable);
                    return;
                }
                else if (heldingInteractable.AllowJustSpecificUIToShow && heldingInteractable.SpecificOthersUI.Contains(interactable) == false)
                {
                    ResetUI(interactable);
                    return;
                }
            }

            if (isDragging)
            {
                if (draggingInteractable.CanShowOthersUI == false)
                {
                    ResetUI(interactable);
                    return;
                }
                else if (draggingInteractable.AllowJustSpecificUIToShow && draggingInteractable.SpecificOthersUI.Contains(interactable) == false)
                {
                    ResetUI(interactable);
                    return;
                }
            }
            
            if (isInspecting)
            {
                if (inspectionInteractable.CanShowOthersUI == false)
                {
                    ResetUI(interactable);
                    return;
                }
                else if (inspectionInteractable.AllowJustSpecificUIToShow && inspectionInteractable.SpecificOthersUI.Contains(interactable) == false)
                {
                    ResetUI(interactable);
                    return;
                }
            }

            if (interactable.gameObject == heldingObject || interactable.gameObject == draggingObject || interactable.gameObject == inspectionObject)
            {
                ResetUI(interactable);
                return;
            }

            if (interactable.ScreenSpacePrompt)
            {
                GameObject prefab = interactable.overrideScreenSpacePrefab && interactable.ScreenSpacePrefab != null ? interactable.ScreenSpacePrefab : ScreenPromptPrefab;

                if (screenPromptObject == null)
                {
                    // Just called one time
                    screenPromptObject = Instantiate(prefab, transform);
                    screenPromptUIPrefab = screenPromptObject.GetComponent<UIPrefab>();
                }
                else
                {
                    screenPromptUIPrefab.ShowUI(interactable);
                }
            }

            if (interactable.WorldSpacePrompt)
            {
                if (!(interactable.AdditionalWorldPrompt && interactable.HasSensor))
                {
                    if (worldPromptObject == null)
                    {
                        // Just called one time
                        worldPromptObject = Instantiate(interactable.overrideWorldSpacePrefab && interactable.WorldSpacePrefab != null ? interactable.WorldSpacePrefab : WorldPromptPrefab, transform);
                        worldOwner = interactable.gameObject;
                        worldOwnerLayer = interactable.WorldPromptLayer;
                        worldOwnerFromSensor = fromSensor;
                        worldPromptUIPrefab = worldPromptObject.GetComponent<UIPrefab>();
                        worldPromptUIPrefab.layer = worldOwnerLayer;
                        worldPromptUIPrefab.interactedInteractable = interactable;
                    }
                    else
                    {
                        bool isOwner = worldOwner == interactable.gameObject;
                        int candidateLayer = interactable.WorldPromptLayer;
                        int currentLayer = worldOwnerLayer;

                        bool shouldReplace = false;

                        if (!fromSensor && !isOwner)
                            shouldReplace = true;

                        if (!shouldReplace && !isOwner)
                        {
                            if (candidateLayer > currentLayer) shouldReplace = true;
                            else if (candidateLayer == currentLayer && !fromSensor && worldOwnerFromSensor)
                                shouldReplace = true;
                        }

                        if (shouldReplace)
                        {
                            worldOwner = interactable.gameObject;
                            worldOwnerLayer = candidateLayer;
                            worldOwnerFromSensor = fromSensor;

                            if (worldPromptObject.TryGetComponent<UIPrefab>(out var uiPrefab) && worldPromptUIPrefab != uiPrefab)
                            {
                                worldPromptUIPrefab = uiPrefab;
                            }

                            worldPromptUIPrefab.layer = worldOwnerLayer;
                            worldPromptUIPrefab.interactedInteractable = interactable;
                        }
                    }

                    if (worldOwner != null && worldPromptObject != null)
                    {
                        CalculatePosition(worldPromptUIPrefab, interactable);
                    }
                }
                else
                {
                    if (!CloneUIS.ContainsKey(interactable))
                    {
                        GameObject g = Instantiate(interactable.overrideWorldSpacePrefab && interactable.WorldSpacePrefab != null ? interactable.WorldSpacePrefab : WorldPromptPrefab, transform);

                        if (g.TryGetComponent<UIPrefab>(out var uiprefab)) CloneUIS.Add(interactable, uiprefab);
                    }

                    foreach ((Interactable key, UIPrefab worldPromptUIPrefab) in CloneUIS)
                    {
                        CalculatePosition(worldPromptUIPrefab, key);
                    }
                }
            }

            void CalculatePosition(UIPrefab objectUI, Interactable interactableKey)
            {
                GameObject worldPrompt = objectUI.gameObject;

                if (interactableKey.OverrideWorldPromptMessage && objectUI.InteractionTmpText != null && objectUI.hasInteractionText)
                {
                    if (interactableKey.UseConditions && interactableKey.HasDeclinedConditionMessage)
                    {
                        if (!ConditionRuntime.Evaluate(interactableKey.Conditions)) objectUI.SetInteractionText(interactableKey.DeclinedWorldPromptMessage, interactableKey);
                        else objectUI.SetInteractionText(interactableKey.WorldPromptMessage, interactableKey);
                    }
                    else objectUI.SetInteractionText(interactableKey.WorldPromptMessage, interactableKey);
                }
                else if (interactableKey.OverrideWorldPromptMessage && objectUI.InteractionTmpText == null && objectUI.hasInteractionText)
                {
                    Debug.LogWarning("[Press E] UI Prefab trying to show a null interaction text");
                }

                objectUI.layer = interactableKey.WorldPromptLayer;
                objectUI.ShowUI(interactableKey);

                Vector3 anchorWorld = interactableKey.GetWorldUIAnchorWorld();
                if (interactableKey.AlignWorldSpaceToAnchor && interactableKey.WillOverrideAnchor)
                    anchorWorld = interactableKey.transform.TransformPoint(interactableKey.LocalPositionNewAnchor);

                Vector3 screenPos = Cam.WorldToScreenPoint(anchorWorld) + (Vector3)interactableKey.WorldSpaceOffset;

                bool behindCamera = screenPos.z < 0f;
                if (worldPrompt.activeSelf == behindCamera) worldPrompt.SetActive(!behindCamera);
                if (behindCamera) return;

                objectUI.rectTransform.position = screenPos;

                if (interactableKey.WorldSize)
                {
                    float dist = Vector3.Distance(CamT.position, anchorWorld);
                    float scale = interactableKey.ReferencePromptDistance / Mathf.Max(0.01f, dist);
                    if (interactableKey.ReferencePrompHasScaleMinMax) scale = Mathf.Clamp(scale, interactableKey.PromptScaleMinMax.x, interactableKey.PromptScaleMinMax.y);
                    worldPrompt.transform.localScale = Vector3.one * scale;
                }
                else
                {
                    if (worldPrompt.transform.localScale != Vector3.one)
                        worldPrompt.transform.localScale = Vector3.one;
                }
            }
        }

        [HideInInspector] public void ResetUI(Interactable interactable = null)
        {
            if (transform.childCount <= 0) return;

            if (interactable != null && (interactable.HasSensor && interactable.AdditionalWorldPrompt) == false)
            {
                if (worldOwner == interactable.gameObject && worldPromptObject != null)
                {
                    Destroy(worldPromptObject);
                    worldPromptObject = null;
                    worldOwner = null;
                    worldOwnerLayer = int.MinValue;
                }

                if (interactable.ScreenSpacePrompt && screenPromptObject != null)
                {
                    Destroy(screenPromptObject);
                    screenPromptObject = null;
                }
            }
            else if (interactable != null)
            {
                if (CloneUIS.TryGetValue(interactable, out var value))
                {
                    if (value != null)
                        Destroy(value.gameObject);

                    CloneUIS.Remove(interactable);
                }

                if (interactable.ScreenSpacePrompt && screenPromptObject != null)
                {
                    Destroy(screenPromptObject);
                    screenPromptObject = null;
                }
            }
            else if (interactable == null && SensorsObjects.Count == 0)
            {
                foreach (Transform child in transform)
                {
                    if (grabUI != null && grabUI == child.gameObject) continue;
                    if (DragUI != null && DragUI == child.gameObject) continue;
                    if (holdUI != null && holdUI == child.gameObject) continue;

                    Destroy(child.gameObject);
                }

                if (CloneUIS.Count > 0) CloneUIS.Clear();

                if (screenPromptObject != null) screenPromptObject = null;
                if (worldPromptObject != null) worldPromptObject = null;
            }
            else if (interactable == null && SensorsObjects.Count != 0)
            {
                if (screenPromptObject != null)
                {
                    Destroy(screenPromptObject);
                    screenPromptObject = null;
                }

                if (worldPromptObject != null && worldOwner != null && !worldOwnerFromSensor)
                {
                    Destroy(worldPromptObject);
                    worldPromptObject = null;
                    worldOwner = null;
                    worldOwnerLayer = int.MinValue;
                }

                _cloneUiRemoveBuffer.Clear();
                foreach (var pair in CloneUIS)
                {
                    Interactable key = pair.Key;

                    if (key == null || !key.HasSensor)
                        _cloneUiRemoveBuffer.Add(key);
                }

                for (int i = 0; i < _cloneUiRemoveBuffer.Count; i++)
                {
                    Interactable toDestroy = _cloneUiRemoveBuffer[i];

                    if (CloneUIS.TryGetValue(toDestroy, out var ui) && ui != null)
                        Destroy(ui.gameObject);

                    CloneUIS.Remove(toDestroy);
                }
            }
        }

        [HideInInspector] public void AddCheckSensorList(GameObject objectToAdd)
        {
            if (objectToAdd == null) return;
            if (!SensorsObjects.Contains(objectToAdd)) SensorsObjects.Add(objectToAdd);
        }

        [HideInInspector] public void RemoveFromSensorList(GameObject objectToRemove)
        {
            if (objectToRemove == null) return;
            SensorsObjects.Remove(objectToRemove);
        }

#if ENABLE_INPUT_SYSTEM
        static void EnableAction(InputActionReference a)
        {
            if (a != null && a.action != null) a.action.Enable();
        }

        static void DisableAction(InputActionReference a)
        {
            if (a != null && a.action != null) a.action.Disable();
        }
#endif
        #endregion

        #region Aux Methods

        public bool IsExclusiveActive() => exclusiveOwner != null && exclusiveInteractable != null;

        public bool IsNearestSensorObject(GameObject candidate)
        {
            if (candidate == null) return false;

            if (_nearestSensorFrame != Time.frameCount)
                RebuildNearestSensorCache();

            return _nearestSensorGO == candidate;
        }

        public GameObject GetPlayerObject(GameObject objectThatInteracted = null)
        {
            SettedPlayer = true;

            if (playerObject != null)
            {
                if (playerCollider == null) TryGetPlayerCollider();
                return playerObject;
            }

            switch (PlayerDetection)
            {
                case PlayerDetection.GameObject:
                    if (playerCollider == null) TryGetPlayerCollider();
                    return playerObject;

                case PlayerDetection.Tag:

                    if (objectThatInteracted == null)
                    {
                        if (_lastFindPlayerFrame == Time.frameCount) return playerObject;
                        _lastFindPlayerFrame = Time.frameCount;
                        objectThatInteracted = GameObject.FindGameObjectWithTag(playerTag);
                    }

                    if (objectThatInteracted != null && objectThatInteracted.CompareTag(playerTag)) playerObject = objectThatInteracted;
                    if (playerCollider == null) TryGetPlayerCollider();
                    return playerObject;

                case PlayerDetection.Layer:
                    if (objectThatInteracted.layer == playerLayer) playerObject = objectThatInteracted;
                    if (playerCollider == null) TryGetPlayerCollider();
                    return playerObject;

                case PlayerDetection.ObjectName:
                    if (objectThatInteracted.name == playerObjectName) playerObject = objectThatInteracted;
                    if (playerCollider == null) TryGetPlayerCollider();
                    return playerObject;

                case PlayerDetection.MonoBehaviour:
                    if (objectThatInteracted.GetComponent(nameof(playerScript))) playerObject = objectThatInteracted;
                    if (playerCollider == null) TryGetPlayerCollider();
                    return playerObject;

                case PlayerDetection.Reference:
                    if (playerCollider == null) TryGetPlayerCollider();
                    return playerObject;

                default:
                    if (objectThatInteracted != null) Debug.LogWarning($"[PressE] Couldnt Find Player Object In {objectThatInteracted}");
                    else Debug.LogWarning($"[PressE] Couldnt Find Player Object and Object that Interacted");
                    return null;
            }
        }

        #endregion
    }
}