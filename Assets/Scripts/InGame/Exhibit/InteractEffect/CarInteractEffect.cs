using Fusion;
using InGame.Interact;

namespace InGame.Exhibit.InteractEffect
{
    public class CarInteractEffect : CharacterInteractEffectBase
    {
        public CarInteractable  CarInteractable;
        
        // Interactが完了したら車を動かす
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            var playerRef = PlayerRef.FromEncoded(context.Interactor);
            CarInteractable.RPC_OnInteractStart(playerRef);
            CarInteractable.EffectSpawn();
        }
        
        public override CharacterInteractEffectBase Clone()
        {
            return new CarInteractEffect
            {
                CarInteractable = CarInteractable
            };
        }
    }
}