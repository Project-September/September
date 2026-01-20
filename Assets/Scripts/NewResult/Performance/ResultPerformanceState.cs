using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

namespace September.NewResult
{
    [RequireComponent(typeof(PlayableDirector))]
    public class ResultPerformanceState : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _playableDirector;
        [SerializeField] private Animator _characterIdleAnimator;
        [SerializeField] private string _idleStateName = "Idle";

        private bool _isFinished = false;

        private void OnValidate()
        {
            _playableDirector = GetComponent<PlayableDirector>();
        }

        public void Finish() => _isFinished = true;
        public void Play() => _playableDirector.Play();

        public async UniTask WaitFinish()
        {
            UniTask.WaitUntil(_playableDirector, p => p.state != PlayState.Playing)
                .ContinueWith(() => _characterIdleAnimator.Play(_idleStateName)).Forget();
            
            await UniTask.WhenAny(
                UniTask.WaitUntil(this, o => o._isFinished),
                UniTask.WaitUntil(_playableDirector, p => p.state != PlayState.Playing)
            );
        }
    }
}