using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FastStudios
{
    public partial class Interactable // Inspection
    {
        #region Inspection Public
        public float InspectionDistance = 0.5f;
        public InspectionViewMode InspectionViewMode = InspectionViewMode.MoveObjectToCamera;

        public InspectionNavigationTargetType InspectionTargetType = InspectionNavigationTargetType.Transform;
        public Transform InspectionTargetTransform;
        public Vector3 InspectionTargetPosition;
        public Vector3 InspectionTargetRotation;
        public bool InspectionHasMargin;
        public float InspectionLeftMargin;
        public float InspectionRightMargin;
        public float InspectionTopMargin;
        public float InspectionBottomMargin;
        public bool InspectionRotationOffsetOnEdge;
        public float InspectionLeftOffset;
        public float InspectionRightOffset;
        public float InspectionTopOffset;
        public float InspectionBottomOffset;
        [Range(0f, 0.95f)] public float InspectionMarginDeadZone = 0.20f;
        [Range(0f, 1f)] public float InspectionMarginFeather = 1f;
        public bool InspectionPreviewDeadZoneOnGame = true;

        public bool InspectionCanRotate;
        public float InspectionRotationSens = 1;
        public bool OverrideRotationKey;
        public PressEInputBind NewInspectionRotation = new PressEInputBind()
        {
            InputMethod = InputMethod.Mouse,
            Key = KeyCode.LeftAlt,
            MouseButton = MouseMethod.Left
        };
        public bool ShowCursorWhenRotating = true;
        public bool DontHideCursorOnStop = false;
        public bool hasDetailBackground = false;
        public Color DetailBackgroundColor = new Color(0, 0, 0, 0.35f);
        public Sprite DetailBackground;
        public bool hasDetailText = false;
        public bool hasDetailImage = false;
        public Color DetailImageColor = new Color(1, 1, 1, 1);
        public Sprite DetailImage;
        [TextArea(5, int.MaxValue)] public string DetailText;
        public bool OverrideDetailImageKey = false;
        public PressEInputBind NewDetailImage = new PressEInputBind()
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.I,
            MouseButton = MouseMethod.Left
        };
        public bool OverrideDetailTextKey = false;
        public PressEInputBind NewDetailText = new PressEInputBind()
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.T,
            MouseButton = MouseMethod.Left
        };
        public bool DetailImageFirst = false;
        public bool DetailTextFirst = false;
        public float TimeToTakeObject = .25f;
        public AnimationCurve TakeObjectAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public bool overrideInspectionQuaternion = false;
        public Quaternion inspectionQuaternionOverride;
        public bool overrideInspectionPrefab = false;
        public GameObject InspectionPrefab;


        #endregion

#if UNITY_EDITOR

        private GUIStyle inspectionDeadzoneLabelStyle;
        private GUIStyle inspectionDeadzoneLabelStyleLeft;

        private const float inspectionArrowLen = 120f;
        private const float inspectionArrowThickness = 3f;
        private const float inspectionFeatherBarW = 160f;
        private const float inspectionFeatherBarH = 10f;
        private const float inspectionDeadzoneLineThickness = 2f;
        private const float inspectionDeadzoneCursorSize = 7f;

        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            if (singleton == null) return;
            if (!singleton.isInspecting) return;

            if (singleton.inspectionInteractable != this) return;

            if (InspectionViewMode != InspectionViewMode.MoveCameraToObject) return;
            if (!InspectionHasMargin) return;
            if (!InspectionPreviewDeadZoneOnGame) return;

            float w = Screen.width;
            float h = Screen.height;

            Vector2 mp;
            if (!InputHandler.TryGetMouseScreenPosition(out mp))
                mp = new Vector2(w * 0.5f, h * 0.5f);

            InspectionDeadzoneOverlayGUI.Draw(this, w, h, mp, out _);
        }

        private void DrawInspectionDeadzoneOverlay()
        {
            float w = Screen.width;
            float h = Screen.height;
            if (w <= 2f || h <= 2f) return;

            float dead = Mathf.Clamp(singleton.inspectionInteractable.InspectionMarginDeadZone, 0f, 0.95f);

            float halfW = w * 0.5f;
            float halfH = h * 0.5f;

            float dx = dead * halfW;
            float dy = dead * halfH;

            float xL = halfW - dx;
            float xR = halfW + dx;
            float yT = halfH - dy;
            float yB = halfH + dy;

            Rect deadRect = new Rect(xL, yT, dx * 2f, dy * 2f);

            EnsureInspectionDeadzoneStyle();

            DrawSolidRect(deadRect, new Color(1f, 1f, 1f, 0.06f));
            DrawRectOutline(deadRect, inspectionDeadzoneLineThickness, new Color(1f, 1f, 1f, 0.85f));

            DrawSolidRect(new Rect(xL - inspectionDeadzoneLineThickness * 0.5f, 0f, inspectionDeadzoneLineThickness, h), new Color(1f, 1f, 1f, 0.35f));
            DrawSolidRect(new Rect(xR - inspectionDeadzoneLineThickness * 0.5f, 0f, inspectionDeadzoneLineThickness, h), new Color(1f, 1f, 1f, 0.35f));
            DrawSolidRect(new Rect(0f, yT - inspectionDeadzoneLineThickness * 0.5f, w, inspectionDeadzoneLineThickness), new Color(1f, 1f, 1f, 0.35f));
            DrawSolidRect(new Rect(0f, yB - inspectionDeadzoneLineThickness * 0.5f, w, inspectionDeadzoneLineThickness), new Color(1f, 1f, 1f, 0.35f));

            float feather = Mathf.Clamp01(singleton.inspectionInteractable.InspectionMarginFeather);
            float literalness = 1f - feather;

            if (literalness > 0.0001f)
            {
                float a = 0.10f * literalness;
                Color band = new Color(1f, 0.55f, 0.15f, a);

                DrawSolidRect(new Rect(xL, 0f, xR - xL, yT), band);
                DrawSolidRect(new Rect(xL, yB, xR - xL, h - yB), band);

                DrawSolidRect(new Rect(0f, yT, xL, yB - yT), band);
                DrawSolidRect(new Rect(xR, yT, w - xR, yB - yT), band);
            }

            Rect featherBar = new Rect(8f, 28f, inspectionFeatherBarW, inspectionFeatherBarH);
            DrawSolidRect(featherBar, new Color(1f, 1f, 1f, 0.10f));
            DrawSolidRect(new Rect(featherBar.x, featherBar.y, featherBar.width * feather, featherBar.height), new Color(1f, 1f, 1f, 0.70f));
            DrawRectOutline(featherBar, 1f, new Color(1f, 1f, 1f, 0.65f));
            GUI.Label(new Rect(featherBar.xMax + 8f, featherBar.y - 4f, 260f, 20f), $"Feather: {feather:0.00}  (literal -> free)", inspectionDeadzoneLabelStyleLeft);

            GUI.Label(new Rect(halfW - 60f, Mathf.Max(2f, yT - 22f), 120f, 20f), "UP", inspectionDeadzoneLabelStyle);
            GUI.Label(new Rect(halfW - 60f, Mathf.Min(h - 22f, yB + 2f), 120f, 20f), "DOWN", inspectionDeadzoneLabelStyle);
            GUI.Label(new Rect(Mathf.Max(2f, xL - 62f), halfH - 10f, 60f, 20f), "LEFT", inspectionDeadzoneLabelStyle);
            GUI.Label(new Rect(Mathf.Min(w - 62f, xR + 2f), halfH - 10f, 60f, 20f), "RIGHT", inspectionDeadzoneLabelStyle);
            GUI.Label(new Rect(halfW - 70f, halfH - 10f, 140f, 20f), "DEADZONE", inspectionDeadzoneLabelStyle);

            Vector2 mp;
            if (!InputHandler.TryGetMouseScreenPosition(out mp))
                mp = new Vector2(w * 0.5f, h * 0.5f);

            Vector2 guiPos = new Vector2(mp.x, h - mp.y);

            DrawSolidRect(
                new Rect(guiPos.x - inspectionDeadzoneCursorSize * 0.5f, guiPos.y - inspectionDeadzoneCursorSize * 0.5f, inspectionDeadzoneCursorSize, inspectionDeadzoneCursorSize),
                new Color(1f, 1f, 1f, 0.9f)
            );

            Vector2 n = new Vector2((mp.x / w - 0.5f) * 2f, (mp.y / h - 0.5f) * 2f);
            bool inDead = Mathf.Abs(n.x) <= dead && Mathf.Abs(n.y) <= dead;

            GUI.Label(new Rect(8f, 8f, w - 16f, 18f),
                $"MouseN: {n.x:0.00}, {n.y:0.00} | Dead: {dead:0.00} | {(inDead ? "IN" : "OUT")}",
                inspectionDeadzoneLabelStyle
            );

            float xRaw = Mathf.Clamp(n.x, -1f, 1f);
            float yRaw = Mathf.Clamp(n.y, -1f, 1f);

            InspectorRuntimeDeadzonePreview.ComputeAxisFreeFinal(
                new Vector2(xRaw, yRaw),
                dead,
                feather,
                out Vector2 axisN,
                out Vector2 freeN,
                out Vector2 finalN
            );

            float maxA = Mathf.Max(Mathf.Abs(xRaw), Mathf.Abs(yRaw));
            if (maxA > 0.0001f)
            {
                float tFree = ApplySoftCurve_Debug(ApplyDeadZone_Debug(maxA, dead));
                freeN = new Vector2(xRaw / maxA, yRaw / maxA) * tFree;
            }

            Vector2 center = new Vector2(halfW, halfH);

            Vector2 axisDir = new Vector2(axisN.x, -axisN.y);
            Vector2 freeDir = new Vector2(freeN.x, -freeN.y);
            Vector2 finalDir = new Vector2(finalN.x, -finalN.y);

            if (axisDir.sqrMagnitude > 0.0001f) axisDir.Normalize();
            if (freeDir.sqrMagnitude > 0.0001f) freeDir.Normalize();
            if (finalDir.sqrMagnitude > 0.0001f) finalDir.Normalize();

            if (feather > 0.0001f && feather < 0.9999f)
            {
                DrawArrowGUI(center, center + axisDir * inspectionArrowLen, 2.0f, new Color(1f, 1f, 1f, 0.20f * (1f - feather)));

                DrawArrowGUI(center, center + freeDir * inspectionArrowLen, 2.0f, new Color(1f, 1f, 1f, 0.20f * feather));
            }

            DrawArrowGUI(center, center + finalDir * inspectionArrowLen, inspectionArrowThickness, new Color(1f, 1f, 1f, 0.90f));

            GUI.Label(new Rect(8f, 46f, w - 16f, 18f),
                $"AxisN: {axisN.x:0.00},{axisN.y:0.00} | FreeN: {freeN.x:0.00},{freeN.y:0.00} | FinalN: {finalN.x:0.00},{finalN.y:0.00}",
                inspectionDeadzoneLabelStyleLeft
            );
        }

        private void EnsureInspectionDeadzoneStyle()
        {
            if (inspectionDeadzoneLabelStyle != null) return;

            inspectionDeadzoneLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            inspectionDeadzoneLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.92f);

            inspectionDeadzoneLabelStyleLeft = new GUIStyle(inspectionDeadzoneLabelStyle)
            {
                alignment = TextAnchor.MiddleLeft
            };
        }

        private void DrawSolidRect(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private void DrawRectOutline(Rect r, float thickness, Color c)
        {
            DrawSolidRect(new Rect(r.xMin, r.yMin, r.width, thickness), c);
            DrawSolidRect(new Rect(r.xMin, r.yMax - thickness, r.width, thickness), c);
            DrawSolidRect(new Rect(r.xMin, r.yMin, thickness, r.height), c);
            DrawSolidRect(new Rect(r.xMax - thickness, r.yMin, thickness, r.height), c);
        }

        private float ApplyDeadZone_Debug(float v, float deadZone)
        {
            float a = Mathf.Abs(v);
            if (a <= deadZone) return 0f;

            float sign = Mathf.Sign(v);
            float t = (a - deadZone) / Mathf.Max(0.0001f, 1f - deadZone);
            return sign * Mathf.Clamp01(t);
        }

        private float ApplySoftCurve_Debug(float v)
        {
            float a = Mathf.Abs(v);
            a = a * a * (3f - 2f * a);
            return Mathf.Sign(v) * a;
        }

        private void DrawArrowGUI(Vector2 from, Vector2 to, float thickness, Color color)
        {
            Handles.BeginGUI();
            Color prev = Handles.color;
            Handles.color = color;

            Handles.DrawAAPolyLine(thickness, from, to);

            Vector2 dir = (to - from);
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                Vector2 perp = new Vector2(-dir.y, dir.x);

                const float headLen = 10f;
                const float headWidth = 6f;

                Vector2 p1 = to - dir * headLen + perp * headWidth;
                Vector2 p2 = to - dir * headLen - perp * headWidth;

                Handles.DrawAAPolyLine(thickness, to, p1);
                Handles.DrawAAPolyLine(thickness, to, p2);
            }

            Handles.color = prev;
            Handles.EndGUI();
        }

        private void DrawInspectionCameraPreviewGizmos()
        {
            if (InspectionViewMode != InspectionViewMode.MoveCameraToObject) return;

            Vector3 focusWorld = ResolveInspectionFocusWorld();
            Camera refCam = ResolvePreviewCamera();

            Vector3 refPos = refCam ? refCam.transform.position : focusWorld - transform.forward * 2;
            Quaternion refRot = refCam ? refCam.transform.rotation : Quaternion.LookRotation(focusWorld - refPos, Vector3.up);

            float dist = Mathf.Max(0.01f, InspectionDistance);

            bool useTargetTf = InspectionTargetType == InspectionNavigationTargetType.Transform && InspectionTargetTransform != null;
            bool usePosRot = InspectionTargetType == InspectionNavigationTargetType.Position;

            Quaternion camCenterRot;
            if (useTargetTf) camCenterRot = InspectionTargetTransform.rotation;
            else if (usePosRot) camCenterRot = transform.rotation * Quaternion.Euler(InspectionTargetRotation);
            else
            {
                Vector3 fwd = focusWorld - refPos;
                if (fwd.sqrMagnitude < 1e-6f) fwd = refRot * Vector3.forward;
                fwd.Normalize();

                Vector3 upRef = refRot * Vector3.up;
                upRef = Vector3.ProjectOnPlane(upRef, fwd);
                if (upRef.sqrMagnitude < 1e-6f) upRef = Vector3.up;
                upRef.Normalize();

                camCenterRot = Quaternion.LookRotation(fwd, upRef);
            }

            Vector3 dir = camCenterRot * Vector3.forward;
            Vector3 camCenterPos = focusWorld - dir * dist;

            float fov = refCam ? refCam.fieldOfView : 60f;
            float aspect = refCam ? refCam.aspect : 16f / 9f;
            float near = refCam ? Mathf.Max(0.001f, refCam.nearClipPlane) : 0.01f;
            float far = Mathf.Clamp(dist * 3f, 0.15f, 10f);

            float halfV = fov * 0.5f;
            float halfH = Mathf.Atan(Mathf.Tan(halfV * Mathf.Deg2Rad) * aspect) * Mathf.Rad2Deg;

            DrawWireAsymmetricFrustum(
                camCenterPos, camCenterRot,
                near, far,
                halfH, halfH, halfV, halfV,
                new Color(0.2f, 0.65f, 1f, 1f)
            );

            Gizmos.color = new Color(0.2f, 0.65f, 1f, 1f);
            Gizmos.DrawLine(camCenterPos, camCenterPos + dir * dist * 3f);
            Gizmos.DrawSphere(focusWorld, PointsRadius);

            if (!InspectionHasMargin) return;

            float leftM = Mathf.Abs(InspectionLeftMargin);
            float rightM = Mathf.Abs(InspectionRightMargin);
            float topM = Mathf.Abs(InspectionTopMargin);
            float bottomM = Mathf.Abs(InspectionBottomMargin);

            Vector3 r = camCenterRot * Vector3.right;
            Vector3 u = camCenterRot * Vector3.up;

            Vector3 pLT = camCenterPos - r * leftM + u * topM;
            Vector3 pRT = camCenterPos + r * rightM + u * topM;
            Vector3 pRB = camCenterPos + r * rightM - u * bottomM;
            Vector3 pLB = camCenterPos - r * leftM - u * bottomM;

            Gizmos.color = new Color(0.25f, 1f, 0.25f, 1f);
            Gizmos.DrawLine(pLT, pRT);
            Gizmos.DrawLine(pRT, pRB);
            Gizmos.DrawLine(pRB, pLB);
            Gizmos.DrawLine(pLB, pLT);

            Gizmos.DrawSphere(pLT, PointsRadius * 0.9f);
            Gizmos.DrawSphere(pRT, PointsRadius * 0.9f);
            Gizmos.DrawSphere(pRB, PointsRadius * 0.9f);
            Gizmos.DrawSphere(pLB, PointsRadius * 0.9f);

            if (InspectionRotationOffsetOnEdge)
            {
                float yawL = -InspectionLeftOffset;
                float yawR = InspectionRightOffset;
                float pitchT = -InspectionTopOffset;
                float pitchB = InspectionBottomOffset;

                Gizmos.color = new Color(1f, 0.6f, 0.15f, 1f);

                void DrawRotRay(Vector3 origin, float pitch, float yaw)
                {
                    Quaternion rot = camCenterRot * Quaternion.Euler(pitch, yaw, 0f);
                    Vector3 f = rot * Vector3.forward;
                    Gizmos.DrawLine(origin, origin + f * far);
                }

                DrawRotRay(pLT, pitchT, yawL);
                DrawRotRay(pRT, pitchT, yawR);
                DrawRotRay(pRB, pitchB, yawR);
                DrawRotRay(pLB, pitchB, yawL);
            }
        }

        private void DrawWireAsymmetricFrustum(
            Vector3 pos, Quaternion rot,
            float near, float far,
            float leftDeg, float rightDeg, float topDeg, float bottomDeg,
            Color col)
        {
            Matrix4x4 oldM = Gizmos.matrix;
            Color oldC = Gizmos.color;

            Gizmos.color = col;
            Gizmos.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);

            float l = Mathf.Tan(leftDeg * Mathf.Deg2Rad);
            float r = Mathf.Tan(rightDeg * Mathf.Deg2Rad);
            float t = Mathf.Tan(topDeg * Mathf.Deg2Rad);
            float b = Mathf.Tan(bottomDeg * Mathf.Deg2Rad);

            Vector3 nLT = new Vector3(-l * near, t * near, near);
            Vector3 nRT = new Vector3(r * near, t * near, near);
            Vector3 nRB = new Vector3(r * near, -b * near, near);
            Vector3 nLB = new Vector3(-l * near, -b * near, near);

            Vector3 fLT = new Vector3(-l * far, t * far, far);
            Vector3 fRT = new Vector3(r * far, t * far, far);
            Vector3 fRB = new Vector3(r * far, -b * far, far);
            Vector3 fLB = new Vector3(-l * far, -b * far, far);

            Gizmos.DrawLine(nLT, nRT);
            Gizmos.DrawLine(nRT, nRB);
            Gizmos.DrawLine(nRB, nLB);
            Gizmos.DrawLine(nLB, nLT);

            Gizmos.DrawLine(fLT, fRT);
            Gizmos.DrawLine(fRT, fRB);
            Gizmos.DrawLine(fRB, fLB);
            Gizmos.DrawLine(fLB, fLT);

            Gizmos.DrawLine(nLT, fLT);
            Gizmos.DrawLine(nRT, fRT);
            Gizmos.DrawLine(nRB, fRB);
            Gizmos.DrawLine(nLB, fLB);

            Gizmos.matrix = oldM;
            Gizmos.color = oldC;
        }

        private Vector3 ResolveInspectionFocusWorld()
        {
            if (InspectionTargetType == InspectionNavigationTargetType.Transform && InspectionTargetTransform != null)
                return InspectionTargetTransform.position;

            return transform.TransformPoint(InspectionTargetPosition);
        }

        private Camera ResolvePreviewCamera()
        {
            Camera cam = Camera.main;
            if (cam != null) return cam;

            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
                return SceneView.lastActiveSceneView.camera;

            return null;
        }

        private void DrawWireFrustum(Vector3 pos, Quaternion rot, float fov, float aspect, float near, float far, Color col)
        {
            Matrix4x4 old = Gizmos.matrix;
            Color oldC = Gizmos.color;

            Gizmos.color = col;
            Gizmos.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
            Gizmos.DrawFrustum(Vector3.zero, fov, far, near, aspect);

            Gizmos.matrix = old;
            Gizmos.color = oldC;
        }

#endif
    }
}