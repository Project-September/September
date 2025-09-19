using Fusion;
using InGame.Interact;
using InGame.Player;
using September.Common;
using UnityEngine;


namespace InGame.Exhibit
{
    public class TyrannoInteractEffect : CharacterInteractEffectBase
    {
        [SerializeField] private float _interactTime;
        [SerializeField] private TyrannoInteractable _tyrannoInteractable;
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

            if (playerInput.Buttons.IsSet(PlayerButtons.Interact) && _interactTimer > 1f)
            {
                GetOff();
                return;
            }


            if (CheckInteractEnd())
            {
                GetOff();
            }

            _tyrannoInteractable.OnInteractFixedUpdate(playerInput, _networkRunner.DeltaTime);
        }

        private bool CheckInteractEnd()
        {
            if (_interactTimer > _interactTime) return true;
            return !_tyrannoInteractable.IsAlive;
        }

        private void GetOn(PlayerRef ownerPlayerRef)
        {
            _ownerPlayerRef = ownerPlayerRef;
            _tyrannoInteractable.GetOn(ownerPlayerRef);
        }

        private void GetOff()
        {
            _tyrannoInteractable.GetOff(_ownerPlayerRef);
            _interactTimer = 0;
            _ownerPlayerRef = PlayerRef.None;
            _interactable.EndInteract();
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new TyrannoInteractEffect()
            {
                _interactTime = _interactTime,
                _tyrannoInteractable = _tyrannoInteractable,
            };
        }
    }
}