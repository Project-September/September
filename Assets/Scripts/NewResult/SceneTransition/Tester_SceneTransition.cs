using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace September.NewResult
{
    public class Tester_SceneTransition : MonoBehaviour
    {
        [SerializeField, Scene] string _sceneToLoad;
        [SerializeField] private SceneTransitionEffect _transitionEffect;
        [SerializeField] private Button _button;

        private async void Start()
        {
            await _transitionEffect.TryFadeIn(UniTask.Delay(1000));
            
            _button.onClick.AddListener(async () =>
            {
                var success = await _transitionEffect.TryFadeOut();
                if (success)
                {
                    await SceneManager.LoadSceneAsync(_sceneToLoad);
                }
            });
        }
    }
}