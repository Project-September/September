using System;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace September.NewResult
{
    public class ResultTest : MonoBehaviour
    {
        [SerializeField, Scene] private string _startSceneName;
        [SerializeField, Scene] private string _endSceneName;
        [SerializeField] private float _delay = 1f;
        [SerializeField] private SceneTransitionEffect _sceneTransitionEffect;
        
        private void Start()
        {
            Perform().Forget();
        }

        private async UniTask Perform()
        {
            await SceneManager.LoadSceneAsync(_startSceneName, LoadSceneMode.Additive);
            await UniTask.Delay(TimeSpan.FromSeconds(_delay));
            await _sceneTransitionEffect.TryTransitionOut();
            SceneManager.LoadSceneAsync(_endSceneName);
        }
    }
}