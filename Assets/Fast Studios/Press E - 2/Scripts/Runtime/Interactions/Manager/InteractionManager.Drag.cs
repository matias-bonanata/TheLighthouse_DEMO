using UnityEngine;

namespace FastStudios
{
    public partial class InteractionManager
    {
        #region Drag
        private GameObject draggingObject;
        private Interactable draggingInteractable;
        [HideInInspector] public bool isDragging;
        private bool dragKeyConsumedThisFrame;
        #endregion
        void HandleDrag(Transform hitTransform, Interactable inter)
        {
            void ToggleDrag()
            {
                if (draggingObject == null)
                {
                    if (inter.DragLocked) return;

                    if (inter.DragHasMaxDistance)
                    {
                        Vector3 center = inter.WillOverrideAnchor
                            ? inter.DragGetOriginWorld()
                            : inter.DragGetAnchorWorldNow();

                        Vector3 refPos = playerObject.transform.position;

                        if (Vector3.Distance(refPos, center) > inter.DragMaxDistance)
                        {
                            return;
                        }
                    }

                    if (inter.interactableRb != null)
                    {
                        UseAndMaybeConsumeKeys(inter);
                        draggingObject = hitTransform.gameObject;
                        isDragging = true;
                        draggingInteractable = inter;

                        if (draggingInteractable.ConsiderHitPlace && _hasLastRaycastHit)
                        {
                            Transform hitSpace = _lastRaycastHit.rigidbody != null ?
                                                 _lastRaycastHit.rigidbody.transform :
                                                    (_lastRaycastHit.collider != null ?
                                                    _lastRaycastHit.collider.transform :
                                                    null);

                            draggingInteractable.BeginDragAtHit(_lastRaycastHit.point, hitSpace);
                        }
                        else
                            draggingInteractable.DragBegin();

                        if (draggingInteractable.AddOnInteractEvent)
                            draggingInteractable.onInteract.Invoke();

                        draggingInteractable.interactionTimes += 1;

                        SetExclusiveOwner(draggingObject, draggingInteractable);
                    }
                }
                else
                {
                    if (draggingInteractable != null)
                    {
                        if (draggingInteractable.AddEndEvent)
                            draggingInteractable.onInteractEnd.Invoke();

                        draggingInteractable.DragEnd();

                        ClearExclusiveOwner(draggingObject);
                    }

                    draggingInteractable = null;
                    draggingObject = null;
                    isDragging = false;
                }
            }

            PressEInputBind bind = InputHandler.ResolveBind(Interaction, inter.OverrideInteractionKey, inter.NewInteraction);
            bool InteractionDown = InputHandler.GeneralInputDown(bind);
            InteractionDown |= bind.UIButtonDown;

            if (InteractionDown)
            {
                dragKeyConsumedThisFrame = true;
                ToggleDrag();
            }
            else if (inter.HasAutoInteract && !oneTimeInteraction)
            {
                ToggleDrag();
                oneTimeInteraction = true;
            }
        }

        void ForceReleaseDrag()
        {
            if (draggingObject == null || draggingInteractable == null) return;

            ClearExclusiveOwner(draggingObject);

            if (playerObject != null && draggingInteractable.Collider != null && playerCollider != null)
            {
                Collider col = draggingInteractable.Collider;

                if (col != null && playerCollider != null) Physics.IgnoreCollision(col, playerCollider, false);
                else
                {
                    string targets = $"{(col == null ? "Dragging Object Collider" : "")}{(col == null && playerCollider == null ? " and " : "")}{(playerCollider == null ? "Player Object Collider" : "")}";
                    string targetsObj = $"{(col == null ? "Dragging Object" : "")}{(col == null && playerCollider == null ? " and " : "")}{(playerCollider == null ? "Player Object" : "")}";
                    Debug.LogWarning($"[PressE] Trying to get {targets} but not finding. Does {targetsObj} have a collider to it?");
                }
            }

            if (draggingInteractable.AddEndEvent)
                draggingInteractable.onInteractEnd.Invoke();

            draggingInteractable.DragEnd();

            draggingInteractable = null;
            draggingObject = null;
            isDragging = false;
        }

        void UpdateIsDragging()
        {
            if (draggingInteractable.DragUIEnabled)
            {
                if (DragUI == null)
                {
                    DragUI = Instantiate(draggingInteractable.overrideDragUIPrefab ? draggingInteractable.DragUIPrefab : InteractionUIPrefab, transform);
                    if (DragUI.TryGetComponent<UIPrefab>(out var uIPrefab))
                    {
                        dragUIprefab = uIPrefab;
                    }

                    if (dragUIprefab && dragUIprefab.hasImage)
                    {
                        dragUIprefab.interactedInteractable = draggingInteractable;

                        if (draggingInteractable.DragUIControlColor) dragUIprefab.Image.color = draggingInteractable.DragUIColor;
                        if (draggingInteractable.DragUIControlSprite) dragUIprefab.Image.sprite = draggingInteractable.DragUISprite;
                        if (draggingInteractable.DragUIControlSize) dragUIprefab.Image.rectTransform.sizeDelta = draggingInteractable.DragUISize;

                        Vector3 uiWorld;
                        bool isRotation = draggingInteractable.DragType == DragType.Rotation;

                        if (isRotation && draggingInteractable.DragUIOverrideOnArc)
                        {
                            uiWorld = draggingInteractable.ComputeDragUIWorldOnArc();
                        }
                        else if (draggingInteractable.DragUIOverrideAnchor)
                        {
                            uiWorld = draggingInteractable.transform.TransformPoint(draggingInteractable.LocalPositionDragUIAnchor);
                        }
                        else if (draggingInteractable.ConsiderHitPlace && draggingInteractable.TryGetDragHitPivot(out uiWorld))
                        {
                            // uiWorld already comes from the hit pivot
                        }
                        else if (draggingInteractable.WillOverrideAnchor)
                        {
                            uiWorld = draggingInteractable.transform.TransformPoint(draggingInteractable.LocalPositionNewAnchor);
                        }
                        else
                        {
                            uiWorld = draggingObject.transform.position;
                        }

                        Vector3 uiScreen = Cam.WorldToScreenPoint(uiWorld) + (Vector3)draggingInteractable.DragUIOverrideScreenOffset;
                        dragUIprefab.rectTransform.position = uiScreen;
                    }
                }
                else if (dragUIprefab != null)
                {
                    Vector3 uiWorld;
                    bool isRotation = draggingInteractable.DragType == DragType.Rotation;

                    if (isRotation && draggingInteractable.DragUIOverrideOnArc)
                    {
                        uiWorld = draggingInteractable.ComputeDragUIWorldOnArc();
                    }
                    else if (draggingInteractable.DragUIOverrideAnchor)
                    {
                        uiWorld = draggingInteractable.transform.TransformPoint(draggingInteractable.LocalPositionDragUIAnchor);
                    }
                    else if (draggingInteractable.ConsiderHitPlace && draggingInteractable.TryGetDragHitPivot(out uiWorld))
                    {
                        // uiWorld already comes from the hit pivot
                    }
                    else if (draggingInteractable.WillOverrideAnchor)
                    {
                        uiWorld = draggingInteractable.transform.TransformPoint(draggingInteractable.LocalPositionNewAnchor);
                    }
                    else
                    {
                        uiWorld = draggingObject.transform.position;
                    }

                    Vector3 uiScreen = Cam.WorldToScreenPoint(uiWorld) + (Vector3)draggingInteractable.DragUIOverrideScreenOffset;
                    dragUIprefab.rectTransform.position = uiScreen;
                }
            }

            if (draggingInteractable.DragHasMaxDistance)
            {
                Vector3 center = draggingInteractable.WillOverrideAnchor
                    ? draggingInteractable.DragGetOriginWorld()
                    : draggingInteractable.DragGetAnchorWorldNow();

                Vector3 refPos = playerObject.transform.position;

                float distance = (refPos - center).sqrMagnitude;
                if (distance > (draggingInteractable.DragMaxDistance * draggingInteractable.DragMaxDistance))
                {
                    ForceReleaseDrag();
                    return;
                }
            }

            if (!dragKeyConsumedThisFrame)
            {
                bool overrideDrag = draggingInteractable.OverrideInteractionKey;

                PressEInputBind dragInter = InputHandler.ResolveBind(Interaction, overrideDrag, draggingInteractable.NewInteraction);
                bool dragInterDown = InputHandler.GeneralInputDown(dragInter);
                dragInterDown |= dragInter.UIButtonDown;

                if (dragInterDown) ForceReleaseDrag();
            }
        }
    
        void UpdateNotDragging()
        {
            if (DragUI != null) Destroy(DragUI);
            if (dragUIprefab != null) Destroy(dragUIprefab);
        }
    }
}