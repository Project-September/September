using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NewResult
{
    public class SceneTransitionController : MonoBehaviour
    {
        [SerializeField, Scene] private string _titleSceneName;
        [SerializeField, Scene] private string _lobbySceneName;
        [SerializeField] private Button _titleButton;
        [SerializeField] private Button _lobbyButton;
        
        private void Start()
        {
            _titleButton.onClick.AddListener( () =>
            {
                SceneManager.LoadSceneAsync(_titleSceneName);
            });
            
            _lobbyButton.onClick.AddListener(() =>
            {
                SceneManager.LoadSceneAsync(_lobbySceneName);
            });
        }
    }
}