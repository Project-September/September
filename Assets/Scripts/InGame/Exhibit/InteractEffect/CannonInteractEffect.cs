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
        [SerializeField] private float _interactCoolTime;

        public CannonInteractEffect(CannonInteractable cannonInteractable, float coolTime)
        {
            _cannonInteractable = cannonInteractable;
            _interactCoolTime = coolTime;
        }
        public CannonInteractEffect()
        {
        }
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            var playerRef = PlayerRef.FromEncoded(context.Interactor);
            _cannonInteractable.OnInteractStart(playerRef);
            _cannonInteractable.InteractCoolTime = _interactCoolTime;
        }

        public override void OnInteractFixedNetworkUpdate(PlayerInput playerInput)
        {
            base.OnInteractFixedNetworkUpdate(playerInput);
            _cannonInteractable.OnInteractFixedNetworkUpdate(playerInput);
        }

        public override void OnInteractEnd()
        {
            base.OnInteractEnd();
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new CannonInteractEffect(_cannonInteractable, _interactCoolTime);
        }
    }
}
