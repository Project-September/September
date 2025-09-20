using System;
using InGame.Interact;
using UnityEngine;

namespace InGame.Exhibit
{
    [Serializable]
    public class MuramasaInteractEffect : CharacterInteractEffectBase
    {
        [SerializeField] private MuramasaInteractInvoker _invoker;
        public MuramasaInteractEffect(MuramasaInteractInvoker invoker)
        {
            _invoker = invoker;
        }

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            _invoker.Rpc_StartAttack(context.Interactor);
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new MuramasaInteractEffect(_invoker);
        }
    }
}