using September.InGame.Common.Stats;
using UnityEngine;
using September.InGame.UI;

namespace InGame.Player
{
    public class PlayerDataManager : MonoBehaviour
    {
        private PlayerManager _playerManager;
        private PlayerStatus _playerStatus;

        private void Start()
        {
            Initialize();
            RegisterPlayer(_playerStatus);
        }

        private void Initialize()
        {
            _playerManager = GetComponent<PlayerManager>();
            _playerStatus = GetComponent<PlayerStatus>();
        }

        // GameLauncherでDataを登録する必要がある
        private void RegisterPlayer(PlayerStatus status)
        {
            if (!_playerManager.IsLocalPlayer)
                return;

            if (UIController.I)
            {
                // Health監視
                status.SubscribeStatOnChanged(StatType.Health, x => UIController.I.ChangeSliderValue((int)x));
                // Stamina 監視
                status.SubscribeStatOnChanged(StatType.Stamina, x => UIController.I.ChangeStaminaValue(x));
            }
        }
    }
}