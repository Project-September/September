using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

namespace September.NewResult
{
    [RequireComponent(typeof(PlayableDirector))]
    public class ResultPerformanceState : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _playableDirector;

        private bool _isFinished = false;

        private void OnValidate()
        {
            _playableDirector = GetComponent<PlayableDirector>();
        }

        public void Finish() => _isFinished = true;
        public void Play() => _playableDirector.Play();

        public async UniTask WaitFinish()
        {
            await UniTask.WhenAny(
                UniTask.WaitUntil(this, o => o._isFinished),
                UniTask.WaitUntil(_playableDirector, p => p.state != PlayState.Playing)
            );
        }
    }
}