using InGame.Common;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace InGame.Player.Ult
{
    [System.Serializable]
    public class AnimationClipPlayerPlayable : PlayableBehaviour
    {
        [SerializeField] private AnimationClip _clip;
        
        private AnimationClipPlayable _animationClipPlayable;
        private AnimationPlayableOutput _output;
        
        private bool _isPlayed;
        
        public AnimationClip Clip => _clip;

        public override void OnPlayableCreate(Playable playable)
        {
            var graph = playable.GetGraph();
            
            _animationClipPlayable = AnimationClipPlayable.Create(graph, _clip);
            
            if (_clip)
            {
                _animationClipPlayable.SetDuration(_clip.length);
            }
            
            playable.SetInputCount(1);
            playable.SetInputWeight(0, 1);
            playable.ConnectInput(0, _animationClipPlayable, 0);
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var clipPlayer = playerData as AnimationClipPlayer;
            
            if (clipPlayer == null || _clip == null) return;
            
            if (Application.isPlaying)
            {
                if (_isPlayed) return;
                
                clipPlayer.PlayClip(_clip);
                _isPlayed = true;
            }
            else
            {
                // エディタープレビュー用の処理
                // Animatorを直接使用しているためプレイ中と同じ動作になる保証がない点に注意
                // ゲーム中の処理と共通化したいが、そうなるとAnimationClipPlayer側にネットワーク同期可能なSetTime処理を実装する必要がありそう
                
                var time = playable.GetTime();
                
                _animationClipPlayable.SetTime(time);
                
                if (_output.IsOutputNull() || _output.GetTarget() != clipPlayer.Animator)
                {
                    _output = AnimationPlayableOutput.Create(playable.GetGraph(), "AnimationPreviewOutput", clipPlayer.Animator);
                    _output.SetSourcePlayable(playable);
                    _output.SetWeight(1f);
                }
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _isPlayed = false;
        }
    }
}