using System;
using InGame.Common;
using September.Common;
using September.InGame.Common;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    [Serializable]
    public class AbilityOkabeUlt : AbilityBase
    {
        [SerializeField] private StatusEffect _buffEffect;
        
        protected override void OnStart()
        {
            Debug.Log("[AbilityUlt] OnStart");
            var manager = StaticServiceLocator.Instance.Get<InGameManager>();
            var player = manager.PlayerDataDic[manager.Runner.LocalPlayer];

            if (player.TryGetComponent<PlayerStatus>(out var playerStatus))
            {
                EffectableStatus.StatusEffectSpec spec = new EffectableStatus.StatusEffectSpec(_buffEffect);
                playerStatus.AddEffect(spec);
            }
        }
    }
}