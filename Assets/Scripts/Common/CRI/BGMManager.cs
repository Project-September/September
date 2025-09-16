using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace CRISound
{
    public class BGMManager
    {
        private static bool _isInitialized;
        private static string _currentCueName;
        private static bool _isWaiting = false;

        public static void Initialize()
        {
            if(_isInitialized)
                return;
            
            _isInitialized = true;

            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        /// <summary> 待機フラグを false にする </summary>
        public static void ReleseFlag()
        {
            _isWaiting = false;
        }

        private static async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!CuePlayAtomExPlayer.Instance.IsReady)
            {
                await UniTask.WaitUntil(() => CuePlayAtomExPlayer.Instance.IsReady);
            }
            string newCueName = GetCueNameByScene(scene.name);
            _isWaiting = GetWaitFlagByScene(scene.name);
            
            // 例：タイトル
            //if (!string.IsNullOrEmpty(_currentCueName) && newCueName != _currentCueName)
            //{
            //    CRIAudio.StopBGM("ALLCue", _currentCueName);
            //}

            // 待機フラグが立っているときは現在のBGMを止め、解除されるまで流さない
            if (_isWaiting)
            {
                CRIAudio.StopBGM("ALLCue", _currentCueName);
                await UniTask.WaitUntil(() => !_isWaiting);
            }
            
            if(!string.IsNullOrEmpty(newCueName))
            {
                CRIAudio.PlayBGM("ALLCue", newCueName);
                _currentCueName = newCueName;
            }
        }
        
        private static string GetCueNameByScene(string sceneName)
        {
            return sceneName switch
            {
                "Title" => "BGM_Title_01",
                "Lobby" => "BGM_Select_01",
                "InGameMock" => "BGM_Ingame_01",
                "Field" => "BGM_Ingame_01",
                "Result" => "BGM_Result_01",
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