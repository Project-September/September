using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
using InGame.Player;
using September.Common;
using September.InGame.UI;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace September.InGame.Tutorial
{
    public class TutorialSceneSetup : MonoBehaviour
    {
        [SerializeField] private TutorialManager _tutorialManager;
        [SerializeField] private NetworkPrefabRef _playerPrefab;
        [SerializeField] private NetworkPrefabRef _playerDatabasePrefab;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private UnityEngine.UI.Image _fadeImage;
        [SerializeField] private ControlsUIGenerator _controlsUIGenerator;
        [SerializeField] private ControlDescriptionType _controlDescriptionType;
        [SerializeField] private UIController _uiController;

        private NetworkRunner _runner;
        private bool _hasSpawned = false;

        private void Awake()
        {
            _runner = FindObjectOfType<NetworkRunner>();
            GameInput.I.IsInputBlockedByUI = true;
            Initialized();
        }

        private void Initialized()
        {
            Debug.Log($"[Tutorial] _runner: {_runner}");
            Debug.Log($"[Tutorial] _runner.IsRunning: {_runner?.IsRunning}");
            if (_runner != null && _runner.IsRunning)
            {
                _runner.ProvideInput = true;
                UISet();
                SpawnPlayer(_runner, _runner.LocalPlayer).Forget();
                return;
            }

            // 起動済みでない場合は新規作成
            if (_runner == null)
            {
                var go = new GameObject("NetworkRunner");
                _runner = go.AddComponent<NetworkRunner>();
            }

            // Runnerの設定
            _runner.ProvideInput = true;
            UISet();
            StartNetworkGame().Forget();
        }

        private void UISet()
        {
            _uiController.SetUpStartUI();
            ChangeExhibitDescriptionUI(_controlDescriptionType);
        }

        /// <summary>
        /// ネットワークに接続
        /// </summary>
        private async UniTaskVoid StartNetworkGame()
        {
            var result = await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Single,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex)
            });

            if (result.Ok)
                Debug.Log("[Tutorial] Network Started Successfully");
            else
                Debug.LogError($"[Tutorial] Failed to start network: {result.ErrorMessage}");

            SpawnPlayer(_runner, _runner.LocalPlayer).Forget();
        }

        /// <summary>
        /// コントロール説明UIを設定する
        /// </summary>
        private void ChangeExhibitDescriptionUI(ControlDescriptionType type)
        {
            _controlsUIGenerator.GenerateDescription(type);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (_hasSpawned || player != runner.LocalPlayer) return;
            _hasSpawned = true;
            SpawnPlayer(runner, player).Forget();
        }

        /// <summary>
        /// プレイヤーをスポーンする。PlayerDatabase のスポーンと同期を待ってからプレイヤーをスポーンする
        /// </summary>
        private async UniTaskVoid SpawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            // PlayerDatabase をスポーン
            var db = await runner.SpawnAsync(_playerDatabasePrefab);
            await UniTask.WaitUntil(() => PlayerDatabase.Instance != null);
            PlayerDatabase.Instance.AddPlayerData(player);

            // PlayerDatabase の同期を待つ
            await UniTask.WaitUntil(() =>
                PlayerDatabase.Instance.PlayerDataDic.ContainsKey(player));

            Debug.Log("[Tutorial] PlayerDatabase ready");

            // プレイヤーをスポーン
            var playerNetworkObject = await runner.SpawnAsync(_playerPrefab, _spawnPoint.position, Quaternion.identity, player);

            Debug.Log("[Tutorial] Player spawned");
            FadeOut(playerNetworkObject).Forget();
        }

        private async UniTask FadeOut(NetworkObject playerNetworkObject)
        {
            if (_fadeImage)
            {
                _fadeImage.color = new Color(0f, 0f, 0f, 1f);

                await _fadeImage.DOFade(0f, 1f).SetEase(Ease.InOutQuad);
            }
            else
            {
                Debug.LogWarning("[Tutorial] Fade image is not assigned.");
            }

            // チュートリアルを開始
            var playerInputManager = playerNetworkObject.GetComponent<PlayerInputManager>();
            if (playerInputManager != null)
            {
                RPC_ToggleInputs(true, true, true);
                _tutorialManager.OnTutorialStart(playerNetworkObject, playerInputManager);
            }
            else
            {
                Debug.LogError("[Tutorial] PlayerInputManager not found on the player object.");
            }
        }

        /// <summary>
        /// 全クライアントの入力状態を切り替えるRPC
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ToggleInputs(bool moveInputEnabled, bool actionInputEnabled, bool lookInputEnabled)
        {
            GameInput.I.ToggleMoveInput(moveInputEnabled);
            GameInput.I.ToggleActionInput(actionInputEnabled);
            GameInput.I.ToggleLookInput(lookInputEnabled);
        }
    }
}