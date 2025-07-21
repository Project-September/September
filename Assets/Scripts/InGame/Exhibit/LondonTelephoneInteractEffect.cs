using System;
using Fusion;
using InGame.Interact;

namespace InGame.Exhibit
{
    [Serializable]
    public class LondonTelephoneInteractEffect : CharacterInteractEffectBase
    {
        public LondonTelephoneInteractRPCInvoker LondonTelephoneInteractRPC;
        
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            LondonTelephoneInteractRPC.RpcRequestInteraction(PlayerRef.FromEncoded(context.Interactor));
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new LondonTelephoneInteractEffect
            {
                LondonTelephoneInteractRPC = LondonTelephoneInteractRPC,
            };
        }
    }
}