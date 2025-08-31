using System;
using Fusion;
using UniRx;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace InGame.Common
{
    public class AnimationClipPlayer : NetworkBehaviour
    {
        [SerializeField] protected Animator _animator;

        private Rigidbody _rb;
        private PlayableGraph _graph;
        private AnimationLayerMixerPlayable _layerMixer;
        private const int PlayClipLayerIndex = 1;
        private IDisposable _playSub;

        void Start()
        {
            _rb = GetComponent<Rigidbody>();

            SetPlayableAPI();
        }

        private void SetPlayableAPI()
        {
            // PlayableGraph構築
            _graph = PlayableGraph.Create("CharacterGraph");

            // AnimatorControllerPlayableを作成（レイヤー0）
            AnimatorControllerPlayable controllerPlayable =
                AnimatorControllerPlayable.Create(_graph, _animator.runtimeAnimatorController);

            // blend用
            _layerMixer = AnimationLayerMixerPlayable.Create(_graph, 2);
            // どのレイヤーに接続するか
            _layerMixer.ConnectInput(0, controllerPlayable, 0);

            // 出力設定
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Output", _animator);
            // LayerMixerで混ぜた結果をAnimatorに流し込む
            output.SetSourcePlayable(_layerMixer);

            // スキル用レイヤーを初期化（無効化）
            _layerMixer.SetInputWeight(0, 1f);
            // こちらは無効化
            _layerMixer.SetInputWeight(PlayClipLayerIndex, 0f);

            _graph.Play();
        }

        public void PlayClip(AnimationClip clip, float speed = 1, float startTime = 0,
            bool useRootMotion = false)
        {
            if (AnimationClipsContainer.Instance == null)
            {
                Debug.LogWarning("AnimationClipsContainer not loaded yet.");
                return;
            }

            int index = Array.FindIndex(AnimationClipsContainer.Instance.AnimationMontages,
                m => m.AnimClip == clip);

            if (index == -1)
            {
                Debug.LogError("Set the AnimationClip to use in ScriptableObject");
                return;
            }

            // 送信前に自ローカルで即時再生（従来通り）
            PlayClipLocal(new AnimationPlaySpec(index, speed, startTime, useRootMotion));

            // ★ 開始Tickを送る
            int startTick = Runner.Tick;
            RPCPlayClip(index, speed, startTime, startTick);
        }

        [Rpc(InvokeLocal = false)]
        void RPCPlayClip(int montageIndex, float speed, float startTime, int startTick)
        {
            // 受信側でも、同じmontageIndexからメタを引く
            PlayClipLocal(new AnimationPlaySpec(montageIndex, speed, startTime, useRootMotion: false),
                startTick); // ※次のオーバーロード
        }

        void PlayClipLocal(AnimationPlaySpec spec, int? startTick = null)
        {
            var montage = spec.Montage;
            if (!montage.AnimClip || !_graph.IsValid())
                return;

            // クリップPlayable
            var clipPlayable = AnimationClipPlayable.Create(_graph, montage.AnimClip);
            clipPlayable.SetApplyFootIK(true);

            // ★ Tick補正（同期）
            if (startTick.HasValue)
            {
                int dtTicks = Runner.Tick - startTick.Value;
                double dt = dtTicks * Runner.DeltaTime; // Fusionのデルタ（シミュレーション時間）
                double time = spec.StartTime + dt * spec.SpeedRate;
                // クリップ範囲内に丸める（必要に応じて）
                time = Math.Max(0, Math.Min(time, montage.AnimClip.length));
                clipPlayable.SetTime(time);
            }
            else
            {
                clipPlayable.SetTime(spec.StartTime);
            }

            clipPlayable.SetDuration(montage.AnimClip.length);
            clipPlayable.SetSpeed(spec.SpeedRate);

            // レイヤー差し替え
            _layerMixer.DisconnectInput(PlayClipLayerIndex);
            _layerMixer.ConnectInput(PlayClipLayerIndex, clipPlayable, 0);

            // ★ ここが同期の要：IsAdditive/Maskはmontageから取得（RPCで送らない）
            bool isAdditive = montage.IsAdditive;
            _layerMixer.SetLayerAdditive(PlayClipLayerIndex, isAdditive);
            if (montage.AvatarMask != null)
                _layerMixer.SetLayerMaskFromAvatarMask(PlayClipLayerIndex, montage.AvatarMask);

            // ウェイト初期化（安全のため毎回明示）
            _layerMixer.SetInputWeight(0, 1f);
            _layerMixer.SetInputWeight(PlayClipLayerIndex, 0f); // 0開始→Blendで上げる

            // RootMotionはAdditiveではオフ
            _animator.applyRootMotion = (!isAdditive) && montage.UseRootMotion;

            // 連打対策：前の購読を破棄
            _playSub?.Dispose();

            // 長さ（速度込み）：Tick補正しても終了時刻は共通
            float remain = montage.AnimClip.length - (float)clipPlayable.GetTime();
            if (remain <= 0f)
            {
                _layerMixer.SetInputWeight(PlayClipLayerIndex, 0f);
                _layerMixer.DisconnectInput(PlayClipLayerIndex);
                _animator.applyRootMotion = false;
                return;
            }

            float duration = remain / Mathf.Max(0.0001f, spec.SpeedRate);
            float t = 0f;

            _playSub = Observable.EveryUpdate()
                .TakeUntilDestroy(this)
                .TakeWhile(_ => t < duration)
                .DoOnCompleted(() =>
                {
                    _layerMixer.SetInputWeight(PlayClipLayerIndex, 0f);
                    _layerMixer.DisconnectInput(PlayClipLayerIndex);
                    _animator.applyRootMotion = false;
                })
                .Subscribe(_ =>
                {
                    BlendSetWeight(t, duration, montage, isAdditive);
                    t += Runner != null ? Runner.DeltaTime : Time.deltaTime;
                });
        }

        private void BlendSetWeight(float t, float duration, AnimationMontage montage, bool isAdditive = false)
        {
            float weight = 1;

            if (montage.BlendIn.BlendCurve != null && montage.BlendIn.BlendTime >= t)
            {
                weight = montage.BlendIn.BlendCurve.Evaluate(t / montage.BlendIn.BlendTime);
            }
            else if (montage.BlendOut.BlendCurve != null && montage.BlendOut.BlendTime >= duration - t)
            {
                weight = 1 -
                         montage.BlendOut.BlendCurve.Evaluate((t - duration + montage.BlendOut.BlendTime) /
                                                              montage.BlendOut.BlendTime);
            }

            _layerMixer.SetInputWeight(PlayClipLayerIndex, weight);

            if (!isAdditive)
                _layerMixer.SetInputWeight(0, 1f);
        }

        private void OnAnimatorMove()
        {
            if (_animator.applyRootMotion && _rb != null)
            {
                _rb.MovePosition(_rb.position + _animator.deltaPosition);
                _rb.MoveRotation(_rb.rotation * _animator.deltaRotation);
            }
        }

        void OnDestroy()
        {
            _playSub?.Dispose();
            if (_graph.IsValid())
                _graph.Destroy();
        }
    }

    public struct AnimationPlaySpec
    {
        public AnimationMontage Montage;
        public float SpeedRate;
        public float StartTime;

        public bool UseRootMotion;

        public AnimationPlaySpec(AnimationClip clip, float speed = 1, float startTime = 0, bool useRootMotion = false)
        {
            Montage = Array.Find(AnimationClipsContainer.Instance.AnimationMontages,
                montage => montage.AnimClip == clip);
            SpeedRate = speed;
            StartTime = startTime;
            UseRootMotion = useRootMotion;
        }

        public AnimationPlaySpec(int montageIndex, float speed = 1, float startTime = 0,
            bool useRootMotion = false)
        {
            Montage = (uint)montageIndex < AnimationClipsContainer.Instance.AnimationMontages.Length
                ? AnimationClipsContainer.Instance.AnimationMontages[montageIndex]
                : new AnimationMontage();
            SpeedRate = speed;
            StartTime = startTime;
            UseRootMotion = useRootMotion;
        }
    }
}