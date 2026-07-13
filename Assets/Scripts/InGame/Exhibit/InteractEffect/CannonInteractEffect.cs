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
        [SerializeField] private CannonInteractable _cannonAimPosRenderer;
        public CannonInteractEffect(CannonInteractable cannonAimPosRenderer)
        {
            _cannonAimPosRenderer = cannonAimPosRenderer;
        }
        public CannonInteractEffect()
        {
        }
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            var playerRef = PlayerRef.FromEncoded(context.Interactor);
            _cannonAimPosRenderer.OnInteractStart(playerRef);
        }

        public override void OnInteractFixedNetworkUpdate(PlayerInput playerInput)
        {
            base.OnInteractFixedNetworkUpdate(playerInput);
            _cannonAimPosRenderer.OnInteractFixedNetworkUpdate(playerInput);
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new CannonInteractEffect(_cannonAimPosRenderer);
        }
    }
}
