using NaughtyAttributes;
using UnityEngine;
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
        
        private void Start()
        {
            _titleButton.onClick.AddListener(async () =>
            {
                _transitionEffect.LoadScene(_titleSceneName);
            });
            
            _lobbyButton.onClick.AddListener(async () =>
            {
                _transitionEffect.LoadScene(_lobbySceneName);
            });
        }
    }
}