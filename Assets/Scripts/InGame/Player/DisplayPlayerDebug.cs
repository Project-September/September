using September.InGame.Common.Stats;
using TMPro;
using UnityEngine;

namespace InGame.Player
{
    // デバッグ用にPlayer情報をUIに出す
    public class DisplayPlayerDebug : MonoBehaviour
    {
        [SerializeField] TMP_Text _healthText;
        [SerializeField] TMP_Text _staminaText;
        [SerializeField] TMP_Text _speedText;
        [SerializeField] TMP_Text _isGroundText;

        PlayerMovement _playerMovement;
        PlayerStatus _playerStatus;
        
        private void Start()
        {
            PlayerManager playerManager = GetComponentInParent<PlayerManager>();
            _playerStatus = GetComponentInParent<PlayerStatus>();
            _playerMovement = _playerStatus.GetComponent<PlayerMovement>();

            if (!playerManager.IsLocalPlayer)
            {
                gameObject.SetActive(false);
                return;
            }

            _playerStatus.SubscribeStatOnChanged(StatType.Health, health => _healthText.text = health.ToString());
            _playerStatus.SubscribeStatOnChanged(StatType.Stamina, stamina => _staminaText.text = stamina.ToString("F1"));
        }

        private void FixedUpdate()
        {
            _speedText.text = $"velo:{_playerMovement.MoveVelocity}\non plane mag:{_playerMovement.GetSpeedOnPlane():F2}";
            _isGroundText.text = $"IsGround:{_playerMovement.IsGround}";
        }
    }
}
