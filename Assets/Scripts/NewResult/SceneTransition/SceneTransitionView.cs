using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    public enum TransitionState
    {
        BeforeClosing,
        Closing,
        Covered,
        Opening,
        Opened,
    }
    
    public abstract class SceneTransitionView : MonoBehaviour
    {
        public bool IsTransitioning { get; private set; }
        public abstract TransitionState State { get; protected internal set; }
        
        [ContextMenu("Fade In")]
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

        [ContextMenu("Fade Out")]
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