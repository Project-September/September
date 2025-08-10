using Fusion;
using InGame.Interact;
using UnityEngine;


namespace InGame.Exhibit.InteractEffect
{
    public class StradivariusInteractEffect : CharacterInteractEffectBase
    {
        [SerializeField] private StradivariusController _stradivariusController;
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            var playerRef = PlayerRef.FromEncoded(context.Interactor);
            _stradivariusController.HealPlayer(playerRef);
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new StradivariusInteractEffect()
            {
                _stradivariusController = _stradivariusController,
            };
        }
    }
}