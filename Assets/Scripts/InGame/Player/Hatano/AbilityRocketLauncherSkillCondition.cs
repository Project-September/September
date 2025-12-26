using System;
using InGame.Player.Hatano;
using September.Common;

namespace InGame.Player.Ability
{
    [Serializable]
    public class AbilityRocketLauncherSkillCondition : IAbilityExecuteCondition
    {
        public string TargetAbilityName => nameof(AbilityRocketLauncher);

        // キャッシュ用
        private PlayerMovement _playerMovement;
        private PlayerManager _playerManager;

        public bool IsConditionMatch(in TriggerEventContext context)
        {
            if (!context.Owner) return false;
            if (!_playerMovement) _playerMovement = context.Owner.GetComponent<PlayerMovement>();
            if (!_playerManager) _playerManager = context.Owner.GetComponent<PlayerManager>();
            
            if(AbilityStatusManagement.instance.AbilityStatus == HatanoAbilityStatus.None) return false;

            // 条件を定義
            return context.AbilityRef.Phase == AbilityBase.AbilityPhase.Available
                   && context.AbilityRef.CanStartAbilityOverride()
                   && context.CurrentButtons.GetPressed(context.PreviousButtons)
                       .IsSet(PlayerButtons.Ability2); // 使用するボタンを指定
        }
    }
}
