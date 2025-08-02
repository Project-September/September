using System.Collections.Generic;
using Fusion;
using InGame.Common;
using September.Common;
using UnityEngine;
using September.InGame.Common;

namespace InGame.Player.Ability
{
    public class AbilityExecutor : NetworkBehaviour, IAbilityExecutor, IRegisterableService
    {
        [SerializeReference, SubclassSelector] private List<AbilityBase> _abilityReferences = new();
        [SerializeField] private SerializableDictionary<CharacterType, float> _cooldownTimeDictionary = new();
        [SerializeField] private SerializableDictionary<CharacterType, float> _eachCharacterLastActiveTime = new();

        private ISpawner _spawner;
        private bool _isInitialized = false;
        public Dictionary<int, List<AbilityBase>> ActiveAbilities { get; } = new();

        private void Awake()
        {
            Register(StaticServiceLocator.Instance);
        }

        private void Initialize()
        {
            _spawner = StaticServiceLocator.Instance.Get<ISpawner>();
            _isInitialized = true;
        }

        public void RequestAbilityExecution(AbilityContext context)
        {
            if (!_isInitialized) Initialize();
            if (PlayerDatabase.Instance.PlayerDataDic.TryGet(PlayerRef.FromEncoded(context.SourcePlayer),
                    out var playerData))
            {
                // クールダウンチェック
                var characterType = playerData.CharacterType;
                if (_eachCharacterLastActiveTime.Dictionary.TryGetValue(characterType, out var lastActiveTime))
                {
                    var cooldownTime = _cooldownTimeDictionary.Dictionary.TryGetValue(CharacterType.All, out var allCooldown)
                        ? allCooldown
                        : _cooldownTimeDictionary.Dictionary.GetValueOrDefault(characterType, 0f);
                    if (Runner && Runner.SimulationTime - lastActiveTime < cooldownTime)
                    {
                        Debug.Log($"キャラクター {characterType} のアビリティはクールダウン中です。");
                        return;
                    }
                }
            }

            // アビリティの開始通知を全プレイヤーに送信。現状はアニメーションを同期するのに使っている
            RPC_NotifyAbilityStart(context);
            if (Runner.IsServer)
            {
                ExecuteAbilityUnified(context);
            }
            else
            {
                RPC_RequestAbility(context);
            }
        }
        
        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_NotifyAbilityStart(AbilityContext context)
        {
            var abilityRef = _abilityReferences.Find(x => x.AbilityName == context.AbilityName);
            abilityRef?.OnStartNotifyAll(context);
        }

        private void ExecuteAbilityUnified(AbilityContext context)
        {
            var abilityRef = _abilityReferences.Find(x => x.AbilityName == context.AbilityName);
            if (abilityRef == null)
            {
                Debug.LogError($"Ability {context.AbilityName} が見つかりません。");
                return;
            }

            var abilityInstance = abilityRef.Clone(abilityRef);
            abilityInstance.InitAbility(context, _spawner);

            if (!ActiveAbilities.ContainsKey(context.SourcePlayer))
                ActiveAbilities[context.SourcePlayer] = new List<AbilityBase>();

            ActiveAbilities[context.SourcePlayer].Add(abilityInstance);

            if (PlayerDatabase.Instance.PlayerDataDic.TryGet(PlayerRef.FromEncoded(context.SourcePlayer),
                    out var playerData))
                _eachCharacterLastActiveTime.Dictionary[playerData.CharacterType] = Runner.SimulationTime;
        }
        
        public override void FixedUpdateNetwork()
        {
            if (!_isInitialized) Initialize();

            foreach (var kvp in ActiveAbilities)
            {
                kvp.Value.RemoveAll(runtime => runtime.Phase == AbilityBase.AbilityPhase.Ended);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_RequestAbility(AbilityContext context)
        {
            ExecuteAbilityUnified(context);
        }

        public void Register(ServiceLocator locator)
        {
            locator.Register<IAbilityExecutor>(this);
        }
    }
}