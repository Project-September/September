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

        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            
            // PlayableGraph構築
            _graph = PlayableGraph.Create("CharacterGraph");
            //_graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            // AnimatorControllerPlayableを作成（レイヤー0）
            var controllerPlayable = AnimatorControllerPlayable.Create(_graph, _animator.runtimeAnimatorController);

            // AnimationLayerMixerPlayable（2レイヤー：0 = AnimController, 1 = PlayClip）
            _layerMixer = AnimationLayerMixerPlayable.Create(_graph, 2);
            _layerMixer.ConnectInput(0, controllerPlayable, 0);

            // 出力設定
            var output = AnimationPlayableOutput.Create(_graph, "Output", _animator);
            output.SetSourcePlayable(_layerMixer);

            // スキル用レイヤーを初期化（無効化）
            _layerMixer.SetInputWeight(0, 1f);
            _layerMixer.SetInputWeight(PlayClipLayerIndex, 0f);
            //_layerMixer.SetLayerAdditive(PlayClipLayerIndex, false);

            _graph.Play();
        }

        public void PlayClip(AnimationClip clip, float speed = 1, float startTime = 0, bool useRootMotion = false)
        {
            int index = Array.FindIndex(AnimationClipsContainer.Instance.AnimationMontages, montage => montage.AnimClip == clip);
            
            if (index == -1)
            {
                Debug.LogError("Set the AnimationClip to use in ScriptableObject");
            }
            else
            {
                // 呼び出しクライアントはローカルで即時再生
                PlayClipLocal(new AnimationPlaySpec(index, speed, startTime, useRootMotion));
                // 他クライアントは RPC で同期
                RPCPlayClip(index, speed, startTime, useRootMotion);
            }
        }

        [Rpc(InvokeLocal = false)]
        void RPCPlayClip(int montageIndex, float speed, float startTime, bool useRootMotion)
        {
            PlayClipLocal(new AnimationPlaySpec(montageIndex, speed, startTime, useRootMotion));
        }

        void PlayClipLocal(AnimationPlaySpec playSpec)
        {
            if (!playSpec.Montage.AnimClip)
            {
                Debug.LogError("AnimationClip is null");
                return;
            }
            if (_graph.IsValid() == false)
            {
                Debug.LogError("PlayableGraph is not valid");
                return;
            }

            // AnimationClipPlayable を作成
            var animationClipPlayable = AnimationClipPlayable.Create(_graph, playSpec.Montage.AnimClip);
            animationClipPlayable.SetApplyFootIK(true);
            animationClipPlayable.SetTime(playSpec.StartTime);
            animationClipPlayable.SetDuration(playSpec.Montage.AnimClip.length);
            animationClipPlayable.SetSpeed(playSpec.SpeedRate);

            // ミキサーのスキル用レイヤーに接続（既存がある場合は置き換え）
            _layerMixer.DisconnectInput(PlayClipLayerIndex);
            _layerMixer.ConnectInput(PlayClipLayerIndex, animationClipPlayable, 0);
            _layerMixer.SetInputWeight(PlayClipLayerIndex, 1f);
            _animator.applyRootMotion = playSpec.UseRootMotion;

            // 終了後に戻す
            float t = 0;
            float duration = (playSpec.Montage.AnimClip.length - playSpec.StartTime) / playSpec.SpeedRate;
            Observable.EveryUpdate()
                .TakeUntilDestroy(this)
                .TakeWhile(_ => t < duration)
                .DoOnCompleted(() =>
                {
                    _layerMixer.SetInputWeight(PlayClipLayerIndex, 0f);
                    _layerMixer.DisconnectInput(PlayClipLayerIndex);
                    _animator.applyRootMotion = false;
                })
                .Subscribe(f =>
                {
                    BlendSetWeight(t, duration, playSpec.Montage);
                    t += Time.deltaTime;
                });
        }

        private void BlendSetWeight(float t, float duration, AnimationMontage montage)
        {
            float weight = 1;
            
            if (montage.BlendIn.BlendCurve != null && montage.BlendIn.BlendTime >= t)
            {
                weight = montage.BlendIn.BlendCurve.Evaluate(t / montage.BlendIn.BlendTime);
            }
            else if (montage.BlendOut.BlendCurve != null && montage.BlendOut.BlendTime >= duration - t)
            {
                weight = 1 - montage.BlendOut.BlendCurve.Evaluate((t - duration + montage.BlendOut.BlendTime) / montage.BlendOut.BlendTime);
            }
            
            _layerMixer.SetInputWeight(PlayClipLayerIndex, weight);
        }

        private void OnAnimatorMove()
        {
            if (_animator.applyRootMotion)
            {
                _rb.MovePosition(_rb.position + _animator.deltaPosition);
                _rb.MoveRotation(_rb.rotation * _animator.deltaRotation);
            }
        }

        void OnDestroy()
        { 
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
            Montage = Array.Find(AnimationClipsContainer.Instance.AnimationMontages, montage => montage.AnimClip == clip);
            SpeedRate = speed;
            StartTime = startTime;
            UseRootMotion = useRootMotion;
        }

        public AnimationPlaySpec(int montageIndex, float speed = 1, float startTime = 0, bool useRootMotion = false)
        {
            Montage = (uint)montageIndex < AnimationClipsContainer.Instance.AnimationMontages.Length ? 
                AnimationClipsContainer.Instance.AnimationMontages[montageIndex] : new AnimationMontage();
            SpeedRate = speed;
            StartTime = startTime;
            UseRootMotion = useRootMotion;
        }
    }

    // public struct AnimationPlaySpecNetwork : INetworkStruct
    // {
    //     public int ClipIndex;
    //     public float SpeedRate;
    //     public float StartTime;
    //     public bool UseRootMotion;
    //
    //     public AnimationPlaySpecNetwork(AnimationClip clip, float speedRate = 1, float startTime = 0, bool useRootMotion = false)
    //     {
    //         ClipIndex = Array.FindIndex(AnimationClipsContainer.Instance.AnimationMontages, montage => montage.AnimClip == clip);
    //         SpeedRate = speedRate;
    //         StartTime = startTime;
    //         UseRootMotion = useRootMotion;
    //     }
    //     
    //     public AnimationPlaySpecNetwork(AnimationPlaySpec playSpec)
    //     {
    //         ClipIndex = Array.FindIndex(AnimationClipsContainer.Instance.AnimationMontages, montage => montage.AnimClip == playSpec.Montage.AnimClip);
    //         SpeedRate = playSpec.SpeedRate;
    //         StartTime = playSpec.StartTime;
    //         UseRootMotion = playSpec.UseRootMotion;
    //     }
    // }
}
