using Fusion;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace InGame.Player.Hatano
{
    public class HatanoSequenceManager : NetworkBehaviour
    {
        [SerializeField] private PlayableDirector _director;
        [SerializeField] private TimelineAsset _startTimeline;
        [SerializeField] private TimelineAsset _endTimeline;
        
        private Animator _animator;

        public override void Spawned()
        {
            _animator = GetComponentInChildren<Animator>();
        }

        public bool IsSequencePlaying()
        {
            return _director.state == PlayState.Playing;
        }

        public void SignalStart()
        {
            _animator.applyRootMotion = true;
        }

        public void SignalEnd()
        {
            _animator.applyRootMotion = false;
            
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.Euler(Vector3.zero);
        }
        
        [Rpc]
        public void RPC_SetEndTimeline()
        {
            _director.playableAsset = _endTimeline;
            _director.Play();
        }

        [Rpc]
        public void RPC_SetStartTimeline()
        {
            _director.playableAsset = _startTimeline;
        }
    }
}
