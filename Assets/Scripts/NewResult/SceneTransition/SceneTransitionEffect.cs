using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace September.NewResult
{
    [CreateAssetMenu(fileName = "SceneTransitionEffect", menuName = "ScriptableObjects/SceneTransitionEffect")]
    public class SceneTransitionEffect : ScriptableObject
    {
        [SerializeField] private SceneTransitionView _transitionView;

        public async UniTask LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            var view = Instantiate(_transitionView);
            DontDestroyOnLoad(view);
            
            await view.FadeOut();
            await SceneManager.LoadSceneAsync(sceneName, mode);
            await view.FadeIn();
            
            Destroy(view);
        }

        public async UniTask FadeIn()
        {
            var view = Instantiate(_transitionView);
            await view.FadeIn();
            Destroy(view);
        }

        public async UniTask FadeIn(UniTask backgroundTask)
        {
            var view = Instantiate(_transitionView);
            await view.FadeIn(backgroundTask);
            Destroy(view);
        }
    }
}