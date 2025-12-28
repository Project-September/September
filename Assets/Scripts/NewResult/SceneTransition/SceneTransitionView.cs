using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    public abstract class SceneTransitionView : MonoBehaviour
    {
        public async UniTask FadeIn()
        {
            SceneTransitionState.IsTransitioning = true;
            await FadeInPanel();
            SceneTransitionState.IsTransitioning = false;
        }

        public async UniTask FadeIn(UniTask backgroundTask)
        {
            SceneTransitionState.IsTransitioning = true;
            await FadeInPanel(backgroundTask);
            SceneTransitionState.IsTransitioning = false;
        }

        public async UniTask FadeOut()
        {
            SceneTransitionState.IsTransitioning = true;
            await FadeOutPanel();
            SceneTransitionState.IsTransitioning = false;
        }

        protected abstract UniTask FadeInPanel();
        protected abstract UniTask FadeInPanel(UniTask backgroundTask);
        protected abstract UniTask FadeOutPanel();
    }
}