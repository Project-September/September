using InGame.Interact;

namespace InGame.Exhibit.InteractEffect
{
    public class CarInteractEffect : CharacterInteractEffectBase
    {
        public CarInteractable  CarInteractable;
        
        // Interactが完了したら車を動かす
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            CarInteractable.RPC_OnInteractStart();
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