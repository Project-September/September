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
            await FadeInPanel();
            IsTransitioning = false;
        }

        [ContextMenu("Fade Out")]
        public async UniTask FadeOut()
        {
            IsTransitioning = true;
            await FadeOutPanel();
            IsTransitioning = false;
        }
        
        protected abstract UniTask FadeInPanel();
        protected abstract UniTask FadeOutPanel();
        public abstract void SetCovered();
    }
}