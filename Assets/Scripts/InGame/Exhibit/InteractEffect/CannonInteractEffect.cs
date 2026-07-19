using System;
using Fusion;
using InGame.Interact;
using September.Common;
using September.InGame.Exhibit;
using UnityEngine;

namespace InGame.Exhibit
{
    [Serializable]
    public class CannonInteractEffect : CharacterInteractEffectBase
    {
        [SerializeField] private CannonInteractable _cannonInteractable;
        public CannonInteractEffect(CannonInteractable cannonInteractable)
        {
            _cannonInteractable = cannonInteractable;
        }
        public CannonInteractEffect()
        {
        }
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            var playerRef = PlayerRef.FromEncoded(context.Interactor);
            _cannonInteractable.InteractStart(playerRef);
        }

        public override void OnInteractFixedNetworkUpdate(PlayerInput playerInput)
        {
            base.OnInteractFixedNetworkUpdate(playerInput);
            _cannonInteractable.InteractFixedNetworkUpdate(playerInput);
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new CannonInteractEffect(_cannonInteractable);
        }
    }
}
