using System;
using Fusion;
using InGame.Interact;

namespace Ingame.Exhibit
{
    [Serializable]
    public class SateliteCannonInteractEffect : CharacterInteractEffectBase
    {
        public SateliteCannonInteractRPCInvoker SateliteCannonInteractRPCInvoker;
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            PlayerRef playerRef = PlayerRef.FromEncoded(context.Interactor);
            SateliteCannonInteractRPCInvoker.Rpc_RequestInteraction(playerRef);
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new SateliteCannonInteractEffect
            {
                SateliteCannonInteractRPCInvoker = SateliteCannonInteractRPCInvoker,
            };
        }
    }
}