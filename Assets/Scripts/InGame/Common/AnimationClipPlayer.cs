using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace InGame.Common
{
    public class AnimationClipPlayer : NetworkBehaviour
    {
        [SerializeField] protected Animator _animator;
        
        private PlayableGraph _graph;
        private AnimationLayerMixerPlayable _layerMixer;
        private const int PlayClipLayerIndex = 1;

        void Start()
        {
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

        [Rpc]
        public void RPC_PlayClip(AnimationPlaySpecNetwork playSpecNetwork)
        {
            PlayClip(new AnimationPlaySpec(playSpecNetwork));
        }

        public void PlayClip(AnimationPlaySpec playSpec)
        {
            //Debug.Log($"Play clip : {clip.name}");
            // スキル用 AnimationClipPlayable を作成
            if (!playSpec.Clip)
            {
                Debug.LogError("AnimationClip is null");
                return;
            }
            if (_graph.IsValid() == false)
            {
                Debug.LogError("PlayableGraph is not valid");
                return;
            }
            var skillPlayable = AnimationClipPlayable.Create(_graph, playSpec.Clip);
            skillPlayable.SetApplyFootIK(true);
            //skillPlayable.SetTime(0);
            //skillPlayable.SetDuration(clip.length);
            //skillPlayable.SetSpeed(1);

            // ミキサーのスキル用レイヤーに接続（既存がある場合は置き換え）
            _layerMixer.DisconnectInput(PlayClipLayerIndex);
            _layerMixer.ConnectInput(PlayClipLayerIndex, skillPlayable, 0);
            _layerMixer.SetInputWeight(PlayClipLayerIndex, 1f);

            // 終了後に戻す
            StartCoroutine(DisableSkillLayerAfter(playSpec.Clip.length));
        }

        private System.Collections.IEnumerator DisableSkillLayerAfter(float time)
        {
            yield return new WaitForSeconds(time);
            _layerMixer.SetInputWeight(PlayClipLayerIndex, 0f);
            _layerMixer.DisconnectInput(PlayClipLayerIndex);
        }

        void OnDestroy()
        { 
            _graph.Destroy();
        }
    }

    public struct AnimationPlaySpec
    {
        public AnimationClip Clip;
        public float SpeedRate;
        public float StartTime;
        public bool UseRootMotion;

        public AnimationPlaySpec(AnimationPlaySpecNetwork specNetwork)
        {
            Clip = (uint)specNetwork.ClipIndex > AnimationClipsContainer.Instance.AnimationClips.Length ? null : AnimationClipsContainer.Instance.AnimationClips[specNetwork.ClipIndex];
            SpeedRate = specNetwork.SpeedRate;
            StartTime = specNetwork.StartTime;
            UseRootMotion = specNetwork.UseRootMotion;
        }
    }

    public struct AnimationPlaySpecNetwork : INetworkStruct
    {
        public int ClipIndex;
        public float SpeedRate;
        public float StartTime;
        public bool UseRootMotion;

        public AnimationPlaySpecNetwork(AnimationClip clip, float speedRate = 1, float startTime = 0, bool useRootMotion = false)
        {
            ClipIndex = Array.FindIndex(AnimationClipsContainer.Instance.AnimationClips, element => element == clip);
            SpeedRate = speedRate;
            StartTime = startTime;
            UseRootMotion = useRootMotion;
        }
    }
}
