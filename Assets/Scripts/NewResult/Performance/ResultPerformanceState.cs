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
        [SerializeField] private AnimationClip _idleAnimationClip;

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
                .ContinueWith(() =>
                {
                    if (!_idleAnimationClip || !_characterIdleAnimator) return;
                    _characterIdleAnimator.PlayInstant(_idleAnimationClip);
                }).Forget();
            
            await UniTask.WhenAny(
                UniTask.WaitUntil(this, o => o._isFinished),
                UniTask.WaitUntil(_playableDirector, p => p.state != PlayState.Playing)
            );
        }

        private void Start()
        {
            if (_characterIdleAnimator == null)
            {
                Debug.LogWarning($"[{nameof(ResultPerformanceState)}] {nameof(_characterIdleAnimator).ToFieldName()}にアニメーターが設定されていません。呼吸モーションのループは実行されません", this);
            }
            
            if (_idleAnimationClip == null)
            {
                Debug.LogWarning($"[{nameof(ResultPerformanceState)}] {nameof(_idleAnimationClip).ToFieldName()}にアニメーションクリップが設定されていません。呼吸モーションのループは実行されません", this);
            }
        }
    }
}