using Cysharp.Threading.Tasks;
using September.NewResult;
using UnityEngine;
using UnityEngine.Events;

namespace September.Common
{
    public class Fader : MonoBehaviour
    {
        [SerializeField] private SceneTransitionEffect _transitionEffect;

        [SerializeField] private UnityEvent OnFadeOuted;

        public void StartFade()
        {
            Fade().Forget();
        }

        private async UniTaskVoid Fade()
        {
            await _transitionEffect.TryTransitionOut();
            OnFadeOuted.Invoke();
            await _transitionEffect.TryTransitionIn();
        }
    }
}