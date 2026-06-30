using InGame.Interact;
using InGame.Player.Ability;
using UnityEngine;

namespace InGame.Exhibit
{
    public class ArmoryInteractEffect : CharacterInteractEffectBase
    {
        [SerializeReference, SubclassSelector] private AbilityBase _addAbility;
        [SerializeReference, SubclassSelector] private IAbilityExecuteCondition _addAbilityCondition;

        public override CharacterInteractEffectBase Clone()
        {
            return new ArmoryInteractEffect()
            {
                _addAbility = _addAbility,
                _addAbilityCondition = _addAbilityCondition
            };
        }

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            Debug.Log("ArmoryInteract");
        }
    }
}
