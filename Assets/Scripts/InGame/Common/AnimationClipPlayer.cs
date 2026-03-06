using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace InGame.Common
{
    /// <summary>
    /// アニメーションをPlayableGraphで再生するコンポーネント。
    /// 永続するモーション（移動など）はBaseレイヤーに設定し、
    /// 一時的なモーションはFullBodyやUpperBodyレイヤーに設定して使う。
    /// RootMotion非対応。
    /// </summary>
    public class AnimationClipPlayer : NetworkBehaviour
    {
        [SerializeField] private List<LayerInfo> _layerInfo;
        [SerializeField, Range(0f, 10f)] private float _graphSpeed = 1f;
        [Header("移動アニメーション")] 
        [SerializeField] private AnimationClip _wait;
        [SerializeField] private AnimationClip _walk;
        [SerializeField] private AnimationClip _run;
        [SerializeField, Range(0f, 2f)] private float _locoWeight = 0f;
        [SerializeField] protected Animator _animator;

        private PlayableGraph _graph;
        private AnimationMixerPlayable _baseMixer;
        private AnimationLayerMixerPlayable _layerMixer;

        private readonly Dictionary<LayerInfo.LayerType, int> _slotOf = new();
        private readonly Dictionary<LayerInfo.LayerType, AnimationClipPlayable> _runtimeClips = new();
        private readonly Dictionary<LayerInfo.LayerType, CancellationTokenSource> _layerCts = new();
        private readonly Dictionary<LayerInfo.LayerType, CancellationTokenSource> _weightBlendCts = new();

        private readonly Dictionary<LayerInfo.LayerType, AnimationClip> _clipOf = new();

        public AnimationMixerPlayable BaseMixer => _baseMixer;

        public bool IsPlayingTargetClip(AnimationClip clip)
        {
            foreach (var kv in _clipOf)
            {
                if (kv.Value == clip && _runtimeClips.TryGetValue(kv.Key, out var p) && p.IsValid())
                {
                    // レイヤー重みもチェック
                    if (_slotOf.TryGetValue(kv.Key, out int slot) && _layerMixer.GetInputWeight(slot) > 0.001f)
                    {
                        // アニメーション完了状態もチェック
                        if (p.GetTime() < p.GetDuration() - 0.01) // まだ再生中
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void Awake()
        {
            if (!_animator && !TryGetComponent(out _animator))
                Debug.LogError("[AnimationClipPlayer] Animator がありません。");
        }

        private void Start()
        {
            if (_layerInfo == null || _layerInfo.Count == 0)
            {
                Debug.LogError("LayerInfo を設定してください。（Base 含む）");
                enabled = false;
                return;
            }

            for (int i = 0; i < _layerInfo.Count; i++)
            {
                var t = _layerInfo[i].Type;
                if (!_slotOf.TryAdd(t, i))
                    Debug.LogWarning($"LayerType {t} が重複しています。最初の定義を採用します。");
            }

            if (!_slotOf.ContainsKey(LayerInfo.LayerType.Base))
            {
                Debug.LogError("Base レイヤーがありません。");
                enabled = false;
                return;
            }

            _graph = PlayableGraph.Create("AnimationClipPlayerGraph");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            var output = AnimationPlayableOutput.Create(_graph, "AnimationOutput", _animator);
            output.SetWeight(1f);

            _layerMixer = AnimationLayerMixerPlayable.Create(_graph, _layerInfo.Count);
            output.SetSourcePlayable(_layerMixer);

            _baseMixer = AnimationMixerPlayable.Create(_graph, 3);
            var baseSlot = _slotOf[LayerInfo.LayerType.Base];
            _graph.Connect(_baseMixer, 0, _layerMixer, baseSlot);
            _layerMixer.SetInputWeight(baseSlot, 1f);
            _layerMixer.SetLayerAdditive((uint)baseSlot, false);

            //各レイヤーの初期設定
            for (int i = 0; i < _layerInfo.Count; i++)
            {
                var li = _layerInfo[i];
                if (li.LayerMask) _layerMixer.SetLayerMaskFromAvatarMask((uint)i, li.LayerMask);
                _layerMixer.SetLayerAdditive((uint)i, li.Additive);
                if (i != baseSlot) _layerMixer.SetInputWeight(i, Mathf.Clamp01(li.Weight));
            }

            var port = 0;
            if (_wait)
            {
                var p = AnimationClipPlayable.Create(_graph, _wait);
                _baseMixer.ConnectInput(port++, p, 0);
            }
            else _baseMixer.SetInputWeight(port++, 0f);

            if (_walk)
            {
                var p = AnimationClipPlayable.Create(_graph, _walk);
                _baseMixer.ConnectInput(port++, p, 0);
            }
            else _baseMixer.SetInputWeight(port++, 0f);

            if (_run)
            {
                var p = AnimationClipPlayable.Create(_graph, _run);
                _baseMixer.ConnectInput(port, p, 0);
            }
            else _baseMixer.SetInputWeight(port, 0f);

            _graph.Play();
        }

        private void Update()
        {
            UpdateLocoBlend(_locoWeight);
            // レイヤー重み（Base以外）
            foreach (var kv in _slotOf)
            {
                if (kv.Key == LayerInfo.LayerType.Base) continue;
                int i = kv.Value;
                _layerMixer.SetInputWeight(i, Mathf.Clamp01(_layerInfo[i].Weight));
            }
        }

        private void LateUpdate()
        {
            _graph.Evaluate(Time.deltaTime * _graphSpeed);
        }
        
        public void SetLocoWeight(float w) => _locoWeight = w;
        
        public float GetTargetLayerWeight(LayerInfo.LayerType layer)
        {
            if (!_slotOf.TryGetValue(layer, out int slot))
            {
                Debug.LogWarning($"未定義のレイヤー {layer}");
                return 0f;
            }

            return _layerInfo[slot].Weight;
        }

        public void PlayClip(AnimationClip clip)
        {
            if (AnimationClipsContainer.Instance.AnimationMontages == null)
            {
                Debug.LogWarning("AnimationClipsContainer Instance is null");
                return;
            }
            
            // AnimationClipsContainer から探して再生
            var index = Array.FindIndex(AnimationClipsContainer.Instance.AnimationMontages,
                x => x.AnimClip.name == clip.name);

            if (index < 0)
            {
                Debug.LogWarning($"AnimationClip {clip.name} is not found in AnimationClipsContainer");
                return;
            }
            
            RPC_PlayAsync(index);
            
            var montage = AnimationClipsContainer.Instance.AnimationMontages[index];
            PlayAsync(montage.AnimClip, montage.TargetLayer, 1f, montage.BlendIn, montage.BlendOut,
                montage.IsAdditive, montage.PlaySpeed).Forget();
        }

        [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false)]
        private void RPC_PlayAsync(int clipIndex)
        {
            var montage = AnimationClipsContainer.Instance.AnimationMontages[clipIndex];
            PlayAsync(montage.AnimClip, montage.TargetLayer, 1f, montage.BlendIn, montage.BlendOut,
                montage.IsAdditive, montage.PlaySpeed).Forget();
        }
        
        /// <summary>
        /// 指定のアニメーションを再生する。
        /// 開始前と終了前にWeightをBlend可能
        /// </summary>
        /// <param name="clip">再生するアニメーション</param>
        /// <param name="layerType">再生するレイヤーマスクの種類（ベースは不可）</param>
        /// <param name="weight">再生するレイヤーの重み</param>
        /// <param name="additive">加算モーションにするか</param>
        /// <param name="external">外部から再生処理を止めるトークン。デフォルトではゲームオブジェクトのトークンに紐づく</param>
        /// <param name="blendIn">アニメーション再生開始時のブレンド</param>
        /// <param name="outBlend">アニメーション再生終了時のブレンド</param>
        private async UniTask<EndClipType> PlayAsync(
            AnimationClip clip,
            LayerInfo.LayerType layerType,
            float weight,
            LayerInfo.Blend blendIn,
            LayerInfo.Blend outBlend,
            bool additive = false,
            float playSpeed = 1f,
            CancellationToken external = default)
        {
            if (!clip) return EndClipType.Failed;
            if (layerType == LayerInfo.LayerType.Base)
            {
                Debug.LogWarning("Base レイヤーには PlayAsync() できません。");
                return EndClipType.Failed;
            }

            if (!_slotOf.TryGetValue(layerType, out int slot))
            {
                Debug.LogWarning($"未定義のレイヤー {layerType}");
                return EndClipType.Failed;
            }

            // 同レイヤーの前回待機をキャンセルして新トークン
            var token = RenewLayerCts(layerType, external);

            var currentW = Mathf.Clamp01(_layerInfo[slot].Weight);
            var useBlendIn = blendIn.BlendTime > 0f;
            var startW = useBlendIn ? currentW : Mathf.Clamp01(weight);
            var targetW = Mathf.Clamp01(weight);

            Play(clip, layerType, startW, additive, playSpeed);

            var liNow = _layerInfo[slot];
            liNow.Weight = startW;
            _layerInfo[slot] = liNow;

            // 再生中 Playable を取得
            if (!_runtimeClips.TryGetValue(layerType, out var played) || !played.IsValid()) return EndClipType.Failed;

            if (useBlendIn)
            {
                try
                {
                    await BlendWeightAsync(blendIn, token, startW, targetW, slot);
                }
                catch (OperationCanceledException)
                {
                    return EndClipType.Interrupted;
                }

                // 最終スナップ
                _layerMixer.SetInputWeight(slot, targetW);
                var liSnap = _layerInfo[slot];
                liSnap.Weight = targetW;
                _layerInfo[slot] = liSnap;
            }

            try
            {
                await WaitClipEndAsync(played, token);
            }
            catch (OperationCanceledException)
            {
                // キャンセル時：まだ自分（played）が刺さっている場合のみ片付け
                if (this != null && _graph.IsValid()
                                 && _runtimeClips.TryGetValue(layerType, out var stillCurrent)
                                 && stillCurrent.Equals(played) && stillCurrent.IsValid())
                {
                    _layerMixer.SetInputWeight(slot, 0f);
                    _layerMixer.DisconnectInput(slot);
                    stillCurrent.Destroy();
                    _runtimeClips.Remove(layerType);

                    var li = _layerInfo[slot];
                    li.Weight = 0f;
                    _layerInfo[slot] = li;
                }

                return EndClipType.Interrupted;
            }

            if (this == null || !_graph.IsValid()) return EndClipType.Interrupted;

            var from = Mathf.Clamp01(_layerInfo[slot].Weight);
            if (outBlend.BlendTime > 0f)
            {
                try
                {
                    await BlendWeightAsync(outBlend, token, from, 0, slot);
                }
                catch (OperationCanceledException)
                {
                    return EndClipType.Interrupted;
                }
            }

            // Out 完了時の最終スナップ → 0
            _layerMixer.SetInputWeight(slot, 0f);
            var liOut = _layerInfo[slot];
            liOut.Weight = 0f;
            _layerInfo[slot] = liOut;

            // 0 になったら “まだ自分が current なら” 接続解除＆破棄
            if (_runtimeClips.TryGetValue(layerType, out var current) && current.Equals(played) && current.IsValid())
            {
                _layerMixer.DisconnectInput(slot);
                current.Destroy();
                _runtimeClips.Remove(layerType);
                _clipOf.Remove(layerType);
            }

            return EndClipType.Complete;
        }

        private async UniTask BlendWeightAsync(LayerInfo.Blend outBlend, CancellationToken token, float from, float to,
            int slot)
        {
            float t = 0f, dur = Mathf.Max(outBlend.BlendTime, 1e-6f);
            var curve = outBlend.BlendCurve;
            while (t < dur)
            {
                token.ThrowIfCancellationRequested();
                t += Time.deltaTime;
                var a = Mathf.Clamp01(t / dur);
                if (curve != null) a = curve.Evaluate(a);
                var w = Mathf.Lerp(from, to, a);

                _layerMixer.SetInputWeight(slot, w);
                var liStep = _layerInfo[slot];
                liStep.Weight = w;
                _layerInfo[slot] = liStep;

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        /// <summary>
        /// 以前使っていたレイヤーの処理が残っていればキャンセル
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="external"></param>
        /// <returns></returns>
        private CancellationToken RenewLayerCts(LayerInfo.LayerType layer, CancellationToken external = default)
        {
            if (_layerCts.TryGetValue(layer, out var old))
            {
                old?.Cancel();
                old?.Dispose();
            }

            var linked =
                CancellationTokenSource.CreateLinkedTokenSource(external, this.GetCancellationTokenOnDestroy());
            _layerCts[layer] = linked;
            return linked.Token;
        }

        private async UniTask WaitClipEndAsync(AnimationClipPlayable p, CancellationToken token)
        {
            const double EPS = 1e-4;
            while (true)
            {
                bool valid;
                try
                {
                    valid = p.IsValid();
                }
                catch
                {
                    break;
                } // 破棄レース保険

                if (!valid) break;

                double dur = 0, tim = 0;
                try
                {
                    dur = p.GetDuration();
                    tim = p.GetTime();
                }
                catch
                {
                    break;
                } // グラフ破棄直後の保険

                if (dur > 0 && tim + EPS >= dur) break;

                // キャンセル時に例外を投げない（Suppress）
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
                if (token.IsCancellationRequested) break;
            }
        }

        private void Play(AnimationClip clip, LayerInfo.LayerType layerType, float weight, bool additive = false, float playSpeed = 1f)
        {
            if (!clip) return;

            if (layerType == LayerInfo.LayerType.Base)
            {
                Debug.LogWarning("Base レイヤーには Play() できません。");
                return;
            }

            if (!_slotOf.TryGetValue(layerType, out int slot))
            {
                Debug.LogWarning($"未定義のレイヤー {layerType}");
                return;
            }

            // 既存接続の後片付け
            if (_runtimeClips.TryGetValue(layerType, out var prev) && prev.IsValid())
            {
                _layerMixer.DisconnectInput(slot);
                prev.Destroy();
            }
            
            _clipOf[layerType] = clip;

            var p = AnimationClipPlayable.Create(_graph, clip);
            p.SetApplyFootIK(false);
            p.SetTime(0);
            p.SetDuration(clip.length);
            p.SetSpeed(playSpeed);

            _layerMixer.ConnectInput(slot, p, 0);
            _layerMixer.SetLayerAdditive((uint)slot, additive);
            _layerMixer.SetInputWeight(slot, Mathf.Clamp01(weight));

            _runtimeClips[layerType] = p;
        }

        private void OnDisable() => SafeDestroy();
        private void OnDestroy() => SafeDestroy();

        private void SafeDestroy()
        {
            if (!_graph.IsValid()) return;

            foreach (var kv in _runtimeClips)
                if (kv.Value.IsValid())
                    kv.Value.Destroy();
            _runtimeClips.Clear();

            _graph.Destroy();
        }

        private void UpdateLocoBlend(float w)
        {
            w = Mathf.Clamp(w, 0f, 2f);
            float wWait, wWalk, wRun;
            if (w < 1f)
            {
                wWait = 1f - w;
                wWalk = w;
                wRun = 0f;
            }
            else
            {
                wWait = 0f;
                wWalk = 2f - w;
                wRun = w - 1f;
            }

            _baseMixer.SetInputWeight(0, wWait);
            _baseMixer.SetInputWeight(1, wWalk);
            _baseMixer.SetInputWeight(2, wRun);
        }
        
        public void SetTopPriorityClip(AnimationClip clip, float time = 1f)
        {
            if (!_slotOf.TryGetValue(LayerInfo.LayerType.TopLayer, out var slot))
            {
                Debug.LogWarning("[AnimationClipPlayer] TopLayer が設定されていません。_layerInfo の最後に追加してください。");
                return;
            }

            // 解除要求
            if (clip == null)
            {
                _layerMixer.SetInputWeight(slot, 0f);

                if (_runtimeClips.TryGetValue(LayerInfo.LayerType.TopLayer, out var current) && current.IsValid())
                {
                    _layerMixer.DisconnectInput(slot);
                    current.Destroy();
                    _runtimeClips.Remove(LayerInfo.LayerType.TopLayer);
                }

                var li0 = _layerInfo[slot];
                li0.Weight = 0f;
                _layerInfo[slot] = li0;
                return;
            }

            if (_runtimeClips.TryGetValue(LayerInfo.LayerType.TopLayer, out var prev) && prev.IsValid())
            {
                _layerMixer.DisconnectInput(slot);
                prev.Destroy();
                _runtimeClips.Remove(LayerInfo.LayerType.TopLayer);
            }

            Play(clip, LayerInfo.LayerType.TopLayer, 1f, additive: false, time);

            var li = _layerInfo[slot];
            li.Weight = 1f; // Update() で毎フレーム反映されるので内部Weightも更新
            _layerInfo[slot] = li;
        }
        
        public async UniTask BlendLayerWeight(
            LayerInfo.LayerType layer,
            float toWeight,
            LayerInfo.Blend blend,
            CancellationToken external = default)
        {
            if (layer == LayerInfo.LayerType.Base)
            {
                Debug.LogWarning("Base レイヤーは SetLocoWeight() で制御してください。");
                return;
            }
            if (!_slotOf.TryGetValue(layer, out int slot))
            {
                Debug.LogWarning($"未定義のレイヤー {layer}");
                return;
            }

            // 進行中のブレンドをキャンセル
            if (_weightBlendCts.TryGetValue(layer, out var old))
            {
                old.Cancel();
                old.Dispose();
            }

            var linked = CancellationTokenSource.CreateLinkedTokenSource(
                external, this.GetCancellationTokenOnDestroy());
            _weightBlendCts[layer] = linked;
            var token = linked.Token;

            float from = Mathf.Clamp01(_layerInfo[slot].Weight);
            float to   = Mathf.Clamp01(toWeight);

            if (Mathf.Approximately(blend.BlendTime, 0f))
            {
                SetLayerWeight(layer, to);
                linked.Dispose();
                _weightBlendCts.Remove(layer);
                return;
            }

            try
            {
                // 既存の補間ルーチンを利用（クラス内の private メソッド）
                await BlendWeightAsync(blend, token, from, to, slot);
            }
            catch (OperationCanceledException)
            {
                // キャンセル時はそのまま終了
                return;
            }
            finally
            {
                if (_weightBlendCts.TryGetValue(layer, out var cts))
                {
                    cts.Dispose();
                    _weightBlendCts.Remove(layer);
                }
            }

            SetLayerWeight(layer, to);
        }
        
        public void SetLayerWeight(LayerInfo.LayerType layer, float weight)
        {
            if (layer == LayerInfo.LayerType.Base)
            {
                Debug.LogWarning("Base レイヤーは SetLocoWeight を使ってください。");
                return;
            }
            if (!_slotOf.TryGetValue(layer, out int slot))
            {
                Debug.LogWarning($"未定義のレイヤー {layer}");
                return;
            }

            var w = Mathf.Clamp01(weight);
            _layerMixer.SetInputWeight(slot, w);      // Playables側に即反映
            var li = _layerInfo[slot];                // 内部状態も更新（Updateで毎フレーム再適用される）
            li.Weight = w;
            _layerInfo[slot] = li;
        }

        /// <summary> 再生したClipが終了または中断されるまで待機 </summary>
        public async UniTask<EndClipType> PlayClipAndWait(AnimationClip clip, CancellationToken ct = default)
        {
            if (AnimationClipsContainer.Instance.AnimationMontages == null)
            {
                Debug.LogWarning("AnimationClipsContainer Instance is null");
                return EndClipType.Failed;
            }
            
            // AnimationClipsContainer から探して再生
            var index = Array.FindIndex(AnimationClipsContainer.Instance.AnimationMontages,
                x => x.AnimClip.name == clip.name);

            if (index < 0)
            {
                Debug.LogWarning($"AnimationClip {clip.name} is not found in AnimationClipsContainer");
                return EndClipType.Failed;
            }
            
            RPC_PlayAsync(index);
            
            var montage = AnimationClipsContainer.Instance.AnimationMontages[index];
            return await PlayAsync(montage.AnimClip, montage.TargetLayer, 1f, montage.BlendIn, montage.BlendOut,
                montage.IsAdditive, montage.PlaySpeed, ct);
        }

        public bool StopClip(AnimationClip clip)
        {
            if (AnimationClipsContainer.Instance.AnimationMontages == null)
            {
                return StopClipLocal(clip);
            }

            // AnimationClipsContainer から探してRPCで停止を全クライアントに通知
            var index = Array.FindIndex(AnimationClipsContainer.Instance.AnimationMontages,
                x => x.AnimClip.name == clip.name);

            if (index >= 0)
            {
                RPC_StopClip(index);
            }

            return StopClipLocal(clip);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_StopClip(int clipIndex)
        {
            var montage = AnimationClipsContainer.Instance.AnimationMontages[clipIndex];
            StopClipLocal(montage.AnimClip);
        }

        private bool StopClipLocal(AnimationClip clip)
        {
            foreach (var kv in _clipOf)
            {
                if (kv.Value == clip && _runtimeClips.TryGetValue(kv.Key, out var p) && p.IsValid())
                {
                    RenewLayerCts(kv.Key);

                    _slotOf.TryGetValue(kv.Key, out int slot);

                    // レイヤーウェイトを0にリセット
                    _layerMixer.SetInputWeight(slot, 0f);
                    var li = _layerInfo[slot];
                    li.Weight = 0f;
                    _layerInfo[slot] = li;

                    if (_runtimeClips.TryGetValue(kv.Key, out var prev) && prev.IsValid())
                    {
                        _layerMixer.DisconnectInput(slot);
                        prev.Destroy();
                    }

                    _clipOf.Remove(kv.Key);
                    _runtimeClips.Remove(kv.Key);

                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public class LayerInfo
    {
        public enum LayerType
        {
            Base = 0, //永続するモーション(移動など)
            FullBody = 1, //一時的な全身モーション
            UpperBody = 2,　//一時的な上半身モーション
            TopLayer = 3, //落下モーションレイヤ、最優先で再生される
        }

        public LayerType Type;
        public AvatarMask LayerMask;
        [Range(0, 1)] public float Weight = 0f;
        public bool Additive = false;

        [Serializable]
        public struct Blend
        {
            public float BlendTime;
            public AnimationCurve BlendCurve;
        }
    }

    public enum EndClipType
    {
        Failed,
        Complete,
        Interrupted
    }
}