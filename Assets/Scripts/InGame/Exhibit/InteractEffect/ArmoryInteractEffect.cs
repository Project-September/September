using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Interact;
using InGame.Player;
using InGame.Player.Ability;
using September.Common;
using UnityEditor;
using UnityEngine;

namespace InGame.Exhibit
{
    public class ArmoryInteractEffect : CharacterInteractEffectBase
    {
        [SerializeReference, SubclassSelector] private AbilityBase _addAbility;
        [SerializeReference, SubclassSelector] private IAbilityExecuteCondition _addAbilityCondition;
        [SerializeField] private MonoScript[] _overrideDisabledAbilitys;
        [SerializeField] private float _duration;

        private PlayerAbilityManager _playerAbility;
        public override CharacterInteractEffectBase Clone()
        {
            return new ArmoryInteractEffect()
            {
                _addAbility = _addAbility,
                _addAbilityCondition = _addAbilityCondition,
                _overrideDisabledAbilitys = _overrideDisabledAbilitys,
                _duration = _duration
            };
        }

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            var player = PlayerRef.FromEncoded(context.Interactor);
            if (!PlayerDatabase.Instance.PlayerObjectDic.TryGet(player, out var playerObject))
            {
                Debug.LogError($"インタラクトしたプレイヤーが見つかりません。{player}");
                return;
            }

            if (!playerObject.TryGetComponent(out _playerAbility))
            {
                return;
            }

            if(_addAbility is AbilityNormalAttack normalAttack)
            {
                normalAttack.SetPlayerComponent(playerObject.gameObject);
            }

            _playerAbility.SetAbilityEnabled(false, _overrideDisabledAbilitys.Select(x => x.name).ToArray());
            _playerAbility.AddAbility(_addAbility,_addAbilityCondition);

            Debug.Log(player + "start");

            ReleaseWeapon();
        }
        private async void  ReleaseWeapon()
        {
            await UniTask.WaitForSeconds(_duration);

            _playerAbility.SetAbilityEnabled(true, _overrideDisabledAbilitys.Select(x => x.name).ToArray());
            _playerAbility.RemoveAbility(_addAbility.GetType().Name);

            Debug.Log("end");
        }
    }
}
