using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace September.NewResult
{
    [CreateAssetMenu(fileName = "SceneTransitionEffect", menuName = "ScriptableObjects/SceneTransitionEffect")]
    public class SceneTransitionEffect : ScriptableObject
    {
        [SerializeField] private SceneTransitionView _transitionView;

        private static SceneTransitionView _currentView;
        private static bool IsTransitioning => _currentView != null && _currentView.IsTransitioning;
        
        private SceneTransitionView GetInstance()
        {
            if (_currentView == null)
            {
                _currentView = Instantiate(_transitionView);
                DontDestroyOnLoad(_currentView.gameObject);
                return _currentView;
            }

            return _currentView;
        }
        
        private static bool CheckTransitionDuplicated()
        {
            if (IsTransitioning)
            {
                Debug.LogWarning("シーン遷移演出が重複して呼び出されました", _currentView);
            }
            return IsTransitioning;
        }

        /// <summary>
        /// 遷移先でのロード処理が不要な場合にのみ使用してください
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="mode"></param>
        public async UniTask LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (CheckTransitionDuplicated()) return;
            var view = GetInstance();
            DontDestroyOnLoad(view);
            
            await view.FadeOut();
            await SceneManager.LoadSceneAsync(sceneName, mode);
            await view.FadeIn();
            
            Destroy(view.gameObject);
        }
        
        public async UniTask<bool> TryFadeIn()
        {
            if (CheckTransitionDuplicated()) return false;
            var view = GetInstance();
            await view.FadeIn();
            Destroy(view.gameObject);
            return true;
        }
        
        public async UniTask<bool> TryFadeIn(UniTask loadingTask)
        {
            if (CheckTransitionDuplicated()) return false;
            var view = GetInstance();
            await view.FadeIn(loadingTask);
            Destroy(view.gameObject);
            return true;
        }

        /// <summary>
        /// 遷移先でロード処理を行う場合にのみ使用してください
        /// </summary>
        /// <returns></returns>
        public async UniTask<bool> TryFadeOut()
        {
            if (CheckTransitionDuplicated()) return false;
            var view = GetInstance();
            await view.FadeOut();
            return true;
        }
        
        public async UniTask<bool> TryFadeOut(UniTask loadingTask)
        {
            if (CheckTransitionDuplicated()) return false;
            var view = GetInstance();
            await view.FadeOut(loadingTask);
            return true;
        }
    }
}