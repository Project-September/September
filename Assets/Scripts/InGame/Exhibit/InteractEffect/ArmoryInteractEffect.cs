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
        [SerializeReference, SubclassSelector] private AbilityBase _addAbility;
        [SerializeReference, SubclassSelector] private IAbilityExecuteCondition _addAbilityCondition;
        [SerializeField] private string[] _overrideDisabledAbilities;
        [SerializeField] private float _duration;

        public override CharacterInteractEffectBase Clone()
        {
            return new ArmoryInteractEffect()
            {
                _addAbility = _addAbility,
                _addAbilityCondition = _addAbilityCondition,
                _overrideDisabledAbilities = _overrideDisabledAbilities,
                _duration = _duration
            };
        }

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            //コンポーネント取得
            var player = PlayerRef.FromEncoded(context.Interactor);
            if (!PlayerDatabase.Instance.PlayerObjectDic.TryGet(player, out var playerObject))
            {
                Debug.LogError($"インタラクトしたプレイヤーが見つかりません。{player}");
                return;
            }

            if (!playerObject.TryGetComponent(out PlayerAbilityManager playerAbility)
                || !playerObject.TryGetComponent(out PlayerEquipmentManager equipmentManager))
            {
                return;
            }

            _addAbility.SetPlayerComponent(playerObject.gameObject);

            //上書きするAbilityを無効化
            playerAbility.SetAbilityEnabled(false, _overrideDisabledAbilities);

            //Abilityを追加する
            playerAbility.AddAbility(_addAbility, _addAbilityCondition);

            equipmentManager.RPC_ChangeEquipment(EquipmentType.Armory);

            ReleaseWeaponAsync(playerAbility, equipmentManager, playerObject.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask ReleaseWeaponAsync(PlayerAbilityManager abilityManager, PlayerEquipmentManager equipmentManager, CancellationToken token)
        {
            await UniTask.WaitForSeconds(_duration, cancellationToken: token);

            //無効化していたAbilityを有効化
            abilityManager.SetAbilityEnabled(true, _overrideDisabledAbilities);

            //追加したAbilityを消す
            abilityManager.RemoveAbility(_addAbility.GetType().Name);

            //武器を元に戻す
            equipmentManager.RPC_ChangeEquipment(EquipmentType.NormalAttack);
        }
    }
}