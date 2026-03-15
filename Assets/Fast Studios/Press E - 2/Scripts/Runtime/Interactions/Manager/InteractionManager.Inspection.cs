using UnityEngine;
using System;
using System.Collections;

namespace FastStudios
{
    public partial class InteractionManager // INSPECTION
    {
        #region Inspection Inputs

        public PressEInputBind InspectionRotation = new PressEInputBind
        {
            InputMethod = InputMethod.Mouse,
            Key = KeyCode.LeftAlt,
            MouseButton = MouseMethod.Left
        };

        public PressEInputBind InspectionDetailsImage = new PressEInputBind
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.H,
            MouseButton = MouseMethod.Left
        };

        public PressEInputBind InspectionDetailsText = new PressEInputBind
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.H,
            MouseButton = MouseMethod.Left
        };

        #endregion

        #region Inspection
        private GameObject inspectionObject;
        [HideInInspector] public Interactable inspectionInteractable;
        [HideInInspector] public bool isInspecting;
        private bool inspectionKeyConsumedThisFrame;
        bool releasingInspection = false;

        private Vector3 originalCamPos;
        private Quaternion originalCamRot;
        private Transform originalCamParent;
        private Vector3 originalCamLocalPos;
        private Quaternion originalCamLocalRot;
        private bool hasOriginalCamLocalPose;
        private bool inspectionCamDetached;

        private Vector3 inspectionFocusWorld;
        private Vector3 inspectionCamCenterPos;
        private Vector3 inspectionBaseForward;
        private Vector3 inspectionBaseUp;
        private Vector3 inspectionBaseRight;
        private Vector2 inspectionMarginN;
        private Vector2 inspectionMarginNVel;
        private Vector2 inspectionPanLocal;
        private Vector2 inspectionPanLocalVel;
        private float inspectionDist;
        private Quaternion inspectionCamCenterRot;
        private bool inspectionRbWasKinematic;

        private Collider inspectionColliderCached;
        private bool inspectionIgnoredCollision;

        private bool cursorVisibleBeforeInspection;
        private CursorLockMode cursorLockBeforeInspection;

        #endregion

        void HandleInspection(GameObject hitObj, Interactable interactable)
        {
            void Interact()
            {
                if (!isInspecting)
                {
                    // Just called one frame
                    blockPlayerMovement.Invoke();

                    inspectionObject = hitObj;
                    isInspecting = true;
                    inspectionInteractable = interactable;
                    if (inspectionDetails == null)
                    {
                        inspectionDetails = Instantiate(inspectionInteractable.overrideInspectionPrefab && inspectionInteractable.InspectionPrefab != null ? inspectionInteractable.InspectionPrefab : InspectionPrefab, transform);
                        inspectionDetailsUIPrefab = inspectionDetails.GetComponent<UIPrefab>();
                    }
                    if (inspectionDetailsUIPrefab != null)
                    {
                        inspectionDetailsUIPrefab.interactedInteractable = inspectionInteractable;
                        inspectionDetailsUIPrefab.DeactivateAllChildren();
                    }

                    originalPos = inspectionObject.transform.position;
                    originalRot = inspectionObject.transform.localRotation;

                    originalCamPos = CamT.position;
                    originalCamRot = CamT.rotation;

                    originalCamParent = CamT != null ? CamT.parent : null;

                    if (originalCamParent != null)
                    {
                        originalCamLocalPos = CamT.localPosition;
                        originalCamLocalRot = CamT.localRotation;
                        hasOriginalCamLocalPose = true;
                    }
                    else
                    {
                        hasOriginalCamLocalPose = false;
                    }

                    cursorVisibleBeforeInspection = Cursor.visible;
                    cursorLockBeforeInspection = Cursor.lockState;

                    inspectionColliderCached = inspectionInteractable.Collider;
                    inspectionIgnoredCollision = false;

                    if (inspectionInteractable.InspectionViewMode == InspectionViewMode.MoveObjectToCamera)
                    {
                        if (inspectionColliderCached != null && playerCollider != null)
                        {
                            Physics.IgnoreCollision(inspectionColliderCached, playerCollider, true);
                            inspectionIgnoredCollision = true;
                        }
                        else if (inspectionColliderCached != null && playerCollider == null)
                        {
                            inspectionColliderCached.enabled = true;
                        }
                        else
                        {
                            string targets = $"{(inspectionColliderCached == null ? "Inspection Object Collider" : "")}{(inspectionColliderCached == null && playerCollider == null ? " and " : "")}{(playerCollider == null ? "Player Object Collider" : "")}";
                            string targetsObj = $"{(inspectionColliderCached == null ? "Inspection Object" : "")}{(inspectionColliderCached == null && playerCollider == null ? " and " : "")}{(playerCollider == null ? "Player Object" : "")}";
                            Debug.LogWarning($"[PressE] Trying to get {targets} but not finding. Does {targetsObj} have a collider to it?");
                        }
                    }

                    inspectionRbWasKinematic = false;
                    if (inspectionInteractable.interactableRb != null)
                    {
                        inspectionRbWasKinematic = inspectionInteractable.interactableRb.isKinematic;
                        inspectionInteractable.interactableRb.isKinematic = true;
                    }

                    if (inspectionInteractable != null)
                    {
                        if (inspectionInteractable.InspectionViewMode == InspectionViewMode.MoveObjectToCamera)
                        {
                            Vector3 targetPos = CamT.position + CamT.forward * inspectionInteractable.InspectionDistance;

                            Quaternion targetRot = Quaternion.identity;
                            if (inspectionInteractable.overrideInspectionQuaternion)
                                targetRot = inspectionInteractable.inspectionQuaternionOverride;

                            StartCoroutine(InspectionMoveObject(
                                objectToMove: inspectionObject.transform,
                                duration: inspectionInteractable.TimeToTakeObject,
                                animCurve: inspectionInteractable.TakeObjectAnimationCurve,
                                targetPos: targetPos,
                                targetRot: targetRot,
                                action: null
                            ));
                        }
                        else // MoveCameraToObject
                        {
                            InspectorRuntimeDeadzonePreview.ComputeInspectionCenterPose(
                                inspectionInteractable,
                                inspectionObject.transform,
                                originalCamPos,
                                originalCamRot,
                                out inspectionFocusWorld,
                                out inspectionDist,
                                out inspectionCamCenterPos,
                                out inspectionCamCenterRot,
                                out inspectionBaseForward,
                                out inspectionBaseUp,
                                out inspectionBaseRight
                            );

                            inspectionCamDetached = false;

                            if (CamT != null && originalCamParent != null)
                            {
                                CamT.SetParent(null, true);
                                inspectionCamDetached = true;
                            }

                            StartCoroutine(InspectionMoveCamera(
                                cam: CamT,
                                duration: inspectionInteractable.TimeToTakeObject,
                                animCurve: inspectionInteractable.TakeObjectAnimationCurve,
                                targetPos: inspectionCamCenterPos,
                                targetRot: inspectionCamCenterRot,
                                action: null
                            ));
                        }

                        if (inspectionInteractable.AddOnInteractEvent) inspectionInteractable.onInteract.Invoke();
                        inspectionInteractable.interactionTimes += 1;
                    }

                }
                else
                {
                    if (releasingInspection == false && inspectionInteractable != null) ForceReleaseInspection(inspectionInteractable);
                }
            }

            PressEInputBind bind = InputHandler.ResolveBind(Interaction, interactable.OverrideInteractionKey, interactable.NewInteraction);
            bool interactionDown = InputHandler.GeneralInputDown(bind);
            interactionDown |= bind.UIButtonDown;

            if (interactionDown)
            {
                Interact();
                inspectionKeyConsumedThisFrame = true;
            }
            else if (interactable.HasAutoInteract && !oneTimeInteraction)
            {
                Interact();
                oneTimeInteraction = true;
            }

            Interaction.CanShow = true;
        }

        void CheckForUI(Interactable interactable)
        {
            if (inspectionDetails != null && inspectionDetailsUIPrefab != null && !inspectionDetailsUIPrefab.CloseUI && !interactionBlockInteraction)
            {
                inspectionDetailsUIPrefab.OpenInspectionUI(interactable);
            }
        }

        void ForceReleaseInspection(Interactable interactable)
        {
            if (interactable == null || releasingInspection) return;

            releasingInspection = true;

            if (inspectionDetails != null) Destroy(inspectionDetails);
            if (inspectionDetailsUIPrefab != null) Destroy(inspectionDetailsUIPrefab);

            inspectionDetails = null;
            inspectionDetailsUIPrefab = null;

            Action onEnd = OnEnd;

            if (interactable.InspectionViewMode == InspectionViewMode.MoveObjectToCamera)
            {
                StartCoroutine(InspectionMoveObject(
                    objectToMove: inspectionObject.transform,
                    duration: interactable.TimeToTakeObject,
                    animCurve: interactable.TakeObjectAnimationCurve,
                    targetPos: originalPos,
                    targetRot: originalRot,
                    action: onEnd
                ));
            }
            else
            {
                if (inspectionCamDetached && CamT != null && hasOriginalCamLocalPose && originalCamParent != null)
                {
                    CamT.SetParent(originalCamParent, true);

                    inspectionCamDetached = false;

                    StartCoroutine(InspectionMoveCameraLocal(
                        cam: CamT,
                        duration: interactable.TimeToTakeObject,
                        animCurve: interactable.TakeObjectAnimationCurve,
                        targetLocalPos: originalCamLocalPos,
                        targetLocalRot: originalCamLocalRot,
                        action: onEnd
                    ));
                }
                else
                {
                    Vector3 returnPos = originalCamPos;
                    Quaternion returnRot = originalCamRot;

                    if (hasOriginalCamLocalPose && originalCamParent != null)
                    {
                        returnPos = originalCamParent.TransformPoint(originalCamLocalPos);
                        returnRot = originalCamParent.rotation * originalCamLocalRot;
                    }

                    StartCoroutine(InspectionMoveCamera(
                        cam: CamT,
                        duration: interactable.TimeToTakeObject,
                        animCurve: interactable.TakeObjectAnimationCurve,
                        targetPos: returnPos,
                        targetRot: returnRot,
                        action: onEnd
                    ));
                }
            }

            void OnEnd()
            {
                if (inspectionIgnoredCollision && inspectionColliderCached != null && playerCollider != null)
                    Physics.IgnoreCollision(inspectionColliderCached, playerCollider, false);

                if (interactable.interactableRb != null)
                    interactable.interactableRb.isKinematic = inspectionRbWasKinematic;

                Cursor.visible = cursorVisibleBeforeInspection;
                Cursor.lockState = cursorLockBeforeInspection;

                if (inspectionCamDetached && CamT != null && originalCamParent != null && hasOriginalCamLocalPose)
                {
                    CamT.SetParent(originalCamParent, true);
                    CamT.localPosition = originalCamLocalPos;
                    CamT.localRotation = originalCamLocalRot;
                }

                inspectionCamDetached = false;

                unblockPlayerMovement.Invoke();

                if (interactable.AddEndEvent) interactable.onInteractEnd.Invoke();

                inspectionObject = null;
                isInspecting = false;
                inspectionInteractable = null;
                releasingInspection = false;
            }
        }

        void UpdateIsInspecting()
        {
            CheckForUI(inspectionInteractable);

            if (inspectionInteractable.InspectionCanRotate && inspectionInteractable.InspectionViewMode == InspectionViewMode.MoveObjectToCamera)
            {
                InspectionRotation.CanShow = true;

                bool inspectionHeld = InputHandler.GeneralInput(InspectionRotation, inspectionInteractable.OverrideRotationKey, inspectionInteractable.NewInspectionRotation);
                bool inspectionUp = InputHandler.GeneralInputUp(InspectionRotation, inspectionInteractable.OverrideRotationKey, inspectionInteractable.NewInspectionRotation);
                inspectionHeld |= InspectionRotation.UIButtonHeld;
                inspectionUp |= InspectionRotation.UIButtonUp;

                if (inspectionHeld)
                {
                    if (inspectionInteractable.ShowCursorWhenRotating) Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;

                    Vector2 md = InputHandler.GeneralMouseDelta(inputSystem);
                    float xRot = md.x * inspectionInteractable.InspectionRotationSens;
                    float yRot = md.y * inspectionInteractable.InspectionRotationSens;

                    inspectionObject.transform.localRotation = Quaternion.AngleAxis(xRot, -transform.up) * Quaternion.AngleAxis(yRot, transform.right) * inspectionObject.transform.localRotation;
                }

                if (inspectionUp) Cursor.visible = false;
            }
            else if (inspectionInteractable.InspectionViewMode == InspectionViewMode.MoveCameraToObject)
            {
                if (inspectionInteractable.InspectionHasMargin && !interactionBlockInteraction && !releasingInspection)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;

                    Vector2 mp;
                    if (!InputHandler.TryGetMouseScreenPosition(out mp))
                        mp = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

                    float xRaw = Screen.width > 0 ? ((mp.x / Screen.width) - 0.5f) * 2f : 0f;
                    float yRaw = Screen.height > 0 ? ((mp.y / Screen.height) - 0.5f) * 2f : 0f;
                    xRaw = Mathf.Clamp(xRaw, -1f, 1f);
                    yRaw = Mathf.Clamp(yRaw, -1f, 1f);

                    float dead = Mathf.Clamp01(inspectionInteractable.InspectionMarginDeadZone);
                    float feather = Mathf.Clamp01(inspectionInteractable.InspectionMarginFeather);

                    float leftM = Mathf.Abs(inspectionInteractable.InspectionLeftMargin);
                    float rightM = Mathf.Abs(inspectionInteractable.InspectionRightMargin);
                    float topM = Mathf.Abs(inspectionInteractable.InspectionTopMargin);
                    float bottomM = Mathf.Abs(inspectionInteractable.InspectionBottomMargin);

                    InspectorRuntimeDeadzonePreview.StepMarginCamera(
                        CamT,
                        new Vector2(xRaw, yRaw),
                        dead,
                        feather,
                        ref inspectionMarginN,
                        ref inspectionMarginNVel,
                        ref inspectionPanLocal,
                        ref inspectionPanLocalVel,
                        inspectionCamCenterPos,
                        inspectionCamCenterRot,
                        inspectionBaseRight,
                        inspectionBaseUp,
                        leftM, rightM, topM, bottomM,
                        inspectionInteractable.InspectionRotationOffsetOnEdge,
                        inspectionInteractable.InspectionLeftOffset,
                        inspectionInteractable.InspectionRightOffset,
                        inspectionInteractable.InspectionTopOffset,
                        inspectionInteractable.InspectionBottomOffset,
                        Time.deltaTime
                    );
                }
            }

            bool isToOver = inspectionInteractable.OverrideInteractionKey;
            PressEInputBind interactionBind = InputHandler.ResolveBind(Interaction, isToOver, inspectionInteractable.NewInteraction);
            bool interactionDownWithOverride = InputHandler.GeneralInputDown(interactionBind);
            interactionDownWithOverride |= interactionBind.UIButtonDown;

            if (interactionDownWithOverride && !inspectionKeyConsumedThisFrame && !interactionBlockInteraction)
            {
                if (releasingInspection == false) ForceReleaseInspection(inspectionInteractable);
            }
        }

        void UpdateNotInspecting()
        {
            InspectionDetailsImage.CanShow = false;
            InspectionDetailsText.CanShow = false;
            InspectionRotation.CanShow = false;
        }

        private float ApplyDeadZone(float v, float deadZone)
        {
            float a = Mathf.Abs(v);
            if (a <= deadZone) return 0f;

            float sign = Mathf.Sign(v);
            float t = (a - deadZone) / Mathf.Max(0.0001f, 1f - deadZone);
            return sign * Mathf.Clamp01(t);
        }

        private float ApplySoftCurve(float v)
        {
            float a = Mathf.Abs(v);
            a = a * a * (3f - 2f * a);
            return Mathf.Sign(v) * a;
        }

        private Vector3 GetInspectionFocusWorld(Interactable it, Transform inspectedObj)
        {
            if (it.InspectionTargetType == InspectionNavigationTargetType.Transform && it.InspectionTargetTransform != null)
                return it.InspectionTargetTransform.position;

            return inspectedObj.TransformPoint(it.InspectionTargetPosition);
        }

        IEnumerator InspectionMoveObject(Transform objectToMove, float duration, AnimationCurve animCurve, Vector3 targetPos, Quaternion targetRot, Action action)
        {
            interactionBlockInteraction = true;

            if (objectToMove == null)
            {
                interactionBlockInteraction = false;
                action?.Invoke();
                yield break;
            }

            if (duration <= 0f)
            {
                objectToMove.position = targetPos;
                objectToMove.localRotation = Helper.SanitizeQuaternion(targetRot);
                interactionBlockInteraction = false;
                action?.Invoke();
                yield break;
            }

            Vector3 startPos = objectToMove.position;
            Quaternion startRot = Helper.SanitizeQuaternion(objectToMove.localRotation);
            Quaternion endRot = Helper.SanitizeQuaternion(targetRot);

            float timeElapsed = 0f;
            float invDuration = 1f / duration;

            while (timeElapsed < duration)
            {
                float t = Mathf.Clamp01(timeElapsed * invDuration);

                float time = t;
                if (animCurve != null)
                    time = animCurve.Evaluate(t);

                if (float.IsNaN(time) || float.IsInfinity(time))
                    time = 0f;

                time = Mathf.Clamp01(time);

                objectToMove.position = Vector3.Lerp(startPos, targetPos, time);
                objectToMove.localRotation = Quaternion.Slerp(startRot, endRot, time);

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            objectToMove.position = targetPos;
            objectToMove.localRotation = endRot;
            interactionBlockInteraction = false;

            action?.Invoke();
        }

        IEnumerator InspectionMoveCamera(Transform cam, float duration, AnimationCurve animCurve, Vector3 targetPos, Quaternion targetRot, Action action)
        {
            interactionBlockInteraction = true;

            if (cam == null)
            {
                interactionBlockInteraction = false;
                action?.Invoke();
                yield break;
            }

            if (duration <= 0f)
            {
                cam.position = targetPos;
                cam.rotation = Helper.SanitizeQuaternion(targetRot);
                interactionBlockInteraction = false;
                action?.Invoke();
                yield break;
            }

            Vector3 startPos = cam.position;
            Quaternion startRot = Helper.SanitizeQuaternion(cam.rotation);
            Quaternion endRot = Helper.SanitizeQuaternion(targetRot);

            float timeElapsed = 0f;
            float invDuration = 1f / duration;

            while (timeElapsed < duration)
            {
                float t = Mathf.Clamp01(timeElapsed * invDuration);

                float time = t;
                if (animCurve != null) time = animCurve.Evaluate(t);
                if (float.IsNaN(time) || float.IsInfinity(time)) time = 0f;
                time = Mathf.Clamp01(time);

                cam.position = Vector3.Lerp(startPos, targetPos, time);
                cam.rotation = Quaternion.Slerp(startRot, endRot, time);

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            cam.position = targetPos;
            cam.rotation = endRot;

            interactionBlockInteraction = false;
            action?.Invoke();
        }

        IEnumerator InspectionMoveCameraLocal(Transform cam, float duration, AnimationCurve animCurve, Vector3 targetLocalPos, Quaternion targetLocalRot, Action action)
        {
            interactionBlockInteraction = true;

            if (cam == null)
            {
                interactionBlockInteraction = false;
                action?.Invoke();
                yield break;
            }

            Quaternion endRot = Helper.SanitizeQuaternion(targetLocalRot);

            if (duration <= 0f)
            {
                cam.localPosition = targetLocalPos;
                cam.localRotation = endRot;
                interactionBlockInteraction = false;
                action?.Invoke();
                yield break;
            }

            Vector3 startPos = cam.localPosition;
            Quaternion startRot = Helper.SanitizeQuaternion(cam.localRotation);

            float timeElapsed = 0f;
            float invDuration = 1f / duration;

            while (timeElapsed < duration)
            {
                float t = Mathf.Clamp01(timeElapsed * invDuration);

                float time = t;
                if (animCurve != null) time = animCurve.Evaluate(t);
                if (float.IsNaN(time) || float.IsInfinity(time)) time = 0f;
                time = Mathf.Clamp01(time);

                cam.localPosition = Vector3.Lerp(startPos, targetLocalPos, time);
                cam.localRotation = Quaternion.Slerp(startRot, endRot, time);

                timeElapsed += Time.deltaTime;
                yield return null;
            }

            cam.localPosition = targetLocalPos;
            cam.localRotation = endRot;

            interactionBlockInteraction = false;
            action?.Invoke();
        }

    }
}