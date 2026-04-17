using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace September.Common
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance;
        [SerializeField] NetworkRunner _runnerPrefab;
        [SerializeField] PlayerDatabase _playerDatabasePrefab;
        [SerializeField] LoadingIcon _loadingIcon;
        [SerializeField] RoomErrorMessage _roomErrorMessage;
        [SerializeField] TextMeshProUGUI _createErrorText;
        [SerializeField] TextMeshProUGUI _joinErrorText;
        [SerializeField, Scene] string _titleSceneName;
        [SerializeField, Scene] string _lobbySceneName;
        [SerializeField, Scene] string _gameSceneName;
        [SerializeField, Scene] string _resultSceneName;
        NetworkRunner _networkRunner;
        UniTask _currentTask;
        private void Start()
        {
            if (Instance == null)
            {
                _networkRunner = Instantiate(_runnerPrefab);
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            if(_createErrorText != null)
            {
                _createErrorText.gameObject.SetActive(false);
            }
            if (_joinErrorText != null)
            {
                _joinErrorText.gameObject.SetActive(false);
            }
        }
        public void CreateLobby(string gameName, int playerCount)
        {
            if (!_currentTask.Status.IsCompleted()) return;
            _loadingIcon.StartAnimation();
            _currentTask = CreateLobbyAsync(gameName, playerCount).Preserve();
            _currentTask.GetAwaiter().OnCompleted(() => _loadingIcon.StopAnimation());
        }
        async UniTask CreateLobbyAsync(string gameName, int playerCount)
        {
            var result = await _networkRunner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Host,
                SessionName = gameName,
                PlayerCount = playerCount
            });

            ChangeErrorMessage(result, _createErrorText);

            if (!result.Ok)
            {
                await InitializeRunner();
                return;
            }
            await _networkRunner.SpawnAsync(_playerDatabasePrefab);
            await _networkRunner.LoadScene(_lobbySceneName);
        }
        public void JoinLobby(string gameName)
        {
            if (!_currentTask.Status.IsCompleted()) return;
            _loadingIcon.StartAnimation();
            _currentTask = JoinLobbyAsync(gameName).Preserve();
            _currentTask.GetAwaiter().OnCompleted(() => _loadingIcon.StopAnimation());
        }
        async UniTask JoinLobbyAsync(string gameName)
        {
            var result = await _networkRunner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = gameName,
            });

            ChangeErrorMessage(result, _joinErrorText);

            if (!result.Ok)
            {

                await InitializeRunner();
            }
        }
        public async UniTask InitializeRunner()
        {
            await _networkRunner.Shutdown();
            _networkRunner = Instantiate(_runnerPrefab);
        }
        public async UniTaskVoid QuitLobby()
        {
            await _networkRunner.Shutdown();
            await SceneManager.LoadSceneAsync(_titleSceneName);
            _networkRunner = Instantiate(_runnerPrefab);
        }

        public async UniTaskVoid StartGame()
        {
            if (!_networkRunner.IsServer) return;
            _networkRunner.SessionInfo.IsOpen = false;

            await _networkRunner.LoadScene(_gameSceneName);
        }

        public async UniTask Fade(Image fadeImage)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0f, 0f, 0f, 0f);

            await fadeImage.DOFade(1f, 0.5f).SetEase(Ease.InOutQuad);
        }


        public async UniTask QuitInGame()
        {
            if (!_networkRunner.IsServer) return;
            _networkRunner.SessionInfo.IsOpen = false;

            if (!SceneManager.GetSceneByName("Field").isLoaded)
            {
                return;
            }

            await _networkRunner.UnloadScene("Field");
            await _networkRunner.LoadScene(_resultSceneName);
        }

        /// <summary>
        /// StartGameの結果に応じてエラーメッセージを変更する
        /// </summary>
        /// <param name="result">StartGameの結果</param>
        /// <param name="text">エラーメッセージを表示するText</param>
        private void ChangeErrorMessage(StartGameResult result,TextMeshProUGUI text)
        {
            if (result == null ||  text == null || _roomErrorMessage == null) return;

            if (result.Ok)
            {
                text.gameObject.SetActive(false);
                return;
            }

            text.gameObject.SetActive(true);
            text.text = _roomErrorMessage.GetMessage(result.ShutdownReason);
        }
    }
}