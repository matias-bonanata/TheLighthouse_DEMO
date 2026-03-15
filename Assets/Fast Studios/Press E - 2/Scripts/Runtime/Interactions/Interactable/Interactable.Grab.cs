using UnityEngine;
using System;

namespace FastStudios
{
    public partial class Interactable // Grab
    {
        #region Grab Public
        [Tooltip("Useful for handling specific objects in Grab Deposit")] public string GrabId = null;

        public float GrabDistance = 1f;
        public bool ScrolledBasedDistance;
        [Min(.01f)] public float ScrollMultiplier = 1;
        public Vector2 GrabDistanceMinMax = new Vector2(1, 4);
        public Vector3 GrabOffset = Vector3.zero;

        public bool CanRotateGrab;
        public bool OverrideGrabRotateKeys;
        public PressEInputBind NewGrabRotation = new PressEInputBind()
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.LeftAlt,
            MouseButton = MouseMethod.Left
        };
        public float RotationSensitivity = 10;
        public bool GrabShowCursorWhenRotating = false;
        public bool GrabDontHideCursorOnStop = false;

        public bool CanPlaceGrabDown = false;
        public bool PreventDropping = true;
        public bool CheckCollisionBeforePlacing = true;
        public bool AlignNormals = true;
        public bool VisualizeObject = true;
        public bool UseManagerMaterials = true;
        public bool CanRotatePlaceObject = false;
        public Axis RotationAxis = Axis.Up;
        public bool CalculateOnLocal = true;
        public bool OverrideTurnKeys = false;
        public PressEInputBind NewTurnLeft = new PressEInputBind()
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.Q,
            MouseButton = MouseMethod.Left
        };
        public PressEInputBind NewTurnRight = new PressEInputBind()
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.E,
            MouseButton = MouseMethod.Left
        };
        public bool PressDown = false;
        public float RotationIncrement = 90;

        public bool OverrideGrabRotation;
        public Vector3 newGrabRotation;
        public bool UseTransformToPosition;
        public bool OverrideManagerTransform;
        public Transform GrabTransform;
        public bool TransformGrabInstantFollow = false;
        [Min(0f)] public float TransformGrabFollowSharpness = 10f;
        [Min(0f)] public float TransformGrabRotationSharpness = 10f;

        [HideInInspector] public float throwForceBackup = 100;
        [HideInInspector] public GameObject lineRendererGameObject;
        [HideInInspector] public LineRenderer lineRenderer;

        public bool isThrowable;
        public float throwForce = 100;

        public bool ThrowTowardsAim = false;
        public bool ThrowUseRaycastAim = false;

        [Min(1f)] public float ThrowAimDistance = 50f;
        public bool TimePressedIncreaseForce = false;
        public Vector2 ForceClamp = new Vector2(1, 100);
        public float TimeToReachMaxForce = 2f;
        public bool hasThrowAnimationCurve = false;
        public AnimationCurve ThrowAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public bool canSeeThrowTrajectory = true;
        public bool OnlySeeTrajectoryOnForceIncrease = false;
        public float lineWidth = 0.05f;
        [Range(10, 100)] public int LinePoints = 25;
        [Range(0.01f, 100f)] public float TrajectoryPrecision = 50f;
        public Material LineMaterial;

        public bool PhysicsGrabMode;
        [Min(1)] public float PlayerHelperForce = 1;
        public float linearDamping = 10;
        public float angularDamping = 5;
        public bool SeeAnchorPointGizmos = false;
        public bool SeeCenterOfMassGizmos = false;
        public float PointsRadius = 0.01f;
        public Color AnchorPointGizmosColor = Color.red;
        public Color SeeCenterOfMassGizmosColor = Color.green;
        [HideInInspector] public float linearDampingBackup;
        [HideInInspector] public float angularDampingBackup;

        public bool isGrabbing;
        #endregion

        #region Grab Private

        private LayerMask throwableLayerMask;

        #endregion

        #region Grab Constants

        const float PRECISION_MIN_DT = 0.01f;
        const float PRECISION_MAX_DT = 0.10f;
        private bool oneTimeConditionEvent = false;

        #endregion

        #region Aux Methods

        float ResolveTimeStep()
        {
            float t01 = Mathf.InverseLerp(0.01f, 100f, Mathf.Clamp(TrajectoryPrecision, 0.01f, 100f));
            return Mathf.Lerp(PRECISION_MAX_DT, PRECISION_MIN_DT, t01);
        }

        #endregion

        #region Helper Methods

        [HideInInspector] public void SetupToLineRenderer()
        {
            if (lineRenderer != null) return;
            if (lineRendererGameObject != null) Destroy(lineRendererGameObject);

            lineRendererGameObject = new GameObject("Trajectory Path");
            lineRenderer = lineRendererGameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;

            lineRenderer.Simplify(0.05f);
            lineRenderer.material = LineMaterial;

            lineRenderer.enabled = false;
            lineRenderer.widthMultiplier = lineWidth;
        }

        [HideInInspector] public void DrawProjection()
        {
            if (!canSeeThrowTrajectory) return;

            SetupToLineRenderer();

            lineRenderer.enabled = true;
            lineRenderer.transform.position = DragGetAnchorWorldNow();

            float dt = ResolveTimeStep();

            lineRenderer.positionCount = Mathf.CeilToInt(LinePoints / dt) + 1;
            Vector3 startPosition = DragGetAnchorWorldNow();

            Vector3 aimPoint;
            if (ThrowUseRaycastAim &&
                Physics.Raycast(
                    singleton.Cam.transform.position,
                    singleton.Cam.transform.forward,
                    out var hit2,
                    ThrowAimDistance,
                    singleton._maskAllButIgnoreRaycast))
            {
                aimPoint = hit2.point;
            }
            else
            {
                aimPoint = singleton.Cam.transform.position + singleton.Cam.transform.forward * ThrowAimDistance;
            }

            Vector3 dir = ThrowTowardsAim
                        ? (aimPoint - startPosition).normalized
                        : singleton.Cam.transform.forward;

            float newThrowForce = TimePressedIncreaseForce ? Mathf.Clamp(throwForce, ForceClamp.x, ForceClamp.y) : throwForce;

            bool throwDown = InputHandler.GeneralInputDown(singleton.GrabThrow);
            bool throwInput = InputHandler.GeneralInput(singleton.GrabThrow);
            bool throwUp = InputHandler.GeneralInputUp(singleton.GrabThrow);

            throwDown |= singleton.GrabThrow.UIButtonDown;
            throwInput |= singleton.GrabThrow.UIButtonHeld;
            throwUp |= singleton.GrabThrow.UIButtonUp;

            if (TimePressedIncreaseForce && !throwDown && !throwInput && !throwUp) newThrowForce = ForceClamp.x;

            Vector3 startVelocity = dir * (newThrowForce / interactableRb.mass);
            int i = 0;
            lineRenderer.SetPosition(i, startPosition);

            for (float time = 0; time < LinePoints; time += dt)
            {
                i++;
                Vector3 point = startPosition + time * startVelocity;
                point.y = startPosition.y + startVelocity.y * time + (Physics.gravity.y / 2f * time * time);

                lineRenderer.SetPosition(i, point);

                Vector3 lastPosition = lineRenderer.GetPosition(i - 1);

                if (Physics.Raycast(lastPosition, (point - lastPosition).normalized, out RaycastHit hit, (point - lastPosition).magnitude, throwableLayerMask))
                {
                    lineRenderer.SetPosition(i, hit.point);
                    lineRenderer.positionCount = i + 1;
                    return;
                }
            }
        }

        [HideInInspector] public void DisableProjection()
        {
            if (!canSeeThrowTrajectory || lineRenderer == null) return;

            lineRenderer.enabled = false;
            if (lineRendererGameObject) Destroy(lineRendererGameObject);
        }

        #endregion
    }
}