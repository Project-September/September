#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace InGame.Common.AnimationMontage.Editor
{
    [CustomEditor(typeof(AnimationMontage))]
    public sealed class AnimationMontageEditor : UnityEditor.Editor
    {
        // Preview scene
        private PreviewRenderUtility _preview;
        private GameObject _root;
        private Animator _anim;

        // Camera control
        private Vector3 _pivot;
        private float _yaw = 30f, _pitch = 15f, _distance = 3f;

        // Animation
        private PlayableGraph _graph;
        private AnimationClipPlayable _clipPlayable;
        private double _length, _time;
        private bool _playing = true, _loop = true;
        private float _playRate = 1f;

        // Timing / throttling
        private double _lastTs;
        private double _accum;
        private const double TargetDt = 1.0 / 30.0;

        // RT reuse
        private RenderTexture _rt;
        private int _rtW, _rtH;
        private bool _viewDirty;

        // Input
        private Vector2 _lastMouse;
        private bool _dragging;

        // Cached SO refs
        private GameObject _modelPrefab;
        private AnimationClip _clip;
        private Avatar _avatar;

        // Serialized properties
        private List<NotifyKey> _notifies;

        // Notify drawing
        private static GUIContent _notifyIcon;

        // 描画したNotifyのRectとインデックス
        private readonly List<(Rect rect, int index)> _notifyRects = new();

#region Lifecycle
        void OnEnable()
        {
            _lastTs = EditorApplication.timeSinceStartup;
            _accum = 0;
            _notifyIcon = EditorGUIUtility.IconContent("Animation.EventMarker");
            EditorApplication.update += Tick;
            BuildFromTarget();

            FrameCameraToBounds(CalcBounds(_root));
            _viewDirty = true;
        }

        void OnDisable()
        {
            EditorApplication.update -= Tick;
            SafeDispose();
        }
#endregion

#region Inspector / Preview
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                BuildFromTarget();
            }
        }

        public override bool HasPreviewGUI() => _modelPrefab || _clip;

        public override void OnPreviewSettings()
        {
            if (!_clip) return;

            // Play/Pause
            var playIcon = EditorGUIUtility.IconContent(_playing ? "PauseButton" : "PlayButton");
            if (GUILayout.Button(playIcon, EditorStyles.toolbarButton, GUILayout.Width(28)))
            {
                if (_time >= _length)
                {
                    _time = 0;
                    _playing = true;
                    _clipPlayable.SetTime(_time);
                }
                else
                {
                    _playing = !_playing;
                }
                if (_playing) _lastTs = EditorApplication.timeSinceStartup;
            }
            
            // Add Notify Key
            if (GUILayout.Button(EditorGUIUtility.TrIconContent("d_CreateAddNew", "Add Notify Key"), EditorStyles.toolbarButton, GUILayout.Width(28)))
            {
                AddNotifyAtTime((float)_time);
                GUI.FocusControl(null);
            }

            // Frame camera
            if (GUILayout.Button(EditorGUIUtility.TrIconContent("d_SceneViewCamera", "Camera Reset"), EditorStyles.toolbarButton, GUILayout.Width(28)))
            {
                FrameCameraToBounds(CalcBounds(_root));
                _viewDirty = true;
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (_preview == null)
                return;

            // ---- レイアウト定義 ----
            const float sliderH = 14f;
            const float markerH = 12f;
            const float uiPad   = 6f;

            // UIエリア全体（下部）
            float uiTotalH = sliderH + markerH + uiPad * 2f;
            var viewRect   = new Rect(r.x, r.y, r.width, Mathf.Max(8, r.height - uiTotalH));
            var sliderRect = new Rect(r.x + uiPad, r.yMax - uiPad - sliderH, r.width - 16, sliderH);
            var markerRect = new Rect(sliderRect.x, sliderRect.y - markerH, sliderRect.width, markerH);
            
            // スクラブバーとノーティファイ
            if (_clip)
            {
                // Notify Icon の Rect を作成してから入力をとる
                BuildNotifyRects(sliderRect, markerRect);
                HandleNotifyContext();
            }
            
            // Preview の入力
            HandleMouse(viewRect);
            
            if (Event.current.type == EventType.Repaint)
            {
                DrawPreviewTexture(viewRect);
            }

            if (_clip)
            {
                DrawScrubAndNotifies(sliderRect, markerRect);
            }
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background) => OnPreviewGUI(r, background);
#endregion

#region Build / Dispose
        void BuildFromTarget()
        {
            SafeDispose();

            var montage = (AnimationMontage)target;
            _modelPrefab = montage.PreviewModel;
            _clip = montage.Clip;
            _avatar = montage.OverrideAvatar;
            _playRate = Mathf.Max(0.0001f, montage.PlayRate);
            _loop = montage.Loop;
            _notifies = montage.Notifies;

            _preview = new PreviewRenderUtility(true)
            {
                cameraFieldOfView = 30f,
                camera =
                {
                    nearClipPlane = 0.01f,
                    farClipPlane = 1000f,
                    cullingMask = ~0,
                    allowHDR = false,
                    allowMSAA = false
                }
            };
            _preview.lights[0].intensity = 1.6f;
            _preview.lights[0].transform.rotation = Quaternion.Euler(50, 30, 0);
            _preview.lights[1].intensity = 1.2f;
            _preview.ambientColor = new Color(0.25f, 0.25f, 0.25f, 1f);

            if (_modelPrefab)
            {
                _root = (GameObject)PrefabUtility.InstantiatePrefab(_modelPrefab);
                if (_root == null) _root = Instantiate(_modelPrefab);
                _root.hideFlags = HideFlags.HideAndDontSave;
                _root.transform.rotation = Quaternion.Euler(0, 180, 0);
                SetLayerRecursively(_root, 0);
                GameObjectUtility.SetStaticEditorFlags(_root, 0);

                _anim = _root.GetComponentInChildren<Animator>() ?? _root.AddComponent<Animator>();
                _anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _anim.updateMode = AnimatorUpdateMode.Normal;
                _anim.applyRootMotion = false;
                if (_avatar) _anim.avatar = _avatar;

                foreach (var smr in _root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    smr.updateWhenOffscreen = true;
                    if (smr.localBounds.extents == Vector3.zero)
                        smr.localBounds = new Bounds(Vector3.zero, Vector3.one);
                }
                LightenRenderers(_root);

                _preview.AddSingleGO(_root);
                _anim.Update(0f);

                var b = CalcBounds(_root);
                _pivot = b.center;
                _distance = Mathf.Max(0.5f, b.extents.magnitude * 2.2f);
                
                FrameCameraToBounds(CalcBounds(_root));
            }

            _graph = PlayableGraph.Create("AnimationMontagePreview");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            if (_clip && _anim)
            {
                _length = Math.Max(0.0001, _clip.length);
                _time = Math.Clamp(_time, 0, _length);
                
                var output = AnimationPlayableOutput.Create(_graph, "AnimOut", _anim);
                _clipPlayable = AnimationClipPlayable.Create(_graph, _clip);
                _clipPlayable.SetApplyFootIK(false);
                _clipPlayable.SetSpeed(_playRate);
                _clipPlayable.SetTime(_time);
                output.SetSourcePlayable(_clipPlayable);

                _graph.Play();
                _graph.Evaluate(0);
            }
            else
            {
                _length = 1;
                _time = 0;
            }

            _viewDirty = true;
            _lastTs = EditorApplication.timeSinceStartup;
        }

        void SafeDispose()
        {
            try
            {
                if (_graph.IsValid()) _graph.Destroy();
                if (_root) DestroyImmediate(_root);
                _preview?.Cleanup();
                if (_rt)
                {
                    if (_rt.IsCreated()) _rt.Release();
                    DestroyImmediate(_rt);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _graph = default;
                _root = null;
                _preview = null;
                _rt = null;
                _rtW = _rtH = 0;
            }
        }
#endregion

#region Preview Control
        void Tick()
        {
            if (!_graph.IsValid() || _clip == null) return;

            var now = EditorApplication.timeSinceStartup;
            var dt = now - _lastTs;
            _lastTs = now;

            if (_playing)
            {
                _accum += dt;
                while (_accum >= TargetDt)
                {
                    _accum -= TargetDt;
                    _graph.Evaluate((float)TargetDt);
                    _time += TargetDt * _playRate;

                    if (_time >= _length)
                    {
                        if (_loop) _time = 0;
                        else { _time = _length; _playing = false; }
                        _clipPlayable.SetTime(_time);
                    }
                    
                    _viewDirty = true;
                    Repaint();
                }
            }
        }

        void Seek(double t)
        {
            _time = Mathf.Clamp((float)t, 0, (float)_length);
            if (_clipPlayable.IsValid())
            {
                _clipPlayable.SetTime(_time);
                _graph.Evaluate(0);
            }
            else if (_anim)
            {
                _anim.Update(0f);
            }
            _viewDirty = true;
        }

        void HandleMouse(Rect r)
        {
            var e = Event.current;
            if (!r.Contains(e.mousePosition)) return;

            if (e.type == EventType.ScrollWheel)
            {
                _distance = Mathf.Clamp(_distance * (1f + e.delta.y * 0.05f), 0.2f, 50f);
                e.Use();
                _viewDirty = true;
            }

            if (e.type == EventType.MouseDown && e.button is 0 or 1)
            {
                _dragging = true;
                _lastMouse = e.mousePosition;
                GUI.FocusControl(null);
                e.Use();
            }
            if (e.type == EventType.MouseUp) _dragging = false;

            if (_dragging && e.type == EventType.MouseDrag)
            {
                var d = e.mousePosition - _lastMouse;
                _lastMouse = e.mousePosition;

                if (e.button == 0)
                {
                    _yaw += d.x * 0.3f;
                    _pitch = Mathf.Clamp(_pitch + d.y * 0.3f, -80f, 80f);
                }
                else if (e.button == 1)
                {
                    var right = Quaternion.Euler(0, _yaw, 0) * Vector3.right;
                    var up = Vector3.up;
                    var s = _distance * 0.0015f;
                    _pivot -= right * (d.x * s);
                    _pivot += up * (d.y * s);
                }
                e.Use();
                _viewDirty = true;
            }
        }
# endregion

#region Preview Draw
        void EnsureRT(Rect r)
        {
            int w = Mathf.Max(8, Mathf.CeilToInt(r.width));
            int h = Mathf.Max(8, Mathf.CeilToInt(r.height));
            if (_rt && w == _rtW && h == _rtH) return;

            if (_rt)
            {
                try
                {
                    _rt.Release();
                }
                catch { /* ignored */ }
            }
            _rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            _rt.Create();
            _rtW = w; _rtH = h;
            _viewDirty = true;
        }

        void DrawPreviewTexture(Rect r)
        {
            EnsureRT(r);

            var cam = _preview.camera;
            var rot = Quaternion.Euler(_pitch, _yaw, 0);
            var pos = _pivot + rot * (Vector3.back * Mathf.Max(0.2f, _distance));
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(_pivot - pos, Vector3.up));
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.16f, 0.18f, 1f)
                : new Color(0.8f, 0.8f, 0.82f, 1f);

            if (!_viewDirty)
            {
                GUI.DrawTexture(r, _rt, ScaleMode.StretchToFill, false);
                return;
            }

            var old = cam.targetTexture;
            cam.targetTexture = _rt;
            cam.Render();
            cam.targetTexture = old;

            GUI.DrawTexture(r, _rt, ScaleMode.StretchToFill, false);
            _viewDirty = false;
        }

        void BuildNotifyRects(Rect sliderRect, Rect markerRect) 
        {
            _notifyRects.Clear();
            if (_notifies == null || _notifies.Count == 0) return;

            // アイコンの見た目サイズとヒット拡張
            const float iconW = 10f, iconH = 10f;
            const float pad   = 4f;

            float y = markerRect.y + (markerRect.height - iconH) * 0.5f;

            for (int i = 0; i < _notifies.Count; i++) {
                var t = Mathf.Clamp01(_notifies[i].Time / (float)_length);
                float x = Mathf.Lerp(sliderRect.x, sliderRect.xMax, t);

                var vis = new Rect(x - iconW * 0.5f, y, iconW, iconH);                 // 見た目
                var hit = new Rect(vis.x - pad, vis.y - pad, vis.width + pad*2, vis.height + pad*2); // ちょい広い当たり

                _notifyRects.Add((hit, i));
            }
        }
        
        void DrawScrubAndNotifies(Rect sliderRect, Rect markerRect)
        {
            // スクラブ本体
            float tNorm = Mathf.InverseLerp(0, (float)_length, (float)_time);
            float newNorm = GUI.HorizontalSlider(sliderRect, tNorm, 0, 1);
            if (!Mathf.Approximately(newNorm, tNorm))
            {
                _playing = false;
                Seek(newNorm * (float)_length);
            }

            // ノーティファイを描画（バーの少し上）
            var markerY = sliderRect.y - 12f;

            serializedObject.Update(); // Notifies 読み取り
            if (_notifies != null && _notifies.Any())
            {
                foreach (var elem in _notifies)
                {
                    var timeProp = elem.Time;
                    float nt = Mathf.Clamp01((_length <= 0) ? 0 : timeProp / (float)_length);
                    const float offset = 6;
                    float x = Mathf.Lerp(sliderRect.x + 4, sliderRect.xMax - offset, nt);

                    // アイコン描画
                    float w = markerRect.width, h = markerRect.height;
                    var r = new Rect(x - w * 0.5f, markerY, w, h);

                    GUI.DrawTexture(r, _notifyIcon.image, ScaleMode.ScaleToFit, true);
                }
            }
            // 時間表示（任意）
            var labelRect = new Rect(sliderRect.x, sliderRect.y - 28, 140, 16);
            GUI.Label(labelRect, $"{_time:F2} / {_length:F2} s", EditorStyles.miniLabel);
        }

        void HandleNotifyContext()
        {
            var e = Event.current;
            if (e.type != EventType.ContextClick) return;

            // 既存ノーティファイのヒット
            foreach (var (rect, index) in _notifyRects)
            {
                if (rect.Contains(e.mousePosition))
                {
                    ShowDeleteNotifyMenu(index);
                    e.Use();
                    return;
                }
            }
        }

        void ShowDeleteNotifyMenu(int index)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("DeleteNotifyKey"), false, () => DeleteNotifyAt(index));
            menu.ShowAsContext();
        }

        void AddNotifyAtTime(float time)
        {
            serializedObject.Update();
            // Undo
            Undo.RecordObject(target, "Add Notify Key");

            _notifies ??= ((AnimationMontage)target).Notifies;

            var newNotify = new NotifyKey(Mathf.Clamp(time, 0, (float)_length), null);
            _notifies.Add(newNotify);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Repaint();
        }

        void DeleteNotifyAt(int index)
        {
            if (_notifies == null) return;

            serializedObject.Update();
            Undo.RecordObject(target, "Delete Notify Key");

            if (index >= 0 && index < _notifies.Count)
            {
                _notifies.RemoveAt(index);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Repaint();
        }
#endregion

#region Preview Obj Control
        static Bounds CalcBounds(GameObject go)
        {
            if (!go) return new Bounds(Vector3.zero, Vector3.one);
            var rs = go.GetComponentsInChildren<Renderer>(true);
            bool has = false;
            var b = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var r in rs)
            {
                if (!r.enabled) continue;
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
            return has ? b : new Bounds(go.transform.position, Vector3.one);
        }

        void FrameCameraToBounds(Bounds b)
        {
            if (_preview == null) return;
            var cam = _preview.camera;
            var extent = Mathf.Max(0.001f, b.extents.magnitude);
            var fov = Mathf.Max(1f, cam.fieldOfView);
            var dist = extent / Mathf.Sin(fov * Mathf.Deg2Rad * 0.5f);
            _distance = Mathf.Max(0.4f, dist * 1.2f);
            _pivot = b.center;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = Mathf.Max(500f, _distance * 6f);
        }

        static void LightenRenderers(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform c in go.transform) SetLayerRecursively(c.gameObject, layer);
        }
#endregion
    }
}
#endif