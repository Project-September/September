using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using September.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace September.NewResult
{
    public class SceneTransitionController : MonoBehaviour
    {
        [SerializeField, Scene] private string _titleSceneName;
        [SerializeField, Scene] private string _lobbySceneName;
        [SerializeField] private Button _titleButton;
        [SerializeField] private Button _lobbyButton;
        [SerializeField] private SceneTransitionEffect _transitionEffect;
        [SerializeField] private MenuActiveController _activeController;
        
        private void Start()
        {
            _titleButton.onClick.AddListener(() =>
            {
                _activeController.Deactivate();
                Title().Forget();
            });
            
            _lobbyButton.onClick.AddListener(() =>
            {
                _activeController.Deactivate();
                Title().Forget();
            });
        }

        private async UniTask Title()
        {
            await _transitionEffect.TryTransitionOut();
            await SceneManager.LoadSceneAsync(_titleSceneName);
            if (NetworkManager.Instance)
            {
                await NetworkManager.Instance.InitializeRunner();
            }
            await _transitionEffect.TryTransitionIn();
            ShowCursor();
        }
        
        private static void ShowCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}