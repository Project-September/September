using System;
using September.Common;
using September.InGame.Common;
using UnityEngine;

namespace InGame.Player.Ability
{
    [Serializable]
    public class AbilityNormalAttackExecuteCondition : IAbilityExecuteCondition
    {
        [SerializeField] private string _targetAbilityName = nameof(AbilityNormalAttack);
        public string TargetAbilityName => _targetAbilityName;
        private PlayerMovement _playerMovement;
        private PlayerManager _playerManager;
        private AimCameraController _aimCameraController;

        public bool IsConditionMatch(in TriggerEventContext context)
        {
            if (!context.Owner) return false;
            if (!_playerMovement) _playerMovement = context.Owner.GetComponent<PlayerMovement>();
            if (!_playerManager) _playerManager = context.Owner.GetComponent<PlayerManager>();
            if (!_aimCameraController) _aimCameraController = context.Owner.GetComponent<AimCameraController>();

            // 展示物から追加される通常攻撃も、ハタノの構え中には開始しない。
            if (_aimCameraController && (_aimCameraController.IsAim ||
                context.CurrentButtons.IsSet(PlayerButtons.Ability2))) return false;


            //Available状態でAttackボタンが押されたら条件を満たす
            return _playerMovement.IsGround && !_playerManager.IsStun && !_playerMovement.IsEvading && !IsGameEnded() &&
                   _playerManager.CurrentPlayerControlState == PlayerManager.PlayerControlState.Normal &&
                   context.AbilityRef.Phase == AbilityBase.AbilityPhase.Available &&
                   context.AbilityRef.CanStartAbilityOverride() &&
                   context.CurrentButtons.GetPressed(context.PreviousButtons).IsSet(PlayerButtons.Attack);
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
    }
}
