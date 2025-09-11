using Fusion;
using InGame.Interact;
using UnityEngine;


namespace InGame.Exhibit.InteractEffect
{
    public class StradivariusInteractEffect : CharacterInteractEffectBase
    {
        [SerializeField] private HealInstrumentController _healInstrumentController;
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            var playerRef = PlayerRef.FromEncoded(context.Interactor);
            _healInstrumentController.HealPlayer(playerRef);
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new StradivariusInteractEffect()
            {
                _healInstrumentController = _healInstrumentController,
            };
        }
    }
}