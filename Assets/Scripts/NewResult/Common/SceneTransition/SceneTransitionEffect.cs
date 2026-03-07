using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Search;

namespace September.NewResult
{
    /// <summary>
    /// シーン遷移演出を管理するクラス
    /// </summary>
    [CreateAssetMenu(fileName = "SceneTransitionEffect", menuName = "ScriptableObjects/SceneTransitionEffect")]
    public class SceneTransitionEffect : ScriptableObject
    {
        [SerializeField, SearchContext("p: t:SceneTransitionView")] private SceneTransitionView _transitionView;

        private static SceneTransitionView _currentView;
        private static bool IsTransitioning => _currentView != null && _currentView.IsTransitioning;
        
        public TransitionState State => _currentView.State;
        
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
        /// 遷移演出を終了する。既に実行中ならキャンセルする
        /// </summary>
        public async UniTask<bool> TryTransitionIn()
        {
            if (CheckTransitionDuplicated()) return false;
            var view = GetInstance();
            await view.FadeIn();
            Destroy(view.gameObject);
            return true;
        }
        
        /// <summary>
        /// 遷移を開始する。既に実行中ならキャンセルする
        /// </summary>
        public async UniTask<bool> TryTransitionOut()
        {
            if (CheckTransitionDuplicated()) return false;
            var view = GetInstance();
            await view.FadeOut();
            return true;
        }
        
        /// <summary>
        /// 強制的に静止状態に移行する
        /// </summary>
        public void SetHoldState()
        {
            var view = GetInstance();
            view.SetCovered();
        }

        /// <summary>
        /// 引数に指定したステートまで待機
        /// </summary>
        public async UniTask WaitUntilState(TransitionState targetState)
        {
            await UniTask.WaitUntil(targetState,
                state => !_currentView || _currentView.State == state);
        }
    }
}