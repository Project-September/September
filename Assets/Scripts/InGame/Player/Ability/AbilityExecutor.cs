using System;
using System.Collections.Generic;
using System.Linq;
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
                var targetAbility = _abilityReferences.Find(x => x.AbilityName == context.AbilityName);
                if (targetAbility.EachCharacterLastActiveTime.Dictionary.TryGetValue(characterType,
                        out var lastActiveTime))
                {
                    var cooldownTime =
                        targetAbility.CooldownTimeDictionary.Dictionary.TryGetValue(CharacterType.All,
                            out var allCooldown)
                            ? allCooldown
                            : targetAbility.CooldownTimeDictionary.Dictionary.GetValueOrDefault(characterType, 0f);
                    if (Runner && Runner.SimulationTime - lastActiveTime < cooldownTime)
                    {
                        Debug.Log($"キャラクター {characterType} のアビリティはクールダウン中です。 " +
                                  $"残り時間: {cooldownTime - (Runner.SimulationTime - lastActiveTime)}秒");
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
            ActiveAbilities[context.SourcePlayer] = new List<AbilityBase> { abilityInstance };

            if (!PlayerDatabase.Instance.PlayerDataDic.TryGet(PlayerRef.FromEncoded(context.SourcePlayer),
                    out var playerData)) return;
            //もし即時クールダウンが有効なら、すぐにクールダウン時間を更新。そうでないなら仮で長めのクールダウン時間をとる
            if (abilityRef.StartCooldownImmediately)
            {
                abilityRef.EachCharacterLastActiveTime.Dictionary[playerData.CharacterType] = Runner.SimulationTime;
            }
            else
            {
                // 仮の長いクールダウン時間を設定
                abilityRef.EachCharacterLastActiveTime.Dictionary[playerData.CharacterType] = float.MaxValue;
            }
        }

        private void Update()
        {
            if (Runner && !Runner.IsServer) return;
            if (!_isInitialized) Initialize();

            foreach (var eachPlayerIdActiveAbilities in ActiveAbilities)
            {
                // 各プレイヤーのアクティブなアビリティを更新
                foreach (var ability in eachPlayerIdActiveAbilities.Value)
                {
                    ability.Tick(Runner.DeltaTime);
                }
                
                var removedAbilities = eachPlayerIdActiveAbilities.Value
                    .Where(runtime => runtime.Phase == AbilityBase.AbilityPhase.Ended)
                    .ToList();

                // 終了したアビリティを削除
                eachPlayerIdActiveAbilities.Value.RemoveAll(runtime => runtime.Phase == AbilityBase.AbilityPhase.Ended);

                if (removedAbilities.Count > 0)
                {
                    Debug.Log($"プレイヤー {eachPlayerIdActiveAbilities.Key} のアビリティが終了しました");
                }

                // 即時クールダウンが有効ではないアビリティの終了時刻を更新
                foreach (var ability in removedAbilities)
                {
                    if (ability.StartCooldownImmediately) continue;
                    var originalAbilityRef = _abilityReferences
                        .Find(x => x.AbilityName == ability.AbilityName);
                    if (PlayerDatabase.Instance.PlayerDataDic.TryGet(
                            PlayerRef.FromEncoded(eachPlayerIdActiveAbilities.Key), out var playerData))
                    {
                        originalAbilityRef.EachCharacterLastActiveTime.Dictionary[playerData.CharacterType] =
                            Runner.SimulationTime;
                    }
                }
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
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