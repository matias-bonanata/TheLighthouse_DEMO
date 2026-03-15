#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FastStudios.EditorTools
{
    public sealed class LerpTimelinePreviewUI : IDisposable
    {
        private PressELerps _target;
        private VisualElement _root;
        private string _hiddenClass;

        private Func<float> _getDuration;
        private Func<Transform> _resolvePreviewTransform;

        private VisualElement _timelineSection;
        private VisualElement _previewContainer;

        private Button _playPauseButton;
        private Button _loopButton;
        private Slider _slider;
        private Label _timeLabel;

        private bool _previewEnabled;
        private bool _playing;
        private bool _loop;

        private float _time;
        private double _lastEditorTime;

        private IVisualElementScheduledItem _pollFoldoutItem;
        private IVisualElementScheduledItem _syncDurationItem;

        private readonly List<Renderer> _hiddenRenderers = new();
        private Transform _hiddenRoot;

        private static readonly Dictionary<Renderer, int> s_forceOffRefCount = new();
        private static readonly Dictionary<Renderer, bool> s_originalForceOff = new();

        private Action _onPlayPauseClicked;
        private Action _onLoopClicked;
        private EventCallback<ChangeEvent<float>> _onSliderChanged;
        private EventCallback<DetachFromPanelEvent> _onDetachedFromPanel;
        private bool _isDisposing;
        public static Texture2D pauseTexture;
        public static Texture2D playTexture;
        public static Texture2D loopTexture;

        [InitializeOnLoadMethod]
        private static void InitGlobalRestoreHooks()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= RestoreAllHiddenRenderers;
            AssemblyReloadEvents.beforeAssemblyReload += RestoreAllHiddenRenderers;

            EditorApplication.quitting -= RestoreAllHiddenRenderers;
            EditorApplication.quitting += RestoreAllHiddenRenderers;

            EditorApplication.playModeStateChanged -= GlobalPlayModeStateChanged;
            EditorApplication.playModeStateChanged += GlobalPlayModeStateChanged;
        }

        private static void GlobalPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode ||
                state == PlayModeStateChange.EnteredPlayMode)
            {
                RestoreAllHiddenRenderers();
            }
        }

        private static void RestoreAllHiddenRenderers()
        {
            var keys = new List<Renderer>(s_forceOffRefCount.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                var r = keys[i];
                if (r == null) continue;

                if (s_originalForceOff.TryGetValue(r, out bool original))
                    r.forceRenderingOff = original;
                else
                    r.forceRenderingOff = false;
            }

            s_forceOffRefCount.Clear();
            s_originalForceOff.Clear();
        }

        public void Setup(
            VisualElement root,
            PressELerps target,
            Func<float> getDuration,
            Func<Transform> resolvePreviewTransform,
            string hiddenClass
        )
        {
            Dispose();

            _root = root;
            _target = target;
            _getDuration = getDuration;
            _resolvePreviewTransform = resolvePreviewTransform;
            _hiddenClass = hiddenClass;

            if (_root == null || _target == null) return;

            _timelineSection = _root.Q<VisualElement>("TimelineSection");
            if (_timelineSection == null) return;

            _onDetachedFromPanel ??= _ =>
            {
                Dispose();
            };

            _root.UnregisterCallback<DetachFromPanelEvent>(_onDetachedFromPanel);
            _root.RegisterCallback<DetachFromPanelEvent>(_onDetachedFromPanel);

            _previewContainer = _timelineSection.Q<VisualElement>("PreviewContainer");

            _playPauseButton = _timelineSection.Q<Button>("TimelinePlayPauseButton");
            _loopButton = _timelineSection.Q<Button>("TimelineLoopButton");
            _slider = _timelineSection.Q<Slider>("TimelineSlider");
            _timeLabel = _timelineSection.Q<Label>("TimelineTimeLabel");

            if (_playPauseButton == null || _slider == null || _timeLabel == null)
                return;

            _previewEnabled = false;
            _playing = false;
            _loop = false;
            _time = 0f;
            _lastEditorTime = EditorApplication.timeSinceStartup;

            SyncDuration(true);
            SyncTimeUI();

            _onPlayPauseClicked = () =>
            {
                if (_previewContainer != null && _previewContainer.ClassListContains(_hiddenClass))
                    return;

                if (!_previewEnabled)
                {
                    SetPreviewEnabled(true);

                    RestartIfAtEnd();

                    SetPlaying(true);
                    return;
                }

                if (!_playing)
                    RestartIfAtEnd();

                SetPlaying(!_playing);
            };

            _onLoopClicked = () =>
            {
                _loop = !_loop;
                UpdateLoopVisual();
            };

            _onSliderChanged = evt =>
            {
                if (!_previewEnabled)
                    SetPreviewEnabled(true);

                _time = Mathf.Max(0f, evt.newValue);
                SetPlaying(false);

                PushTimeToGizmos();
                SyncTimeUI();

                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            };

            _playPauseButton.clicked += _onPlayPauseClicked;
            _slider.RegisterValueChangedCallback(_onSliderChanged);

            if (_loopButton != null)
                _loopButton.clicked += _onLoopClicked;

            UpdatePlayPauseVisual();
            UpdateLoopVisual();

            if (_previewContainer != null)
            {
                bool lastOpen = !_previewContainer.ClassListContains(_hiddenClass);

                SetPreviewEnabled(lastOpen);

                _pollFoldoutItem = _root.schedule.Execute(() =>
                {
                    bool open = !_previewContainer.ClassListContains(_hiddenClass);
                    if (open == lastOpen) return;

                    lastOpen = open;
                    SetPreviewEnabled(open);
                }).Every(100);
            }

            _syncDurationItem = _root.schedule.Execute(() => SyncDuration(false)).Every(250);

            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void Dispose()
        {
            if (_isDisposing) return;
            _isDisposing = true;

            SetPlaying(false);
            SetPreviewEnabled(false);

            if (_pollFoldoutItem != null) _pollFoldoutItem.Pause();
            if (_syncDurationItem != null) _syncDurationItem.Pause();

            if (_playPauseButton != null && _onPlayPauseClicked != null)
                _playPauseButton.clicked -= _onPlayPauseClicked;

            if (_loopButton != null && _onLoopClicked != null)
                _loopButton.clicked -= _onLoopClicked;

            if (_slider != null && _onSliderChanged != null)
                _slider.UnregisterValueChangedCallback(_onSliderChanged);

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            if (_root != null && _onDetachedFromPanel != null)
                _root.UnregisterCallback<DetachFromPanelEvent>(_onDetachedFromPanel);

            _root = null;
            _target = null;
            _getDuration = null;
            _resolvePreviewTransform = null;

            _timelineSection = null;
            _previewContainer = null;
            _playPauseButton = null;
            _loopButton = null;
            _slider = null;
            _timeLabel = null;

            _pollFoldoutItem = null;
            _syncDurationItem = null;

            _onPlayPauseClicked = null;
            _onLoopClicked = null;
            _onSliderChanged = null;

            _isDisposing = false;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode ||
                state == PlayModeStateChange.EnteredPlayMode)
            {
                SetPlaying(false);
                SetPreviewEnabled(false);
            }
        }

        private void OnEditorUpdate()
        {
            if (!_playing || !_previewEnabled || _target == null) return;

            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _lastEditorTime);
            _lastEditorTime = now;

            float duration = Mathf.Max(0.0001f, GetDurationSafe());

            _time += dt;

            if (_time > duration)
            {
                if (_loop)
                {
                    _time = 0f;
                }
                else
                {
                    _time = duration;
                    SetPlaying(false);
                }
            }

            _slider?.SetValueWithoutNotify(_time);

            PushTimeToGizmos();
            SyncTimeUI();

            SceneView.RepaintAll();
        }

        private float GetDurationSafe()
        {
            if (_getDuration == null) return 1f;
            try { return _getDuration(); }
            catch { return 1f; }
        }

        private void RestartIfAtEnd()
        {
            float duration = Mathf.Max(0.0001f, GetDurationSafe());

            const float epsilon = 0.0005f;

            if (_time >= duration - epsilon)
            {
                _time = 0f;
                _slider?.SetValueWithoutNotify(_time);
                PushTimeToGizmos();
                SyncTimeUI();
                SceneView.RepaintAll();
            }
        }

        private void SyncDuration(bool force)
        {
            if (_slider == null) return;

            float duration = Mathf.Max(0.0001f, GetDurationSafe());

            if (force || Math.Abs(_slider.highValue - duration) > 0.0001f)
                _slider.highValue = duration;

            if (_time > duration) _time = duration;

            _slider.SetValueWithoutNotify(_time);
            SyncTimeUI();
        }

        private void SyncTimeUI()
        {
            if (_timeLabel == null) return;

            float duration = Mathf.Max(0.0001f, GetDurationSafe());
            _timeLabel.text = $"{_time:0.00}s / {duration:0.00}s";
        }

        private void SetPlaying(bool value)
        {
            _playing = value;
            _lastEditorTime = EditorApplication.timeSinceStartup;
            UpdatePlayPauseVisual();
        }

        private void UpdatePlayPauseVisual()
        {
            if (_playPauseButton == null) return;

            if (pauseTexture == null) pauseTexture = Resources.Load<Texture2D>("FastStudios/ForEditor/Icons/pause");
            if (playTexture == null) playTexture = Resources.Load<Texture2D>("FastStudios/ForEditor/Icons/play");

#if UNITY_6000_0_OR_NEWER
            _playPauseButton.iconImage = _playing ? pauseTexture : playTexture;
#else
            if (_playPauseButton.childCount == 0)
            {
                VisualElement imageDisplay = new VisualElement();
                imageDisplay.style.backgroundImage = _playing ? pauseTexture : playTexture;
                imageDisplay.style.marginBottom = 5;
                imageDisplay.style.marginLeft = 5;
                imageDisplay.style.marginRight = 5;
                imageDisplay.style.marginTop = 5;
                imageDisplay.style.flexGrow = 1;
                _playPauseButton.Add(imageDisplay);
            }
            else
            {
                _playPauseButton[0].style.backgroundImage = _playing ? pauseTexture : playTexture;
            }
#endif
        }

        private void UpdateLoopVisual()
        {
            if (_loopButton == null) return;

            if (_loop) _loopButton.AddToClassList("Loop");
            else _loopButton.RemoveFromClassList("Loop");

            if (loopTexture == null) loopTexture = Resources.Load<Texture2D>("FastStudios/ForEditor/Icons/loop");

#if UNITY_2022_3_OR_NEWER && !UNITY_6000_0_OR_NEWER

            if (_loopButton.childCount == 0)
            {
                VisualElement imageDisplay = new VisualElement();
                imageDisplay.style.backgroundImage = loopTexture;
                imageDisplay.style.marginBottom = 5;
                imageDisplay.style.marginLeft = 5;
                imageDisplay.style.marginRight = 5;
                imageDisplay.style.marginTop = 5;
                imageDisplay.style.flexGrow = 1;
                _loopButton.Add(imageDisplay);
            }
            else
            {
                _loopButton[0].style.backgroundImage = loopTexture;
            }
#endif
        }

        private void SetPreviewEnabled(bool value)
        {
            if (_previewEnabled == value)
            {
                if (!value) RestoreHiddenRenderers();
                return;
            }

            _previewEnabled = value;

            if (!_previewEnabled)
            {
                SetPlaying(false);
                RestoreHiddenRenderers();
                if (_target != null) PressELerps.__Editor_ClearTimelinePreview(_target);
                SceneView.RepaintAll();
                return;
            }

            _time = Mathf.Clamp(_time, 0f, Mathf.Max(0.0001f, GetDurationSafe()));
            _slider?.SetValueWithoutNotify(_time);

            HideTargetRenderers();
            PushTimeToGizmos();
            SyncTimeUI();
            SceneView.RepaintAll();
        }

        private void PushTimeToGizmos()
        {
            if (_target == null) return;
            PressELerps.__Editor_SetTimelinePreview(_target, true, _time);
        }

        private void HideTargetRenderers()
        {
            var tr = (_resolvePreviewTransform != null) ? _resolvePreviewTransform() : null;
            if (tr == null) return;

            if (_hiddenRoot != null && _hiddenRoot != tr)
                RestoreHiddenRenderers();

            if (_hiddenRoot == tr && _hiddenRenderers.Count > 0)
                return;

            _hiddenRoot = tr;

            _hiddenRenderers.Clear();
            tr.GetComponentsInChildren(true, _hiddenRenderers);

            for (int i = 0; i < _hiddenRenderers.Count; i++)
            {
                var r = _hiddenRenderers[i];
                if (r == null) continue;

                if (!s_forceOffRefCount.TryGetValue(r, out int count) || count <= 0)
                {
                    s_originalForceOff[r] = r.forceRenderingOff;
                    s_forceOffRefCount[r] = 1;
                }
                else
                {
                    s_forceOffRefCount[r] = count + 1;
                }

                r.forceRenderingOff = true;
            }
        }

        private void RestoreHiddenRenderers()
        {
            for (int i = 0; i < _hiddenRenderers.Count; i++)
            {
                var r = _hiddenRenderers[i];
                if (r == null) continue;

                if (s_forceOffRefCount.TryGetValue(r, out int count))
                {
                    count--;

                    if (count <= 0)
                    {
                        s_forceOffRefCount.Remove(r);

                        if (s_originalForceOff.TryGetValue(r, out bool original))
                        {
                            r.forceRenderingOff = original;
                            s_originalForceOff.Remove(r);
                        }
                        else
                        {
                            r.forceRenderingOff = false;
                        }
                    }
                    else
                    {
                        s_forceOffRefCount[r] = count;
                    }
                }
                else
                {
                    r.forceRenderingOff = false;
                }
            }

            _hiddenRenderers.Clear();
            _hiddenRoot = null;
        }

    }
}

#endif