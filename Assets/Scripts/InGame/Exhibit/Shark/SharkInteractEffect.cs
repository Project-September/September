using Fusion;
using InGame.Interact;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace InGame.Exhibit
{
    public class SharkInteractEffect : CharacterInteractEffectBase
    {
        [SerializeField] private float _interactTime;
        [SerializeField] private SharkInteractable _sharkInteractable;
        private PlayerRef _ownerPlayerRef;
        private PlayerManager _ownerPlayerManager;
        private NetworkRunner _networkRunner;
        private float _interactTimer;
        private InteractableBase _interactable;

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            _networkRunner = target.Runner;
            _interactable = target;
            var playerRef = PlayerRef.FromEncoded(context.Interactor);
            GetOn(playerRef);
        }

        public override void OnInteractFixedNetworkUpdate(PlayerInput playerInput)
        {
            _interactTimer += _networkRunner.DeltaTime;

            if (_sharkInteractable.IsEnding)
            {
                GetOff();
                return;
            }
            
            if (CheckInteractEnd())
            {
                GetOff();
                return;
            }

            if (playerInput.Buttons.IsSet(PlayerButtons.Interact) && _interactTimer > 1f)
            {
                GetOff();
                return;
            }
            
            _sharkInteractable.OnInteractFixedUpdate(playerInput, _networkRunner.DeltaTime);
        }

        private bool CheckInteractEnd()
        {
            return _interactTimer > _interactTime || !_sharkInteractable.IsAlive;
        }

        private void GetOn(PlayerRef ownerPlayerRef)
        {
            _ownerPlayerRef = ownerPlayerRef;
            _sharkInteractable.GetOn(ownerPlayerRef);
        }

        private void GetOff()
        {
            _sharkInteractable.GetOff(_ownerPlayerRef);
            _interactTimer = 0;
            _ownerPlayerRef = PlayerRef.None;
            _interactable.EndInteract();
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new SharkInteractEffect()
            {
                _interactTime = _interactTime,
                _sharkInteractable = _sharkInteractable
            };
        }
    }
}
