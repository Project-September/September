using Cysharp.Threading.Tasks;
using Fusion;
using September.Common;
using System;
using UnityEngine.SceneManagement;

namespace CRISound
{
    public class BGMManager
    {
        private static bool _isInitialized;
        private static string _currentCueName;
        private static bool _isWaiting = false;
        private static string _nextCueName = null;

        public static event Action<string, string> OnBGMSwiching;

        public static void Initialize()
        {
            if(_isInitialized)
                return;
            
            _isInitialized = true;

            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }



        /// <summary>
        /// "Title", "Lobby", "InGameMock", "Field", "Result"のいずれかで指定
        /// </summary>
        /// <param name="sceneName"></param>
        public static async void ChangeBGM(string sceneName, CharacterType characterType = default)
        {
            //if (sceneName == "InGameMock") return;
            // 待機フラグが立っているときは現在のBGMを止め、解除されるまで流さない
            string newCueName = GetCueNameByScene(sceneName);
            _isWaiting = GetWaitFlagByScene(sceneName);

            if (_isWaiting)
            {
                CRIAudio.StopBGM("ALLCue", _currentCueName);
                await UniTask.WaitUntil(() => !_isWaiting);
            }

            // ここでイベントを発火 インゲーム中はPlayerAudioControllerからBGMを各々指定
            if (!string.IsNullOrEmpty(_nextCueName) || sceneName == "InGameMock" || sceneName == "Field")
            {
                OnBGMSwiching?.Invoke(sceneName, newCueName);
                return;
            }

            {
                if (!string.IsNullOrEmpty(newCueName))
                {
                    CRIAudio.PlayBGM("ALLCue", newCueName);
                    _currentCueName = newCueName;
                }
            }
        }

        //public static bool GetWaitFlag()
        //{
        //    return _isWaiting;
        //}

        /// <summary> 待機フラグを false にする </summary>
        public static void ReleseFlag()
        {
            _isWaiting = false;
        }

        /// <summary> 次に流すBGMのキューの名前を設定 </summary>
        /// <param name="cueName"></param>
        //public static void SetNextCueName(string cueName)
        //{
        //    _nextCueName = cueName;
        //}

        private static async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!CuePlayAtomExPlayer.Instance.IsReady)
            {
                await UniTask.WaitUntil(() => CuePlayAtomExPlayer.Instance.IsReady);
            }

            ChangeBGM(scene.name);
        }

        private static string GetCueNameByScene(string sceneName)
        {
            return sceneName switch
            {
                "Title" => SoundCues.BGM.Title_01.Name,
                "Lobby" => SoundCues.BGM.Select_01_Loop.Name,
                //"Lobby" => SoundCues.BGM.Haruku_01.Name,
                //"InGameMock" => SoundCues.BGM.Ingame_01.Name,
                //"Field" => SoundCues.BGM.Ingame_01.Name,
                "Result" => SoundCues.BGM.Result_01_Loop.Name,
                _ => "",
            };
        }

        // シーンロード直後、BGM開始まで待つ場合 true
        private static bool GetWaitFlagByScene(string sceneName)
        {
            return sceneName switch
            {
                "InGameMock" => true,
                "Field" => true,
                _ => false,
            };
        }
    }
}