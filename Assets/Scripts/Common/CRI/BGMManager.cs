using Cysharp.Threading.Tasks;
using System;
using September.Common;
using September.Common.CRI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CRISound
{
    public static class BGMManager
    {
        private static bool _isInitialized;
        private static string _currentCueName;

        public static event Action OnBGMSwitching;

        public static void Initialize()
        {
            if(_isInitialized)
                return;
            
            _isInitialized = true;

            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        /// <summary>
        /// <see cref="SceneBGMContainer"/> に設定されたBGMを再生します。
        /// </summary>
        public static async UniTaskVoid ChangeBGM(string sceneName)
        {
            Debug.Log($"[BGMManager] ChangeBGM: {sceneName}");

            SceneBGMContainer sceneBGMContainer = await SceneBGMContainer.GetInstance();
            if (!sceneBGMContainer.TryGetBGM(sceneName, out SceneBGM bgm))
            {
                Debug.LogWarning($"[BGMManager] BGM not found for scene: {sceneName}");
                return;
            }

            string newCueName = bgm.BGMType switch
            {
                BGMType.Constant => bgm.GetConstantBGM(),
                BGMType.CharacterData => bgm.GetCharacterBGM(CharacterType.OkabeWright), // Todo: ローカルプレイヤーのキャラクタータイプを取得する
                _ => ""
            };

            if (newCueName == _currentCueName) return;

            if (!string.IsNullOrEmpty(_currentCueName)) StopBGM();

            if (!string.IsNullOrEmpty(newCueName))
            {
                CRIAudio.PlayBGM("ALLCue", newCueName);
                _currentCueName = newCueName;
            }

            OnBGMSwitching?.Invoke();
        }

        public static void StopBGM()
        {
            Debug.Log("[BGMManager] StopBGM");
            CRIAudio.StopBGM("ALLCue", _currentCueName);
            _currentCueName = "";
        }

        private static async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!CuePlayAtomExPlayer.Instance.IsReady)
            {
                await UniTask.WaitUntil(() => CuePlayAtomExPlayer.Instance.IsReady);
            }

            // タイトルに戻った時音量をリセット(SEも含め)
            if (scene.name == "Title")
            {
                CuePlayAtomExPlayer.Instance.ResetCategoryVolume();
            }

            ChangeBGM(scene.name).Forget();
        }
    }
}
