using InGame.Common;
using UnityEngine;
using UnityEngine.Playables;

namespace InGame.Player.Ult
{
    [System.Serializable]
    public class AnimationClipPlayerPlayable : PlayableBehaviour
    {
        [SerializeField] private AnimationClip _clip;
        
        private bool _isPlayed;
        private AnimationClipPlayer.PlayableInfo _info;

        public AnimationClip Clip => _clip;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var clipPlayer = playerData as AnimationClipPlayer;
            
            if (clipPlayer == null || _clip == null) return;
            
            if (!_isPlayed)
            {
                clipPlayer.Play(_clip);

                if (!clipPlayer.TryGetPlayableInfo(_clip, out _info))
                {
                    Debug.LogWarning("AnimationClipPlayerPlayable: AnimationClipPlayable is not found");
                    return;
                }
                    
                _isPlayed = true;
            }
            
            var time = playable.GetTime();
            
            _info.SetTime((float)time);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            _info.Disconnect();
            _isPlayed = false;
        }
    }
}