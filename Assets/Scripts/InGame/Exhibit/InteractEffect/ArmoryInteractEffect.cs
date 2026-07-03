using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Interact;
using InGame.Player;
using InGame.Player.Ability;
using September.Common;
using UnityEngine;

namespace InGame.Exhibit
{
    public class ArmoryInteractEffect : CharacterInteractEffectBase
    {
        [SerializeField] private ArmoryInteractRPCInvoker _invoker;
        [SerializeReference, SubclassSelector] private AbilityBase _addAbility;
        [SerializeReference, SubclassSelector] private IAbilityExecuteCondition _addAbilityCondition;
        [SerializeField] private string[] _overrideDisabledAbilities;
        [SerializeField] private float _duration;

        public override CharacterInteractEffectBase Clone()
        {
            return new ArmoryInteractEffect()
            {
                _invoker = _invoker,
                _addAbility = _addAbility,
                _addAbilityCondition = _addAbilityCondition,
                _overrideDisabledAbilities = _overrideDisabledAbilities,
                _duration = _duration
            };
        }

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            //他の人が装備していたら実行しない
            if (_invoker.IsEquipped) return;

            //コンポーネント取得
            var player = PlayerRef.FromEncoded(context.Interactor);
            if (!PlayerDatabase.Instance.PlayerObjectDic.TryGet(player, out var playerObject))
            {
                Debug.LogError($"インタラクトしたプレイヤーが見つかりません。{player}");
                return;
            }

            if (!playerObject.TryGetComponent(out PlayerAbilityManager playerAbility)
                || !playerObject.TryGetComponent(out PlayerManager playerManager))
            {
                return;
            }

            if (_addAbility is AbilityNormalAttack normalAttack)
            {
                normalAttack.SetPlayerComponent(playerObject.gameObject);
            }

            //上書きするAbilityを無効化
            playerAbility.SetAbilityEnabled(false, _overrideDisabledAbilities);

            //Abilityを追加する
            playerAbility.AddAbility(_addAbility, _addAbilityCondition);

            //元から持っている武器を非表示にする
            playerManager.RPC_SetWeaponVisible(false);

            //指定の武器を装備する
            _invoker.RPC_AttachWeapon(playerObject);

            ReleaseWeaponAsync(playerAbility, playerManager, playerObject.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask ReleaseWeaponAsync(PlayerAbilityManager abilityManager, PlayerManager playerManager, CancellationToken token)
        {
            await UniTask.WaitForSeconds(_duration, cancellationToken: token);

            //無効化していたAbilityを有効化
            abilityManager.SetAbilityEnabled(true, _overrideDisabledAbilities);

            //追加したAbilityを消す
            abilityManager.RemoveAbility(_addAbility.GetType().Name);

            //武器を元に戻す
            playerManager.RPC_SetWeaponVisible(true);
            _invoker.RPC_DestroyWeapon();
        }
    }
}
