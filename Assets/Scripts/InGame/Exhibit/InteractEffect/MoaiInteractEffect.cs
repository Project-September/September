using System;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Interact;

namespace InGame.Exhibit
{
    [Serializable]
    public class MoaiInteractEffect : CharacterInteractEffectBase
    {
        public MoaiInteractInvoker MoaiInteractInvoker;
        
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            MoaiInteractInvoker.StartSpeakAnimation().Forget();
            var playerRef = PlayerRef.FromEncoded(context.Interactor);
            MoaiInteractInvoker.RPC_ShowStatusUpUI(playerRef);
            
        }
        public override CharacterInteractEffectBase Clone()
        {
            return new MoaiInteractEffect
            {
                MoaiInteractInvoker = MoaiInteractInvoker,
            };
        }
    }
}