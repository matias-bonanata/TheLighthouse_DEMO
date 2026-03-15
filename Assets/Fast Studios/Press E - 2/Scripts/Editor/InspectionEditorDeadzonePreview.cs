#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FastStudios
{
    [InitializeOnLoad]
    internal static class InspectionEditorDeadzonePreview
    {
        private const string FIELD_PREVIEW_BOOL = "InspectionPreviewDeadZoneOnGame";
        private const string FIELD_FEATHER = "InspectionMarginFeather";

        private static readonly BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static FieldInfo _fiPreview;
        private static FieldInfo _fiFeather;

        private static EditorWindow _gameView;
        private static IMGUIContainer _overlay;

        private static Interactable _target;
        private static Interactable _previewOwner; 
        private static Vector2 _marginN;     
        private static Vector2 _marginNVel;

        private static Camera _cam;
        private static bool _hasBackup;
        private static Vector3 _backupPos;
        private static Quaternion _backupRot;
        private static bool _hasPrePlayBackup;
        private static Vector3 _prePlayPos;
        private static Quaternion _prePlayRot;

        private static Vector3 _originalCamPos;
        private static Quaternion _originalCamRot;

        private static Vector3 _focusWorld;
        private static Vector3 _centerPos;
        private static Quaternion _centerRot;
        private static Vector3 _baseForward;
        private static Vector3 _baseUp;
        private static Vector3 _baseRight;
        private static float _dist;

        private static Vector2 _panLocal;
        private static Vector2 _panVel;

        private static Vector2 _mouseRawN;
        private static double _lastMouseTime;

        private static double _lastTime;

        static InspectionEditorDeadzonePreview()
        {
            _fiPreview = typeof(Interactable).GetField(FIELD_PREVIEW_BOOL, FieldFlags);
            _fiFeather = typeof(Interactable).GetField(FIELD_FEATHER, FieldFlags);

            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            _lastTime = EditorApplication.timeSinceStartup;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    {
                        if (_hasBackup)
                        {
                            _prePlayPos = _backupPos;
                            _prePlayRot = _backupRot;
                            _hasPrePlayBackup = true;
                        }
                        else
                        {
                            var cam = Camera.main;
                            if (cam == null)
                            {
                                var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                                if (cams != null && cams.Length > 0) cam = cams[0];
                            }

                            if (cam != null)
                            {
                                _prePlayPos = cam.transform.position;
                                _prePlayRot = cam.transform.rotation;
                                _hasPrePlayBackup = true;
                            }
                        }

                        StopPreview(restoreCamera: true, forceDisablePreviewBool: true);
                        break;
                    }

                case PlayModeStateChange.EnteredEditMode:
                    {
                        StopPreview(restoreCamera: false, forceDisablePreviewBool: true);

                        if (_hasPrePlayBackup)
                        {
                            var cam = Camera.main;
                            if (cam == null)
                            {
                                var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                                if (cams != null && cams.Length > 0) cam = cams[0];
                            }

                            if (cam != null)
                            {
                                cam.transform.position = _prePlayPos;
                                cam.transform.rotation = _prePlayRot;
                            }
                        }

                        _hasPrePlayBackup = false;
                        break;
                    }

                default:
                    StopPreview(restoreCamera: true, forceDisablePreviewBool: true);
                    break;
            }
        }

        private static void OnSelectionChanged()
        {
            if (_previewOwner != null)
            {
                bool stillSelected =
                    Selection.objects != null &&
                    Selection.objects.Length == 1 &&
                    Selection.activeGameObject != null &&
                    Selection.activeGameObject.GetComponent<Interactable>() == _previewOwner;

                if (!stillSelected)
                    SetPreviewBool(_previewOwner, false);
            }

            if (Selection.objects != null && Selection.objects.Length > 1)
            {
                foreach (var obj in Selection.objects)
                {
                    if (obj is GameObject go)
                    {
                        var it = go.GetComponent<Interactable>();
                        if (it != null) SetPreviewBool(it, false);
                    }
                }
            }
        }


        private static void Update()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                StopPreview(restoreCamera: true, forceDisablePreviewBool: true);
                return;
            }

            float dt = ComputeDt();
            ResolveTargetFromSelection();

            if (_target == null || !GetPreviewBool(_target))
            {
                StopPreview(restoreCamera: true, forceDisablePreviewBool: true);
                return;
            }

            if (_target.InspectionViewMode != InspectionViewMode.MoveCameraToObject || !_target.InspectionHasMargin)
            {
                StopPreview(restoreCamera: true, forceDisablePreviewBool: true);
                return;
            }

            EnsureGameViewOverlay();
            EnsureCamera();

            if (_cam == null)
            {
                StopPreview(restoreCamera: true, forceDisablePreviewBool: true);
                return;
            }

            EnsureCenterPoseInitialized();
            
            _previewOwner = _target;

            StepCamera(dt);

            _gameView?.Repaint();
        }

        private static float ComputeDt()
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _lastTime);
            _lastTime = now;

            return Mathf.Clamp(dt, 0f, 0.05f);
        }

        private static void ResolveTargetFromSelection()
        {
            if (Selection.objects == null || Selection.objects.Length != 1 || Selection.activeGameObject == null)
            {
                _target = null;
                return;
            }

            var it = Selection.activeGameObject.GetComponent<Interactable>();
            _target = it;
        }

        private static void EnsureGameViewOverlay()
        {
            if (_gameView == null)
                _gameView = FindGameView();

            if (_gameView == null)
                return;

            if (_overlay == null)
            {
                _overlay = new IMGUIContainer(DrawOverlayGUI);
                _overlay.style.position = Position.Absolute;
                _overlay.style.left = 0;
                _overlay.style.right = 0;
                _overlay.style.top = 0;
                _overlay.style.bottom = 0;
                _overlay.pickingMode = PickingMode.Ignore;
            }

            var root = _gameView.rootVisualElement;
            if (_overlay.parent != root)
            {
                _overlay.RemoveFromHierarchy();
                root.Add(_overlay);
            }
        }

        private static EditorWindow FindGameView()
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (var w in windows)
            {
                if (w == null) continue;
                var t = w.GetType();
                if (t.FullName == "UnityEditor.GameView")
                    return w;
            }
            return null;
        }

        private static void EnsureCamera()
        {
            if (_cam == null)
            {
                _cam = Camera.main;
                if (_cam == null)
                {
                    var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                    if (cams != null && cams.Length > 0) _cam = cams[0];
                }

                if (_cam != null)
                {
                    BackupCameraIfNeeded();
                    _originalCamPos = _backupPos;
                    _originalCamRot = _backupRot;
                }
            }
        }

        private static void BackupCameraIfNeeded()
        {
            if (_cam == null || _hasBackup) return;
            _backupPos = _cam.transform.position;
            _backupRot = _cam.transform.rotation;
            _hasBackup = true;
        }

        private static void EnsureCenterPoseInitialized()
        {
            InspectorRuntimeDeadzonePreview.ComputeInspectionCenterPose(
                _target,
                _target.transform,
                _originalCamPos,
                _originalCamRot,
                out _focusWorld,
                out _dist,
                out _centerPos,
                out _centerRot,
                out _baseForward,
                out _baseUp,
                out _baseRight
            );
        }

        private static Vector3 GetFocusWorld(Interactable it)
        {
            if (it.InspectionTargetType == InspectionNavigationTargetType.Transform && it.InspectionTargetTransform != null)
                return it.InspectionTargetTransform.position;

            return it.transform.TransformPoint(it.InspectionTargetPosition);
        }

        private static void StepCamera(float dt)
        {
            bool mouseFresh = (EditorApplication.timeSinceStartup - _lastMouseTime) < 0.25;
            Vector2 rawN = mouseFresh ? _mouseRawN : Vector2.zero;

            dt = Mathf.Max(0.0001f, dt);

            float xRaw = Mathf.Clamp(rawN.x, -1f, 1f);
            float yRaw = Mathf.Clamp(rawN.y, -1f, 1f);

            float dead = Mathf.Clamp01(_target.InspectionMarginDeadZone);
            float feather = Mathf.Clamp01(_target.InspectionMarginFeather);

            float leftM = Mathf.Abs(_target.InspectionLeftMargin);
            float rightM = Mathf.Abs(_target.InspectionRightMargin);
            float topM = Mathf.Abs(_target.InspectionTopMargin);
            float bottomM = Mathf.Abs(_target.InspectionBottomMargin);

            InspectorRuntimeDeadzonePreview.StepMarginCamera(
                _cam.transform,
                _mouseRawN,
                dead,
                feather,
                ref _marginN,
                ref _marginNVel,
                ref _panLocal,
                ref _panVel,
                _centerPos,
                _centerRot,
                _baseRight,
                _baseUp,
                leftM, rightM, topM, bottomM,
                _target.InspectionRotationOffsetOnEdge,
                _target.InspectionLeftOffset,
                _target.InspectionRightOffset,
                _target.InspectionTopOffset,
                _target.InspectionBottomOffset,
                dt
            );
        }

        private static float ApplyDeadZone(float v, float deadZone)
        {
            float a = Mathf.Abs(v);
            if (a <= deadZone) return 0f;

            float sign = Mathf.Sign(v);
            float t = (a - deadZone) / Mathf.Max(0.0001f, 1f - deadZone);
            return sign * Mathf.Clamp01(t);
        }

        private static float ApplySoftCurve(float v)
        {
            float a = Mathf.Abs(v);
            a = a * a * (3f - 2f * a);
            return Mathf.Sign(v) * a;
        }

        private static bool GetPreviewBool(Interactable it)
        {
            if (_fiPreview == null || it == null) return false;
            try { return (bool)_fiPreview.GetValue(it); }
            catch { return false; }
        }

        private static void SetPreviewBool(Interactable it, bool value)
        {
            if (_fiPreview == null || it == null) return;

            bool current = GetPreviewBool(it);
            if (current == value) return;

            Undo.RecordObject(it, "PressE Inspection Preview Toggle");
            try { _fiPreview.SetValue(it, value); }
            catch { }

            EditorUtility.SetDirty(it);
        }

        private static float GetFeather(Interactable it)
        {
            if (_fiFeather == null || it == null) return 0f;
            try
            {
                float v = (float)_fiFeather.GetValue(it);
                return Mathf.Clamp01(v);
            }
            catch
            {
                return 0f;
            }
        }

        private static void StopPreview(bool restoreCamera, bool forceDisablePreviewBool)
        {
            if (forceDisablePreviewBool && _previewOwner != null)
                SetPreviewBool(_previewOwner, false);

            if (_overlay != null) _overlay.RemoveFromHierarchy();

            if (restoreCamera && _cam != null && _hasBackup)
            {
                _cam.transform.position = _backupPos;
                _cam.transform.rotation = _backupRot;
            }

            _cam = null;
            _hasBackup = false;

            _panLocal = Vector2.zero;
            _panVel = Vector2.zero;

            _marginN = Vector2.zero;
            _marginNVel = Vector2.zero;

            _previewOwner = null;
        }

        private static GUIStyle _label;
        private static GUIStyle _box;
        private static GUIStyle _mini;
        private static GUIStyle _deadzoneLabel;
        private static GUIStyle _deadzoneLabelLeft;

        private const float ARROW_LEN = 120f;
        private const float ARROW_THICKNESS = 3f;
        private const float FEATHER_BAR_W = 160f;
        private const float FEATHER_BAR_H = 10f;
        private const float LINE_THICKNESS = 2f;
        private const float CURSOR_SIZE = 7f;

        private static void EnsureDeadzoneStyles()
        {
            if (_deadzoneLabel != null) return;

            _deadzoneLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            _deadzoneLabel.normal.textColor = new Color(1f, 1f, 1f, 0.92f);

            _deadzoneLabelLeft = new GUIStyle(_deadzoneLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };
        }

        private static void DrawSolidRect(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private static void DrawRectOutline(Rect r, float thickness, Color c)
        {
            DrawSolidRect(new Rect(r.xMin, r.yMin, r.width, thickness), c);
            DrawSolidRect(new Rect(r.xMin, r.yMax - thickness, r.width, thickness), c);
            DrawSolidRect(new Rect(r.xMin, r.yMin, thickness, r.height), c);
            DrawSolidRect(new Rect(r.xMax - thickness, r.yMin, thickness, r.height), c);
        }

        private static void DrawArrowGUI(Vector2 from, Vector2 to, float thickness, Color color)
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
        private static void EnsureStyles()
        {
            if (_label == null)
            {
                _label = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (_mini == null)
            {
                _mini = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (_box == null)
            {
                _box = new GUIStyle("box");
            }
        }

        private static void DrawOverlayGUI()
        {
            if (_target == null || !GetPreviewBool(_target)) return;

            float w = Screen.width;
            float h = Screen.height;
            if (w <= 2f || h <= 2f) return;

            Vector2 guiMouse = Event.current != null ? Event.current.mousePosition : new Vector2(w * 0.5f, h * 0.5f);
            Vector2 mp = new Vector2(guiMouse.x, h - guiMouse.y);

            InspectionDeadzoneOverlayGUI.Draw(_target, w, h, mp, out _mouseRawN);
            _lastMouseTime = EditorApplication.timeSinceStartup;
        }
    }
}
#endif
