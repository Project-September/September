using Cysharp.Threading.Tasks;
using NaughtyAttributes;
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
        
        private void Start()
        {
            _titleButton.onClick.AddListener(async () =>
            {
                await FadePanel.FadeOut();
                await SceneManager.LoadSceneAsync(_titleSceneName).ToUniTask();
                await FadePanel.FadeIn();
            });
            
            _lobbyButton.onClick.AddListener(async () =>
            {
                await FadePanel.FadeOut();
                await SceneManager.LoadSceneAsync(_lobbySceneName).ToUniTask();
                await FadePanel.FadeIn();
            });
        }
    }
}