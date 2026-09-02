using InGame.Exhibit;
using September.Common;
using September.InGame.Common;
using UnityEngine;

namespace InGame.Player.Ability.Condition
{
    public class AbilityStampingAttackCondition : IAbilityExecuteCondition
    {
        [SerializeField] private string _targetAbilityName = nameof(AbilityStampingAttack);
        [SerializeField] private PlayerButtons _button = PlayerButtons.Jump;
        public string TargetAbilityName => _targetAbilityName;
        private PlayerMovement _playerMovement;
        private PlayerManager _playerManager;

        public bool IsConditionMatch(in TriggerEventContext context)
        {
            //return DebugIsConditionMatch(in context);

            if (!context.Owner) return false;
            if (!_playerMovement) _playerMovement = context.Owner.GetComponent<PlayerMovement>();
            if (!_playerManager) _playerManager = context.Owner.GetComponent<PlayerManager>();


            //Available状態でAttackボタンが押されたら条件を満たす
            return !_playerMovement.IsGround && !_playerManager.IsStun && !_playerMovement.IsEvading && !IsGameEnded() &&
                   _playerManager.CurrentPlayerControlState == PlayerManager.PlayerControlState.Normal &&
                   context.AbilityRef.Phase == AbilityBase.AbilityPhase.Available &&
                   context.AbilityRef.CanStartAbilityOverride() &&
                   context.CurrentButtons.GetPressed(context.PreviousButtons).IsSet(_button);
        }

        private bool IsGameEnded()
        {
            try
            {
                var inGameManager = StaticServiceLocator.Instance.Get<InGameManager>();
                if (inGameManager == null) return false;

                // EndingStateかPlayingState以外の場合は攻撃を無効にする
                return inGameManager.CurrentStateName != "PlayingState";
            }
            catch (System.Exception)
            {
                // エラーが発生した場合は安全側に攻撃を無効にする
                return true;
            }
        }
        private bool DebugIsConditionMatch(in TriggerEventContext context)
        {
            if (!context.Owner)
            {
                Debug.Log("[IsConditionMatch] NG: context.Owner が null");
                return false;
            }

            if (!_playerMovement)
                _playerMovement = context.Owner.GetComponent<PlayerMovement>();

            if (!_playerManager)
                _playerManager = context.Owner.GetComponent<PlayerManager>();

            if (_playerMovement == null)
            {
                Debug.Log("[IsConditionMatch] NG: PlayerMovement が null");
                return false;
            }

            if (_playerManager == null)
            {
                Debug.Log("[IsConditionMatch] NG: PlayerManager が null");
                return false;
            }

            // Available状態でAttackボタンが押されたら条件を満たす

            if (_playerMovement.IsGround)
            {
                Debug.Log("[IsConditionMatch] NG: IsGround == true");
                return false;
            }

            if (_playerManager.IsStun)
            {
                Debug.Log("[IsConditionMatch] NG: IsStun == true");
                return false;
            }

            if (_playerMovement.IsEvading)
            {
                Debug.Log("[IsConditionMatch] NG: IsEvading == true");
                return false;
            }

            if (IsGameEnded())
            {
                Debug.Log("[IsConditionMatch] NG: GameEnded == true");
                return false;
            }

            if (_playerManager.CurrentPlayerControlState != PlayerManager.PlayerControlState.Normal)
            {
                Debug.Log(
                    $"[IsConditionMatch] NG: PlayerControlState = {_playerManager.CurrentPlayerControlState}");
                return false;
            }

            if (context.AbilityRef.Phase != AbilityBase.AbilityPhase.Available)
            {
                Debug.Log(
                    $"[IsConditionMatch] NG: AbilityPhase = {context.AbilityRef.Phase}");
                return false;
            }

            if (!context.AbilityRef.CanStartAbilityOverride())
            {
                Debug.Log("[IsConditionMatch] NG: CanStartAbilityOverride() == false");
                return false;
            }

            var pressedButtons = context.CurrentButtons.GetPressed(context.PreviousButtons);

            if (!pressedButtons.IsSet(_button))
            {
                Debug.Log(
                    $"[IsConditionMatch] NG: Button = {_button}, PressedButtons = {pressedButtons}");
                return false;
            }

            Debug.Log("[IsConditionMatch] OK: 全条件を満たしています");
            return true;
        }
    }
}
