using System;
using Cysharp.Threading.Tasks;
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