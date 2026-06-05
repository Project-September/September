using System;
using Fusion;
using InGame.Interact;
using September;
using September.Common;
using UnityEngine;

namespace InGame.Exhibit
{
    [Serializable]
    public class CannonInteractEffect : CharacterInteractEffectBase
    {
        [SerializeField] private CannonInteractable _cannonInteractable;
        [SerializeField] private InteractableBase _interactable;
        [SerializeField] private float _interactTime;

        public CannonInteractEffect(CannonInteractable cannonInteractable, float time)
        {
            _cannonInteractable = cannonInteractable;
            _interactTime = time;
        }
        public CannonInteractEffect()
        {
        }
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            var playerRef = PlayerRef.FromEncoded(context.Interactor);
            _cannonInteractable.OnInteractStart(playerRef);
            _interactable = target;
        }

        public override void OnInteractFixedNetworkUpdate(PlayerInput playerInput)
        {
            base.OnInteractFixedNetworkUpdate(playerInput);
            //_cannonInteractable.OnInteractFixedNetworkUpdate(playerInput);
        }

        public override void OnInteractEnd()
        {
            base.OnInteractEnd();
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new CannonInteractEffect(_cannonInteractable, _interactTime);
        }
    }
}
