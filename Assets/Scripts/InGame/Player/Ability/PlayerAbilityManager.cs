using System;
using System.Collections.Generic;
using Fusion;
using September.Common;
using UnityEngine;

namespace InGame.Player.Ability
{
    /// <summary>
    /// 入力条件とアビリティの実行を結びつける
    /// </summary>
    public class PlayerAbilityManager : NetworkBehaviour
    {
        [SerializeReference, SubclassSelector] private List<AbilityBase> _abilities = new();
        [SerializeReference, SubclassSelector] private List<IAbilityExecuteCondition> _conditions = new();
        private NetworkButtons _previousButtons;
        private NetworkButtons _currentButtons;
        private NetworkObject _networkObject;

        private void Start()
        {
            _networkObject = GetComponent<NetworkObject>();
        }

        private void Update()
        {
            if (!HasStateAuthority) return;
            foreach (var condition in _conditions)
            {
                var targetAbility = _abilities.Find(a => a.GetType().Name == condition.TargetAbilityName);
                
                if (targetAbility == null)
                {
                    Debug.LogError($"[PlayerAbilityManager] アビリティ '{condition.TargetAbilityName}' が見つかりません。");
                    continue;
                }
                // 条件を満たす場合、アビリティを実行
                if (condition.IsConditionMatch(new TriggerEventContext(targetAbility, _currentButtons, _previousButtons)))
                {
                    // アビリティの実行
                    targetAbility.Start(new AbilityParameter() 
                    {
                        Owner = _networkObject,
                    });
                }
            }
            
            foreach (var ability in _abilities)
            {
                ability.Tick(Time.deltaTime);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput<PlayerInput>(out var input)) return;
            
            // 入力を更新
            _previousButtons = _currentButtons;
            _currentButtons = input.Buttons;
        }
    }
}