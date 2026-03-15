using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace FastStudios
{
    public partial class InteractionManager // GRAB
    {
        #region Grab Inputs

        public PressEInputBind GrabDrop = new PressEInputBind
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.E,
            MouseButton = MouseMethod.Left
        };

        public PressEInputBind GrabPlace = new PressEInputBind
        {
            InputMethod = InputMethod.Mouse,
            Key = KeyCode.E,
            MouseButton = MouseMethod.Left
        };


        public PressEInputBind GrabTurnLeft = new PressEInputBind
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.Q,
            MouseButton = MouseMethod.Left
        };

        public PressEInputBind GrabTurnRight = new PressEInputBind
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.E,
            MouseButton = MouseMethod.Left
        };

        public PressEInputBind GrabThrow = new PressEInputBind
        {
            InputMethod = InputMethod.Mouse,
            Key = KeyCode.E,
            MouseButton = MouseMethod.Left
        };


        public PressEInputBind GrabRotation = new PressEInputBind
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.LeftAlt,
            MouseButton = MouseMethod.Left
        };

        #endregion

        #region Grab Transform
        
        public bool UseGrabReference = true;

        public Transform ItemPlayerGrabTransform;
        
        #endregion

        #region Grab Materials
        public Material GrabRightPlacement;
        public Material GrabWrongPlacement;
        #endregion

        #region Grab Privates
        [HideInInspector] public GameObject heldingObject;
        [HideInInspector] public Interactable heldingInteractable;
        public bool isHelding;
        private Collider[] heldingCols;
        private Renderer[] heldingRends;
        private float grabDistance;
        private float grabDistanceBackup;
        bool oneTimeGrabKey = false;
        private bool oneTimeGrabThrowKeyHandler = false;
        private bool grabDropArmed = true;
        private Transform _grabRefTransform;
        private Quaternion _grabRotOffset = Quaternion.identity;
        private Coroutine _grabRotOffsetLerp;
        private GameObject ghostVisualizer;
        private Interactable ghostInteractable;
        private Rigidbody ghostRigidbody;
        private Collider[] ghostCols;
        private Material _ghostLastAppliedMat;
        bool wasKinematic;
        private GameObject _placeRotOwner;
        private float _placeRotAngleDeg;
        private Quaternion _placeBaseRot = Quaternion.identity;
        private bool _placeLastAlignNormals;
        private readonly Collider[] _placeOverlap = new Collider[64];
        private int _ignoreRaycastLayer;
        [HideInInspector] public int _maskAllButIgnoreRaycast;
        private readonly RaycastHit[] _rayHits = new RaycastHit[32];
        private readonly Dictionary<Rigidbody, RigidbodyInterpolation> _interpRestoreCache = new Dictionary<Rigidbody, RigidbodyInterpolation>();
        private double StartPressingGrabThrowTime;

        #endregion

        void HandleGrab(Transform hitTransform, Interactable interactable)
        {
            void GetGrabObject()
            {
                if (heldingObject == null)
                {
                    if (interactable.interactableRb)
                    {
                        heldingObject = hitTransform.gameObject;
                        isHelding = true;
                        interactable.isGrabbing = true;
                        heldingInteractable = interactable;
                        heldingCols = heldingObject.GetComponentsInChildren<Collider>(true);
                        heldingRends = heldingObject.GetComponentsInChildren<Renderer>(true);
                        grabDropArmed = false;

                        if (interactable.grabDeposit != null)
                        {
                            wasKinematic = interactable.wasKinematic;
                            interactable.grabDeposit.RemoveFromDeposit(interactable);
                        }
                        else wasKinematic = interactable.interactableRb.isKinematic;

                        Collider heldingCollider = heldingInteractable.Collider;

                        if (playerCollider == null) TryGetPlayerCollider();

                        if (heldingCollider != null && playerCollider != null) Physics.IgnoreCollision(heldingCollider, playerCollider, true);
                        else if (heldingCollider != null && playerCollider == null)
                        {
                            Debug.LogWarning($"[PressE] Couldnt find player's collider. Please attatch a collider to Player Object, for this grab we are deactivating helding object collider. To prevent this, assign player collider to make just player and grab collider's ignore themselves");
                            heldingCollider.enabled = false;
                        }
                        else
                        {
                            string targets = $"{(heldingCollider == null ? "Helding Object Collider" : "")}{(heldingCollider == null && playerCollider == null ? " and " : "")}{(playerCollider == null ? "Player Object Collider" : "")}";
                            string targetsObj = $"{(heldingCollider == null ? "Helding Object" : "")}{(heldingCollider == null && playerCollider == null ? " and " : "")}{(playerCollider == null ? "Player Object" : "")}";
                            Debug.LogWarning($"[PressE] Trying to get {targets} but not finding. Does {targetsObj} have a collider to it?");
                        }

                        if (heldingInteractable != null)
                        {
                            CaptureGrabRotationOffset(heldingInteractable);

                            UseAndMaybeConsumeKeys(heldingInteractable);
                            grabDistance = interactable.GrabDistance;

                            if (heldingInteractable.PhysicsGrabMode)
                            {
#if UNITY_6000_0_OR_NEWER
                                heldingInteractable.linearDampingBackup = heldingInteractable.interactableRb.linearDamping;
                                heldingInteractable.angularDampingBackup = heldingInteractable.interactableRb.angularDamping;
#elif UNITY_2022_3_OR_NEWER
                                interactable.linearDampingBackup = interactable.interactableRb.drag;
                                interactable.angularDampingBackup = interactable.interactableRb.angularDrag;
#endif
                            }
                            else interactable.interactableRb.isKinematic = true;

                            if (heldingInteractable.AddOnInteractEvent) heldingInteractable.onInteract.Invoke();

                            heldingInteractable.interactionTimes += 1;

                            SetExclusiveOwner(heldingObject, heldingInteractable);

                            if (heldingInteractable.OverrideGrabRotation)
                            {
                                if (heldingInteractable.UseTransformToPosition)
                                {
                                    Quaternion targetWorld = Quaternion.Euler(heldingInteractable.newGrabRotation);

                                    if (heldingInteractable.TransformGrabInstantFollow)
                                    {
                                        _grabRotOffset = targetWorld;
                                    }
                                    else
                                    {
                                        if (_grabRotOffsetLerp != null) StopCoroutine(_grabRotOffsetLerp);
                                        _grabRotOffsetLerp = StartCoroutine(LerpGrabRotationOffset(0.5f, targetWorld));
                                    }
                                }
                                else
                                {
                                    IEnumerator lerp = RotationLerp(heldingInteractable.transform, 0.5f, Quaternion.Euler(heldingInteractable.newGrabRotation));
                                    StartCoroutine(lerp);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[Press E] Please assign a RigidBody to your Grab interactable");
                    }
                }
            }

            PressEInputBind bind = InputHandler.ResolveBind(Interaction, interactable.OverrideInteractionKey, interactable.NewInteraction);
            bool overrideInteractionDown = InputHandler.GeneralInputDown(bind);
            overrideInteractionDown |= bind.UIButtonDown;

            if (overrideInteractionDown)
            {
                GetGrabObject();
            }
            else if (interactable.HasAutoInteract && !oneTimeInteraction)
            {
                GetGrabObject();
                oneTimeInteraction = true;
            }

            if (isHelding) Invoke(nameof(OneTimeGrabKey), 0.1f);
            Interaction.CanShow = true;
        }

        [HideInInspector] public void ReleaseGrab(Vector3? endPoint = null)
        {
            if (heldingObject == null) return;

            if (heldingInteractable != null)
            {
                Collider heldingCollider = heldingInteractable.Collider;

                if (heldingCollider != null && playerCollider != null) StartCoroutine(RightTimeNormalCollision(heldingCollider, playerCollider));
                else if (heldingCollider != null && playerCollider == null) heldingCollider.enabled = true;
                else
                {
                    string targets = $"{(heldingCollider == null ? "Helding Object Collider" : "")}{(heldingCollider == null && playerCollider == null ? " and " : "")}{(playerCollider == null ? "Player Object Collider" : "")}";
                    string targetsObj = $"{(heldingCollider == null ? "Helding Object" : "")}{(heldingCollider == null && playerCollider == null ? " and " : "")}{(playerCollider == null ? "Player Object" : "")}";
                    Debug.LogWarning($"[PressE] Trying to get {targets} but not finding. Does {targetsObj} have a collider to it?");
                }

                if (heldingInteractable.PhysicsGrabMode)
                {
#if UNITY_6000_0_OR_NEWER
                    heldingInteractable.interactableRb.linearDamping = heldingInteractable.linearDampingBackup;
                    heldingInteractable.interactableRb.angularDamping = heldingInteractable.angularDampingBackup;
#elif UNITY_2022_3_OR_NEWER
                    heldingInteractable.interactableRb.drag = heldingInteractable.linearDampingBackup;
                    heldingInteractable.interactableRb.angularDrag = heldingInteractable.angularDampingBackup;
#endif
                }
                else heldingInteractable.interactableRb.isKinematic = wasKinematic;

                if (heldingInteractable.AddEndEvent) heldingInteractable.onInteractEnd.Invoke();

                ClearExclusiveOwner(heldingObject);
                heldingInteractable.DisableProjection();
                heldingInteractable.JustGotIt = false;
            }

            ClearGrabRotationState();
            grabDropArmed = true;
            if (endPoint != null)
            {
                heldingObject.transform.position = endPoint.Value;

                Rigidbody rb = heldingInteractable.interactableRb;
                rb.position = endPoint.Value;

                rb.MovePosition(endPoint.Value);
            }
            heldingObject = null;
            isHelding = false;
            heldingInteractable.isGrabbing = false;
            heldingCols = null;
            heldingRends = null;
            heldingInteractable = null;
            if (ghostVisualizer != null) DestroyGhost();
        }

        void UpdateIsGrabbing(Vector3 rayStartPos, Vector3 rayDirection, bool canPlaceInside, ObjectDepositData placeInsideData, GrabDeposit insideDeposit)
        {
            void ApplyForce(bool releaseGrab = true)
            {
                var rb = heldingInteractable.interactableRb;
                rb.isKinematic = false;

                Vector3 aimPoint;
                if (heldingInteractable.ThrowUseRaycastAim &&
                    RaycastFiltered(
                        new Ray(CamT.position, CamT.forward),
                        heldingInteractable.ThrowAimDistance,
                        _maskAllButIgnoreRaycast,
                        QueryTriggerInteraction.Ignore,
                        out var hit2))
                {
                    aimPoint = hit2.point;
                }
                else
                {
                    aimPoint = CamT.position + CamT.forward * heldingInteractable.ThrowAimDistance;
                }

                Vector3 fromPos = heldingObject.transform.position;
                Vector3 dir = heldingInteractable.ThrowTowardsAim
                            ? (aimPoint - fromPos).normalized
                            : CamT.forward;

                rb.AddForce(dir * heldingInteractable.throwForce, ForceMode.Impulse);
                if (releaseGrab) ReleaseGrab();
            }

            if (heldingInteractable.CanPlaceGrabDown)
            {
                GrabPlace.CanShow = true;
                if (_placeRotOwner != heldingObject)
                {
                    _placeRotOwner = heldingObject;
                    _placeRotAngleDeg = 0f;

                    _placeBaseRot = heldingObject.transform.rotation;
                    _placeLastAlignNormals = heldingInteractable.AlignNormals;
                }
                else if (_placeLastAlignNormals != heldingInteractable.AlignNormals)
                {
                    _placeLastAlignNormals = heldingInteractable.AlignNormals;
                    _placeBaseRot = heldingObject.transform.rotation;
                    _placeRotAngleDeg = 0f;
                }

                Ray ray = new Ray(rayStartPos, rayDirection);
                Vector3 endPoint = ray.GetPoint(PlacementDistance);

                RaycastHit ghostHit;
                bool placeHit = RaycastFiltered(ray, PlacementDistance, _maskAllButIgnoreRaycast, QueryTriggerInteraction.Ignore, out ghostHit);
                bool InsideDeposit = canPlaceInside && placeInsideData != null && insideDeposit != null;

                if (heldingInteractable.CanRotatePlaceObject)
                {
                    GrabTurnLeft.CanShow = true;
                    GrabTurnRight.CanShow = true;

                    PressEInputBind left = InputHandler.ResolveBind(GrabTurnLeft, heldingInteractable.OverrideTurnKeys, heldingInteractable.NewTurnLeft);
                    PressEInputBind right = InputHandler.ResolveBind(GrabTurnRight, heldingInteractable.OverrideTurnKeys, heldingInteractable.NewTurnRight);
                    bool leftDown = InputHandler.GeneralInputDown(left);
                    bool rightDown = InputHandler.GeneralInputDown(right);
                    bool leftHeld = InputHandler.GeneralInput(left);
                    bool rightHeld = InputHandler.GeneralInput(right);
                    leftDown |= left.UIButtonDown;
                    rightDown |= right.UIButtonDown;
                    leftHeld |= left.UIButtonHeld;
                    rightHeld |= right.UIButtonHeld;
                    if (heldingInteractable.PressDown)
                    {
                        if (leftDown) _placeRotAngleDeg -= heldingInteractable.RotationIncrement;
                        if (rightDown) _placeRotAngleDeg += heldingInteractable.RotationIncrement;
                    }
                    else
                    {
                        float dir = 0f;
                        if (leftHeld) dir -= 1f;
                        if (rightHeld) dir += 1f;

                        if (dir != 0f)
                            _placeRotAngleDeg += dir * heldingInteractable.RotationIncrement * Time.deltaTime;
                    }

                    _placeRotAngleDeg = Mathf.Repeat(_placeRotAngleDeg, 360f);
                }
                else
                {
                    _placeRotAngleDeg = 0f;
                }

                Quaternion baseRot;

                if (placeHit && heldingInteractable.AlignNormals && InsideDeposit == false)
                    baseRot = Quaternion.FromToRotation(Vector3.up, ghostHit.normal);
                else
                    baseRot = _placeBaseRot;

                Quaternion ghostDesiredRot = baseRot;

                if (heldingInteractable.CanRotatePlaceObject && InsideDeposit == false)
                {
                    Vector3 axis = AxisToVector(heldingInteractable.RotationAxis);

                    if (heldingInteractable.CalculateOnLocal) ghostDesiredRot = baseRot * Quaternion.AngleAxis(_placeRotAngleDeg, axis);
                    else ghostDesiredRot = Quaternion.AngleAxis(_placeRotAngleDeg, axis) * baseRot;
                }

                bool canPlace = placeHit;

                if (InsideDeposit == false)
                {
                    if (placeHit) endPoint = GetPlacePointPushedOut(heldingObject, heldingCols, heldingRends, ghostHit.point, ghostHit.normal, ghostDesiredRot);

                    if (canPlace && heldingInteractable.CheckCollisionBeforePlacing)
                    {
                        canPlace = !WouldCollideBeforePlace(heldingObject, heldingCols, heldingRends, endPoint, ghostDesiredRot, ghostHit.collider);
                    }

                    if (canPlace)
                    {
                        if (WouldOverlapBlockedPlacementArea(heldingObject, heldingCols, heldingRends, endPoint, ghostDesiredRot))
                            canPlace = false;
                    }
                }
                else
                {
                    Vector3? pos = heldingInteractable.GetPosition(insideDeposit, insideDeposit.boxCollider, placeInsideData);

                    if (pos != null) endPoint = pos.Value;
                    ghostDesiredRot = insideDeposit.transform.rotation * Quaternion.Euler(placeInsideData.Rotation);
                    canPlace = true;
                }

                if (heldingInteractable.VisualizeObject)
                {
                    if (ghostVisualizer == null)
                    {
                        // Wont be called every frame

                        ghostInteractable = Instantiate(heldingInteractable, position: endPoint, rotation: ghostDesiredRot, parent: null);
                        ghostVisualizer = ghostInteractable.gameObject;
                        ghostVisualizer.SetActive(false);

                        ghostRigidbody = ghostInteractable.interactableRb;
                        if (ghostInteractable != null) Destroy(ghostInteractable);

                        ghostCols = ghostVisualizer.GetComponentsInChildren<Collider>(true);
                        for (int i = 0; i < ghostCols.Length; i++) if (ghostCols[i]) ghostCols[i].enabled = false;

                        ghostRigidbody.isKinematic = true;
                        ghostVisualizer.transform.SetPositionAndRotation(endPoint, ghostDesiredRot);
                    }
                    else
                    {
                        ghostRigidbody.MovePosition(endPoint);
                        ghostRigidbody.MoveRotation(ghostDesiredRot);

                        if (!ghostVisualizer.activeInHierarchy)
                            ghostVisualizer.SetActive(true);

                        if (heldingInteractable.UseManagerMaterials && GrabRightPlacement != null && GrabWrongPlacement != null)
                        {
                            Material desired = canPlace ? GrabRightPlacement : GrabWrongPlacement;

                            if (_ghostLastAppliedMat != desired)
                            {
                                _ghostLastAppliedMat = desired;
                                ghostVisualizer.SetMaterialForObjectAndAllChilds(desired);
                            }
                        }

                    }
                }

                bool placeDown = InputHandler.GeneralInputDown(GrabPlace);
                placeDown |= GrabPlace.UIButtonDown;

                if (placeDown && canPlace && canPlaceInside == false)
                {
                    Rigidbody rb = heldingInteractable.interactableRb;

                    TeleportNoFlash(rb, heldingObject.transform, endPoint, ghostDesiredRot);
                    ReleaseGrab(endPoint);

                    goto POS;
                }
            }

            if (heldingInteractable.isThrowable)
            {
                GrabThrow.CanShow = true;

                bool throwDown = InputHandler.GeneralInputDown(GrabThrow);
                bool throwInput = InputHandler.GeneralInput(GrabThrow);
                bool throwUp = InputHandler.GeneralInputUp(GrabThrow);

                throwDown |= GrabThrow.UIButtonDown;
                throwInput |= GrabThrow.UIButtonHeld;
                throwUp |= GrabThrow.UIButtonUp;

                if (!heldingInteractable.OnlySeeTrajectoryOnForceIncrease)
                {
                    if (!throwDown) heldingInteractable.DrawProjection();
                }

                if (heldingInteractable.TimePressedIncreaseForce)
                {
                    if (throwDown)
                    {
                        heldingInteractable.throwForceBackup = heldingInteractable.throwForce;
                        StartPressingGrabThrowTime = Time.time;
                        oneTimeGrabThrowKeyHandler = true;

                        heldingInteractable.throwForce = heldingInteractable.ForceClamp.x;
                        if (heldingInteractable.OnlySeeTrajectoryOnForceIncrease) heldingInteractable.DrawProjection();
                    }

                    if (throwInput && oneTimeGrabThrowKeyHandler)
                    {
                        float tempo = Mathf.Clamp01((float)((Time.time - StartPressingGrabThrowTime) / heldingInteractable.TimeToReachMaxForce));

                        float t = tempo;
                        if (heldingInteractable.hasThrowAnimationCurve) t = heldingInteractable.ThrowAnimationCurve.Evaluate(tempo);

                        float newForce = heldingInteractable.ForceClamp.x + (heldingInteractable.ForceClamp.y - heldingInteractable.ForceClamp.x) * t;
                        heldingInteractable.throwForce = Mathf.Clamp(newForce, heldingInteractable.ForceClamp.x, heldingInteractable.ForceClamp.y);

                        if (heldingInteractable.OnlySeeTrajectoryOnForceIncrease) heldingInteractable.DrawProjection();
                    }

                    if (throwUp) // Throw Click
                    {
                        var cachedInteractable = heldingInteractable;

                        ApplyForce();

                        StartPressingGrabThrowTime = 0;
                        oneTimeGrabThrowKeyHandler = false;

                        if (cachedInteractable != null) cachedInteractable.throwForce = cachedInteractable.throwForceBackup;

                        goto POS;
                    }
                }
                else
                {
                    if (throwDown)
                    {
                        ApplyForce();
                        goto POS;
                    }
                }
            }

            if (heldingInteractable.GrabUIEnabled)
            {
                if (grabUI == null)
                {
                    if (InteractionUIPrefab != null)
                    {
                        grabUI = Instantiate(heldingInteractable.overrideGrabUIPrefab ? heldingInteractable.GrabUIPrefab : InteractionUIPrefab, transform);
                        if (grabUI.TryGetComponent<UIPrefab>(out var uIPrefab))
                        {
                            grabUIprefab = uIPrefab;
                        }

                        if (grabUIprefab != null && grabUIprefab.hasImage)
                        {
                            grabUIprefab.interactedInteractable = heldingInteractable;
                            if (heldingInteractable.GrabUIControlSprite) grabUIprefab.Image.color = heldingInteractable.GrabUIColor;
                            if (heldingInteractable.GrabUIControlColor) grabUIprefab.Image.sprite = heldingInteractable.GrabUISprite;
                            if (heldingInteractable.GrabUIControlSize) grabUIprefab.Image.rectTransform.sizeDelta = heldingInteractable.GrabUISize;

                            if (heldingInteractable.WillOverrideAnchor == false) grabUIprefab.rectTransform.position = Cam.WorldToScreenPoint(heldingObject.transform.position);
                            else grabUIprefab.rectTransform.position = Cam.WorldToScreenPoint(heldingInteractable.transform.TransformPoint(heldingInteractable.LocalPositionNewAnchor));
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[PressE] Interaction Prefab is null in the manager");
                    }

                }
                else if (isHelding && grabUIprefab != null)
                {
                    if (heldingInteractable.WillOverrideAnchor == false) grabUIprefab.rectTransform.position = Cam.WorldToScreenPoint(heldingObject.transform.position);
                    else grabUIprefab.rectTransform.position = Cam.WorldToScreenPoint(heldingInteractable.transform.TransformPoint(heldingInteractable.LocalPositionNewAnchor));
                }
            }

            if (heldingInteractable.CanPlaceGrabDown == false || (heldingInteractable.CanPlaceGrabDown && heldingInteractable.PreventDropping == false))
            {
                GrabDrop.CanShow = true;
            }

            float scroll = InputHandler.GeneralScrollDelta(inputSystem);
            if (heldingInteractable.ScrolledBasedDistance && scroll != 0f)
            {
                float delta = scroll * heldingInteractable.ScrollMultiplier;
                grabDistance += delta;
                grabDistance = Mathf.Clamp(grabDistance, heldingInteractable.GrabDistanceMinMax.x, heldingInteractable.GrabDistanceMinMax.y);
            }

            if (heldingInteractable.CanRotateGrab)
            {
                GrabRotation.CanShow = true;

                void GrabRotate(Interactable interactable)
                {
                    if (interactable.GrabShowCursorWhenRotating) Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;

                    Vector2 md = InputHandler.GeneralMouseDelta(inputSystem);
                    float xRot = md.x * interactable.RotationSensitivity;
                    float yRot = md.y * interactable.RotationSensitivity;

                    if (interactable.UseTransformToPosition)
                    {
                        if (_grabRotOffsetLerp != null)
                        {
                            StopCoroutine(_grabRotOffsetLerp);
                            _grabRotOffsetLerp = null;
                        }

                        Quaternion deltaLocal =
                            Quaternion.AngleAxis(xRot, -Vector3.up) *
                            Quaternion.AngleAxis(yRot, Vector3.right);

                        _grabRotOffset = deltaLocal * _grabRotOffset;
                    }
                    else
                    {
                        Quaternion newRot =
                            Quaternion.AngleAxis(xRot, -transform.up) *
                            Quaternion.AngleAxis(yRot, transform.right) *
                            heldingObject.transform.localRotation;

                        heldingObject.transform.localRotation = newRot;
                    }
                }

                PressEInputBind bind = InputHandler.ResolveBind(GrabRotation, heldingInteractable.OverrideGrabRotateKeys, heldingInteractable.NewGrabRotation);
                bool rotDown = InputHandler.GeneralInputDown(bind);
                bool rotHeld = InputHandler.GeneralInput(bind);
                bool rotUp = InputHandler.GeneralInputUp(bind);

                rotDown |= bind.UIButtonDown;
                rotHeld |= bind.UIButtonHeld;
                rotUp |= bind.UIButtonUp;

                if (rotDown) blockPlayerMovement.Invoke();
                if (rotHeld) GrabRotate(heldingInteractable);
                if (rotUp)
                {
                    if (heldingInteractable.GrabShowCursorWhenRotating && !heldingInteractable.GrabDontHideCursorOnStop) Cursor.visible = false;
                    unblockPlayerMovement.Invoke();
                }
            }

            bool grabDropInput = InputHandler.GeneralInput(GrabDrop);
            bool grabDropDown = InputHandler.GeneralInputDown(GrabDrop);

            grabDropInput |= GrabDrop.UIButtonHeld;
            grabDropDown |= GrabDrop.UIButtonDown;

            if (!grabDropArmed && !GrabDrop.UIButtonDown && !GrabDrop.UIButtonHeld)
            {
                if (!grabDropInput)
                    grabDropArmed = true;
            }
            else if (grabDropDown && (heldingInteractable.CanPlaceGrabDown == false || heldingInteractable.CanPlaceGrabDown && heldingInteractable.PreventDropping == false))
            {
                ReleaseGrab();
                goto POS;
            }

            bool isToOver = heldingInteractable.OverrideInteractionKey;

            PressEInputBind grabInteraction = InputHandler.ResolveBind(Interaction, isToOver, heldingInteractable.NewInteraction);
            bool grabInteractionDown = InputHandler.GeneralInputDown(grabInteraction);
            grabInteractionDown |= grabInteraction.UIButtonDown;

            if (grabInteractionDown && oneTimeGrabKey)
            {
                HandleGrab(heldingObject.transform, heldingInteractable);
            }

            POS:;
        }

        void UpdateNotGrabbing()
        {
            if (oneTimeGrabKey) oneTimeGrabKey = false;

            if (grabUI != null) Destroy(grabUI);
            if (grabUIprefab != null) Destroy(grabUIprefab);

            GrabDrop.CanShow = false;
            GrabPlace.CanShow = false;
            GrabTurnLeft.CanShow = false;
            GrabTurnRight.CanShow = false;
            GrabThrow.CanShow = false;
            GrabRotation.CanShow = false;
        }
        
        private bool WouldOverlapBlockedPlacementArea(GameObject obj, Collider[] objCols, Renderer[] rends, Vector3 targetPos, Quaternion targetRot)
        {
            if (obj == null) return false;
            if (!TryGetCombinedBounds(obj, objCols, rends, out Bounds b)) return false;

            Vector3 localCenter = obj.transform.InverseTransformPoint(b.center);
            Vector3 centerWorld = targetPos + (targetRot * Vector3.Scale(localCenter, obj.transform.lossyScale));
            Vector3 halfExtents = b.extents * 0.97f;

            int count = Physics.OverlapBoxNonAlloc(
                centerWorld,
                halfExtents,
                _placeOverlap,
                targetRot,
                ~0,
                QueryTriggerInteraction.Collide
            );

            if (count <= 0) return false;

            Transform root = obj.transform;
            Quaternion invRootRot = Quaternion.Inverse(root.rotation);
            Matrix4x4 rootM = Matrix4x4.TRS(targetPos, targetRot, root.lossyScale);

            const float FALLBACK = 0.0005f;

            for (int i = 0; i < count; i++)
            {
                var hitCol = _placeOverlap[i];
                if (hitCol == null) continue;

                if (hitCol.transform.IsChildOf(root)) continue;

                if (hitCol.TryGetComponent<BlockPlacementArea>(out var block))
                {
                    if (block.enabled == false || block.Block == false) continue;

                    var blockCol = block.boxCollider;
                    if (blockCol == null) continue;

                    Vector3 blockPos = blockCol.transform.position;
                    Quaternion blockRot = blockCol.transform.rotation;

                    for (int j = 0; j < objCols.Length; j++)
                    {
                        Collider c = objCols[j];
                        if (c == null || c.isTrigger) continue;

                        Transform ct = c.transform;

                        Vector3 localPosInRoot = root.InverseTransformPoint(ct.position);
                        Quaternion localRotInRoot = invRootRot * ct.rotation;

                        Vector3 colPos = rootM.MultiplyPoint3x4(localPosInRoot);
                        Quaternion colRot = targetRot * localRotInRoot;

                        if (Physics.ComputePenetration(
                                c, colPos, colRot,
                                blockCol, blockPos, blockRot,
                                out _, out float dist))
                        {
                            if (dist > FALLBACK)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool WouldCollideBeforePlace(GameObject obj, Collider[] cols, Renderer[] rends, Vector3 targetPos, Quaternion targetRot, Collider surfaceHit)
        {
            if (!TryGetCombinedBounds(obj, cols, rends, out Bounds b)) return false;

            Vector3 localCenter = obj.transform.InverseTransformPoint(b.center);
            Vector3 centerWorld = targetPos + (targetRot * Vector3.Scale(localCenter, obj.transform.lossyScale));

            Vector3 halfExtents = b.extents * 0.97f;

            int count = Physics.OverlapBoxNonAlloc(
                centerWorld,
                halfExtents,
                _placeOverlap,
                targetRot,
                _maskAllButIgnoreRaycast,
                QueryTriggerInteraction.Ignore
            );

            bool surfaceIsPlayer = playerObject != null && surfaceHit != null && surfaceHit.transform.IsChildOf(playerObject.transform);

            for (int i = 0; i < count; i++)
            {
                var c = _placeOverlap[i];
                if (c == null) continue;

                if (c.transform.IsChildOf(obj.transform)) continue;

                if (!surfaceIsPlayer && surfaceHit != null && c == surfaceHit) continue;

                return true;
            }

            return false;
        }

        private bool RaycastFiltered(Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction qti, out RaycastHit bestHit)
        {
            bestHit = default;

            int count = Physics.RaycastNonAlloc(ray, _rayHits, maxDistance, layerMask, qti);
            if (count <= 0) return false;

            bool found = false;
            float bestDist = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                var h = _rayHits[i];
                if (h.collider == null) continue;

                Transform ht = h.collider.transform;

                if (isHelding && ht.IsChildOf(heldingObject.transform)) continue;

                if (ghostVisualizer != null && ht.IsChildOf(ghostVisualizer.transform)) continue;

                if (playerObject != null && ht.IsChildOf(playerObject.transform)) continue;

                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    bestHit = h;
                    found = true;
                }
            }

            return found;
        }

        private bool TryGetCombinedBounds(GameObject obj, Collider[] cols, Renderer[] rends, out Bounds bounds)
        {
            bounds = default;
            bool has = false;

            for (int i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                if (c == null || c.isTrigger) continue;

                if (!has) { bounds = c.bounds; has = true; }
                else bounds.Encapsulate(c.bounds);
            }

            if (has) return true;

            for (int i = 0; i < rends.Length; i++)
            {
                var r = rends[i];
                if (r == null) continue;

                if (!has) { bounds = r.bounds; has = true; }
                else bounds.Encapsulate(r.bounds);
            }

            return has;
        }

        private Vector3 GetPlacePointPushedOut(GameObject obj, Collider[] cols, Renderer[] rends, Vector3 hitPoint, Vector3 surfaceNormal, Quaternion targetRootRot)
        {
            if (obj == null) return hitPoint;

            Vector3 n = surfaceNormal;
            if (n.sqrMagnitude < 1e-8f) return hitPoint;
            n.Normalize();

            float probeDist = 2f;
            if (TryGetCombinedBounds(obj, cols, rends, out var b))
                probeDist = Mathf.Max(0.25f, b.extents.magnitude * 4f + 0.25f);

            Vector3 probePoint = hitPoint - n * probeDist;

            Transform root = obj.transform;

            Matrix4x4 rootM = Matrix4x4.TRS(hitPoint, targetRootRot, root.lossyScale);

            Quaternion invRootRot = Quaternion.Inverse(root.rotation);

            float maxPush = 0f;
            bool hasCollider = false;

            for (int i = 0; i < cols.Length; i++)
            {
                Collider c = cols[i];
                if (c == null || c.isTrigger) continue;

                hasCollider = true;

                Transform ct = c.transform;

                Vector3 localPosInRoot = root.InverseTransformPoint(ct.position);
                Quaternion localRotInRoot = invRootRot * ct.rotation;

                Vector3 colPos = rootM.MultiplyPoint3x4(localPosInRoot);
                Quaternion colRot = targetRootRot * localRotInRoot;

                Vector3 closest = Physics.ClosestPoint(probePoint, c, colPos, colRot);

                float push = Vector3.Dot(n, hitPoint - closest);
                if (push > maxPush) maxPush = push;
            }

            if (!hasCollider) return hitPoint;

            return hitPoint + n * Mathf.Max(0f, maxPush);
        }
        
        private Vector3 AxisToVector(Axis axis)
        {
            if (axis == Axis.Up) return Vector3.up;
            else if (axis == Axis.Right) return Vector3.right;
            else if (axis == Axis.Forward) return Vector3.forward;

            return Vector3.up;
        }

        private Vector3 ComputeGrabTarget(Interactable interactable)
        {
            Vector3 baseTarget = CamT.position + CamT.forward * grabDistance;

            if (interactable.UseTransformToPosition)
            {
                Transform grabTransform = ItemPlayerGrabTransform;
                if (interactable.OverrideManagerTransform) grabTransform = interactable.GrabTransform;

                float extraDistance = (grabDistance < 0f) ? grabDistance : Mathf.Max(0f, grabDistance - 1f);

                baseTarget = grabTransform.position + CamT.forward * extraDistance;
            }

            Vector3 offsetWorld = (interactable != null)
                                ? CamT.TransformVector(interactable.GrabOffset)
                                : Vector3.zero;

            return baseTarget + offsetWorld;
        }

        private Quaternion GetReferenceRotation(Interactable interactable)
        {
            var t = _grabRefTransform != null ? _grabRefTransform : ResolveGrabReferenceTransform(interactable);
            return t != null ? t.rotation : Quaternion.identity;
        }

        private Transform ResolveGrabReferenceTransform(Interactable interactable)
        {
            if (interactable != null && interactable.UseTransformToPosition)
            {
                Transform t = ItemPlayerGrabTransform;
                if (interactable.OverrideManagerTransform) t = interactable.GrabTransform;

                if (t != null) return t;
            }

            return Cam != null ? CamT : transform;
        }

        [HideInInspector] public void SetInterpolationNoneUntilNextFixed(Rigidbody rb)
        {
            if (rb == null) return;

            if (!_interpRestoreCache.ContainsKey(rb))
            {
                _interpRestoreCache.Add(rb, rb.interpolation);
                StartCoroutine(RestoreInterpolationNextFixed(rb));
            }

            rb.interpolation = RigidbodyInterpolation.None;
        }

        private void TeleportNoFlash(Rigidbody rb, Transform tr, Vector3 pos, Quaternion rot)
        {
            if (tr != null) tr.SetPositionAndRotation(pos, rot);
            if (rb == null) return;

            SetInterpolationNoneUntilNextFixed(rb);

            rb.position = pos;
            rb.rotation = rot;

            Physics.SyncTransforms();
        }

        private void CaptureGrabRotationOffset(Interactable interactable)
        {
            _grabRefTransform = ResolveGrabReferenceTransform(interactable);

            Quaternion refRot = GetReferenceRotation(interactable);
            _grabRotOffset = Quaternion.Inverse(refRot) * heldingObject.transform.rotation;
        }

        private void ApplyRotationPD(Rigidbody rb, Quaternion targetWorldRot, float stiffness, float damping)
        {
            Quaternion q = targetWorldRot * Quaternion.Inverse(rb.rotation);

            q.ToAngleAxis(out float angleDeg, out Vector3 axis);
            if (float.IsNaN(axis.x)) return;

            if (angleDeg > 180f) angleDeg -= 360f;

            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector3 torque =
                axis * (angleRad * stiffness)
                - rb.angularVelocity * damping;

            rb.AddTorque(torque, ForceMode.Acceleration);
        }

        private void ApplyGrabFollowRotation(Interactable interactable)
        {
            if (interactable == null || !interactable.UseTransformToPosition) return;
            if (heldingObject == null) return;

            var rb = heldingInteractable.interactableRb;
            if (rb == null) return;

            Quaternion refRot = GetReferenceRotation(interactable);
            Quaternion desired = refRot * _grabRotOffset;

            if (interactable.PhysicsGrabMode)
            {
                ApplyRotationPD(rb, desired, stiffness: 35f, damping: 6f);
            }
            else
            {
                if (interactable.TransformGrabInstantFollow)
                {
                    rb.MoveRotation(desired);
                }
                else
                {
                    float a = Helper.ExpLerp01(interactable.TransformGrabRotationSharpness, Time.fixedDeltaTime);
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, desired, a));
                }
            }
        }

        private void ClearGrabRotationState()
        {
            if (_grabRotOffsetLerp != null)
            {
                StopCoroutine(_grabRotOffsetLerp);
                _grabRotOffsetLerp = null;
            }

            _grabRefTransform = null;
            _grabRotOffset = Quaternion.identity;
            _placeRotOwner = null;
            _placeRotAngleDeg = 0f;
            _placeBaseRot = Quaternion.identity;
            _placeLastAlignNormals = false;
        }

        private void DestroyGhost()
        {
            ghostInteractable = null;
            ghostRigidbody = null;
            ghostCols = null;
            Destroy(ghostVisualizer);
        }

        private void OneTimeGrabKey() => oneTimeGrabKey = true;

        IEnumerator RightTimeNormalCollision(Collider helding, Collider player)
        {
            while (Physics.ComputePenetration(helding, helding.transform.position, helding.transform.rotation,
                                                          player, player.transform.position, player.transform.rotation,
                                                          out var dir, out var dis))
            {
                yield return null;
            }

            Physics.IgnoreCollision(helding, player, false);
        }

        IEnumerator RotationLerp(Transform objectToMove, float duration, Quaternion targetRot)
        {
            interactionBlockInteraction = true;
            Quaternion startRot = objectToMove.localRotation;
            float timeElapsed = 0;

            while (timeElapsed < duration)
            {
                float t = timeElapsed / duration;
                objectToMove.localRotation = Quaternion.Lerp(startRot, targetRot, t);
                timeElapsed += Time.deltaTime;

                yield return null;
            }

            objectToMove.localRotation = targetRot;
            interactionBlockInteraction = false;
        }

        IEnumerator LerpGrabRotationOffset(float duration, Quaternion targetOffset)
        {
            Quaternion start = _grabRotOffset;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, duration);
                _grabRotOffset = Quaternion.Slerp(start, targetOffset, t);
                yield return null;
            }

            _grabRotOffset = targetOffset;
            _grabRotOffsetLerp = null;
        }

        IEnumerator RestoreInterpolationNextFixed(Rigidbody rb)
        {
            yield return new WaitForFixedUpdate();

            if (rb == null) yield break;

            if (_interpRestoreCache.TryGetValue(rb, out var prev))
            {
                rb.interpolation = prev;
                _interpRestoreCache.Remove(rb);
            }
        }

    }
}