using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;

namespace InGame.Common.AnimationMontage
{
    /// <summary>
    /// Animator Controller を土台に、Playables で Montage をレイヤー再生する
    /// </summary>
    [DisallowMultipleComponent]
    public class MontagePlayer : MonoBehaviour
    {
        [Header("Required")]
        [SerializeField] private Animator _animator;

        [Header("LayerAvatarMasks")]
        [SerializeField] private AvatarMask[] _layerAvatarMask;

        private PlayableGraph _graph;
        private AnimationLayerMixerPlayable _layerMixer;   // 入力 0 : Base, max : FullBody
        private AnimatorControllerPlayable _controllerPlayable;
        private Slot[] _slots; // clip を再生する layer は Slot を用いる

        // 再生中の MontageHandle
        private MontageHandle[] _activeHandles;
        // Notify 進捗
        private readonly List<int> _firedNotifyIndices = new();

        private bool _graphReady;

        private const int LBase = 0;
        private int LFull;

        void OnEnable()
        {
            InitGraph();
            _graph.Play();
        }

        void OnDisable()
        {
            if (_graph.IsValid())
            {
                _graph.Stop();
            }
        }

        void OnDestroy()
        {
            foreach (var handle in _activeHandles)
            {
                handle?.Kill();
            }
            
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        void InitGraph()
        {
            if (_graphReady) return;
            if (_animator == null)
            {
                Debug.LogError("Animator が無い");
                return;
            }

            _graph = PlayableGraph.Create("MontagePlayableGraph");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            // レイヤー数は AvatarMaskの数 + AnimatorController + FullBody
            _layerMixer = AnimationLayerMixerPlayable.Create(_graph, _layerAvatarMask.Length + 2, true);

            // Base: AnimatorController を Playable に
            var controller = _animator.runtimeAnimatorController;
            if (controller == null)
            {
                Debug.LogWarning("AnimatorController が設定されてない Baseは空");
            }
            else
            {
                _controllerPlayable = AnimatorControllerPlayable.Create(_graph, controller);
                _graph.Connect(_controllerPlayable, 0, _layerMixer, LBase);
                _layerMixer.SetInputWeight(LBase, 1f);
            }

            // avatar mask の layer を設定
            for (int i = 0; i < _layerAvatarMask.Length; i++)
            {
                if (!_layerAvatarMask[i]) continue;
                _layerMixer.SetLayerMaskFromAvatarMask((uint)i + 1, _layerAvatarMask[i]);
            }
            
            // full body の layer index
            LFull = _layerAvatarMask.Length + 1;
            
            _slots = new Slot[LFull].Select(_ => new Slot(_graph)).ToArray();
            for (int layerIndex = 1; layerIndex <= LFull; layerIndex++)
            {
                _graph.Connect(_slots[layerIndex - 1].Mixer, 0, _layerMixer, layerIndex);
                _layerMixer.SetInputWeight(layerIndex, 0);
            }
            _activeHandles = new MontageHandle[LFull];

            var output = AnimationPlayableOutput.Create(_graph, "AnimationOutput", _animator);
            output.SetSourcePlayable(_layerMixer);

            _graphReady = true;
        }

#region Public API
        /// <summary>
        /// DOTween風フルエントハンドル。OnCompleteなどを設定でき、await も可能。
        /// </summary>
        public sealed class MontageHandle
        {
            internal ulong Id;
            internal CancellationTokenSource Cts;
            internal Action OnCompleteAction, OnKillAction;
            internal Action<float> OnUpdateAction; // 0..1
            internal Action<NotifyKey> OnNotifyAction;
            internal UniTaskCompletionSource Tcs; // UniTask
            internal bool PostProcessed;

            public MontageHandle OnUpdate(Action<float> cb) { OnUpdateAction += cb; return this; }
            public MontageHandle OnComplete(Action cb) { OnCompleteAction += cb; return this; }
            public MontageHandle OnKill(Action cb) { OnKillAction += cb; return this; }
            public MontageHandle OnNotify(Action<NotifyKey> cb) { OnNotifyAction += cb; return this; }

            public ulong MontageId => Id;
            /// <summary>await で終了待ち（Kill/中断でも完了扱いにしたいなら手前で分岐して）</summary>
            public UniTask ToUniTask() => Tcs.Task;
            /// <summary>中断</summary>
            public void Kill()
            {
                if (Cts == null || Cts.IsCancellationRequested) return;
                Cts.Cancel();
                OnKillAction?.Invoke();
            }
        }

        /// <summary>
        /// Montage 再生。sectionName が null なら Clip 全体。Upper/Full は Montage 側の LayerType を使う。
        /// full body レイヤーは一番「上」なので、他を押しのける。
        /// </summary>
        public MontageHandle PlayMontage(AnimationMontage montage, string sectionName = null)
        {
            InitGraph();
            if (!_graph.IsValid() || montage == null || montage.Clip == null)
            {
                Debug.LogWarning("[MontagePlayablePlayer] 再生できない入力。");
                return new MontageHandle { Tcs = new UniTaskCompletionSource() };
            }

            ResolveSection(montage, sectionName, out double start, out double end);
            var duration = end > start ? (end - start) : montage.Clip.length;
            var loop = montage.Loop;
            var rate = montage.PlayRate;

            // 既存レイヤーの Playable を差し替え
            var clipPlayable = AnimationClipPlayable.Create(_graph, montage.Clip);
            clipPlayable.SetApplyFootIK(true);
            clipPlayable.SetTime(start);
            clipPlayable.SetSpeed(rate);
            clipPlayable.SetDuration(duration);

            int avatarMaskIndex = !montage.SelectedMask ? -1 : Array.FindIndex(_layerAvatarMask, mask => mask == montage.SelectedMask);
            int layerIndex = avatarMaskIndex == -1 ? LFull : avatarMaskIndex + 1;

            // Handle 準備
            var handle = new MontageHandle
            {
                Id = montage.Id,
                Cts = new CancellationTokenSource(),
                Tcs = new UniTaskCompletionSource()
            };
            
            // Active Handle の整理
            _activeHandles[layerIndex - 1]?.Kill();
            _activeHandles[layerIndex - 1] = handle;

            // slot 内の weight
            _slots[layerIndex - 1].ReplacePlayableAndBlendWeight(_graph, clipPlayable, montage.BlendIn, handle.Cts.Token).Forget();
            // 再生 coroutine
            RunMontageCoroutine(montage, clipPlayable, layerIndex, start, duration, loop, handle).Forget();

            return handle;
        }

        public void StopMontage(ulong montageId)
        {
            foreach (var handle in _activeHandles)
            {
                if (handle == null) continue;
                
                if (handle.MontageId == montageId)
                {
                    handle.Kill();
                }
            }
        }

        public ulong[] GetActiveMontageId()
        {
            return _activeHandles.Where(handle => handle != null).Select(handle => handle.Id).ToArray();
        }
#endregion

#region 内部処理
        async UniTask RunMontageCoroutine(
            AnimationMontage montage,
            AnimationClipPlayable clipPlayable,
            int layerIndex,
            double startTime,
            double sectionDuration,
            bool loop,
            MontageHandle handle)
        {
            var ct = handle.Cts.Token;

            // BlendIn
            float blendInTime = montage.BlendIn.BlendTime;
            var blendInCurve = montage.BlendIn.BlendCurve ?? AnimationCurve.Linear(0, 0, 1, 1);

            // BlendOut
            float blendOutTime = montage.BlendOut.BlendTime;
            var blendOutCurve = montage.BlendOut.BlendCurve ?? AnimationCurve.Linear(0, 1, 1, 0);

            // 通知リセット
            _firedNotifyIndices.Clear();
            // 後処理登録
            handle.OnKillAction += MontagePostProcess;
            // レイヤーウェイトを上げる
            TweenWeight(layerIndex, 0f, 1f, blendInTime, blendInCurve, ct).Forget();

            var clipLen = (float)sectionDuration;
            float tLocal = 0f; // 0..clipLen
            double lastTime = _graph.GetRootPlayable(0).GetTime();
            bool blendOuted = blendOutTime <= 0;

            // 再生ループ
            while (!ct.IsCancellationRequested)
            {
                // 経過時間
                double now = _graph.GetRootPlayable(0).GetTime();
                float dt = (float)(now - lastTime);
                lastTime = now;

                // 進捗
                tLocal += dt * montage.PlayRate;
                var norm = Mathf.Clamp01(tLocal / clipLen);
                handle.OnUpdateAction?.Invoke(norm);

                // Notify 発火
                FireNotifies(montage, startTime + tLocal, handle);

                // Blend Out 判定
                if (!blendOuted && tLocal >= clipLen - blendOutTime)
                {
                    TweenWeight(layerIndex, 1f, 0f, blendOutTime, blendOutCurve, ct).Forget();
                    blendOuted = true;
                }

                // End 判定
                if (tLocal >= clipLen)
                {
                    if (loop)
                    {
                        tLocal = 0f;
                        clipPlayable.SetTime(startTime);
                        _firedNotifyIndices.Clear();
                        // ループ境界のNotifyは次フレームで拾う
                    }
                    else break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow();
            }
            
            MontagePostProcess();

            void MontagePostProcess()
            {
                Debug.Log("montagePostProcess");
                if (handle.PostProcessed) return;
                
                handle.PostProcessed = true;
                _layerMixer.SetInputWeight(layerIndex, 0f);

                // レイヤーの Playable を無効化
                if (clipPlayable.IsValid()) clipPlayable.Destroy();

                if (ct.IsCancellationRequested)
                {
                    handle.Tcs.TrySetCanceled();
                }
                else
                {
                    handle.OnCompleteAction?.Invoke();
                    handle.Tcs.TrySetResult();
                }

                // 再生が終わったら null
                if (ReferenceEquals(_activeHandles[layerIndex - 1], handle))
                    _activeHandles[layerIndex - 1] = null;
            }
        }

        async UniTask TweenWeight(int layer, float from, float to, float time, AnimationCurve curve, CancellationToken ct)
        {
            if (time <= 0f)
            {
                _layerMixer.SetInputWeight(layer, to);
                return;
            }

            float t = 0f;
            try
            {
                while (t < time && !ct.IsCancellationRequested)
                {
                    t += Time.deltaTime;
                    float x = Mathf.Clamp01(t / time);
                    float y = Mathf.Clamp01(curve.Evaluate(x));
                    float w = Mathf.LerpUnclamped(from, to, y);
                    _layerMixer.SetInputWeight(layer, w);
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            finally
            {
                _layerMixer.SetInputWeight(layer, to);
            }
        }

        void FireNotifies(AnimationMontage montage, double absTime, MontageHandle handle)
        {
            var notifies = montage.Notifies;
            if (notifies == null || notifies.Count == 0) return;

            for (int i = 0; i < notifies.Count; i++)
            {
                if (_firedNotifyIndices.Contains(i)) continue;
                var key = notifies[i];

                // key.Time は Clip 時間基準想定
                if (absTime >= key.Time)
                {
                    _firedNotifyIndices.Add(i);

                    try
                    {
                        key.Event?.Execute(this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, this);
                    }

                    handle.OnNotifyAction?.Invoke(key);
                }
            }
        }

        void ResolveSection(AnimationMontage montage, string sectionName, out double start, out double end)
        {
            start = 0;
            end = montage.Clip.length;

            var sections = montage.Sections;
            if (sections != null && sections.Length > 0)
            {
                if (!string.IsNullOrEmpty(sectionName))
                {
                    foreach (var s in sections)
                    {
                        if (string.Equals(s.Name, sectionName, StringComparison.Ordinal))
                        {
                            start = s.StartTime;
                            end = Mathf.Max(s.StartTime, s.EndTime);
                            return;
                        }
                    }
                }
                // sectionName が無ければ最初の定義を優先する運用もアリ。ここでは全体再生をデフォルトに。
            }
        }
#endregion
        
        /// <summary> 同じ Avatar Layer の中で Montage 同士をBlendする </summary>
        class Slot
        {
            private AnimationClipPlayable _incoming;
            
            public AnimationMixerPlayable Mixer { get; }
            public AnimationClipPlayable Current { get; private set; }

            public Slot(PlayableGraph graph)
            {
                Mixer = AnimationMixerPlayable.Create(graph, 2);
                Mixer.SetInputWeight(0, 0);
                Mixer.SetInputWeight(1, 0);
            }

            public async UniTask ReplacePlayableAndBlendWeight(PlayableGraph graph, AnimationClipPlayable newPlayable, Blend blend, CancellationToken ct)
            {
                // current が無い || Blend しない場合は切り替え
                if (!Current.IsValid() || blend.BlendTime <= 0)
                {
                    if (Current.IsValid()) Current.Destroy();
                    Current = newPlayable;
                    graph.Connect(Current, 0, Mixer, 0);
                    Mixer.SetInputWeight(0, 1);
                    Mixer.SetInputWeight(1, 0);
                    return;
                }

                if (_incoming.IsValid())
                {
                    Current = _incoming;
                    graph.Connect(Current, 0, Mixer, 0);
                    _incoming.Destroy();
                }

                _incoming = newPlayable;
                graph.Connect(_incoming, 0, Mixer, 1);
                
                float t = 0f;
                
                while (t < blend.BlendTime && !ct.IsCancellationRequested)
                {
                    t += Time.deltaTime;
                    float w = Mathf.LerpUnclamped(0, 1, Mathf.Clamp01(blend.BlendCurve.Evaluate(Mathf.Clamp01(t / blend.BlendTime))));
                    Mixer.SetInputWeight(0, w);
                    Mixer.SetInputWeight(1, 1 - w);
                    try { await UniTask.Yield(PlayerLoopTiming.Update, ct); }
                    catch { break; }
                }
                
                Current = _incoming;
                graph.Connect(Current, 0, Mixer, 0);
                if (_incoming.IsValid()) _incoming.Destroy();
                Mixer.SetInputWeight(0, 1);
                Mixer.SetInputWeight(1, 0);
            }
        }
    }
}
