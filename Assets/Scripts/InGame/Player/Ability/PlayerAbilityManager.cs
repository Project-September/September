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
        private readonly Dictionary<string, AbilityBase> _abilityByName = new();

        private void Start()
        {
            _networkObject = GetComponent<NetworkObject>();
            foreach (var a in _abilities)
                _abilityByName[a.GetType().Name] = a;
        }

        private void Update()
        {
            foreach (var condition in _conditions)
            {
                if (!_abilityByName.TryGetValue(condition.TargetAbilityName, out var targetAbility))
                {
                    Debug.LogError($"[PlayerAbilityManager] '{condition.TargetAbilityName}' が見つかりません。");
                    continue;
                }
                
                var ctx = new TriggerEventContext(gameObject, targetAbility, _currentButtons, _previousButtons);
                if (condition.IsConditionMatch(in ctx))
                {
                    targetAbility.Start(new AbilityParameter { Owner = _networkObject });
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