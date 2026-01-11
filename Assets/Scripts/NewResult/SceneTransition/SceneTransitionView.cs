using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    public abstract class SceneTransitionView : MonoBehaviour
    {
        public bool IsTransitioning { get; private set; }
        
        public async UniTask FadeIn()
        {
            IsTransitioning = true;
            await FadeInPanel(UniTask.CompletedTask);
            IsTransitioning = false;
        }

        public async UniTask FadeIn(UniTask loadingTask)
        {
            IsTransitioning = true;
            await FadeInPanel(loadingTask);
            IsTransitioning = false;
        }

        public async UniTask FadeOut()
        {
            IsTransitioning = true;
            await FadeOutPanel(UniTask.CompletedTask);
            IsTransitioning = false;
        }
        
        public async UniTask FadeOut(UniTask loadingTask)
        {
            IsTransitioning = true;
            await FadeOutPanel(loadingTask);
            IsTransitioning = false;
        }

        protected abstract UniTask FadeInPanel(UniTask loadingTask);
        protected abstract UniTask FadeOutPanel(UniTask loadingTask);
    }
}