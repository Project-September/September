using System;
using Fusion;
using InGame.Interact;
using InGame.Player;
using NaughtyAttributes;
using September.Common;
using UnityEngine;

namespace InGame.Exhibit
{
    [Serializable]
    public class PterodactylInteractEffect : CharacterInteractEffectBase
    {
        [SerializeField,Label("インタラクト可能時間")] private float _interactTime;
        [SerializeField] private PterodactylInteractable _pterodactylInteractable;

        private PlayerRef _ownerPlayerRef;
        private PlayerManager _ownerPlayerManager;
        private bool _isInteracting;
        private NetworkRunner _runner;
        private InteractableBase _interactable;
        private float _interactTimer;
        
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            if(_isInteracting)
                return;
            
            _runner = target.Runner;
            _interactable = target;
            CharacterType characterType = context.CharacterType;
            PlayerRef playerRef = PlayerRef.FromEncoded(context.Interactor);
            GetOn(playerRef);
        }

        public override void OnInteractFixedNetworkUpdate(PlayerInput playerInput)
        {
            if (_isInteracting)
            {
                _interactTimer += _runner.DeltaTime;

                if (playerInput.Buttons.IsSet(PlayerButtons.Interact) && _interactTimer > 1f)
                {
                    GetOff();
                    return;
                }
            }

            if (CheckInteractEnd())
            {
                GetOff();
                return;
            }
            _pterodactylInteractable.OnInteractFixedUpdate(playerInput,_runner.DeltaTime);
        }

        private void GetOn(PlayerRef playerRef)
        {
            _ownerPlayerRef = playerRef;
            _pterodactylInteractable.GetOn(playerRef);
            _isInteracting = true;
            
            // Animationの処理
        }

        private void GetOff()
        {
            _pterodactylInteractable.GetOff(_ownerPlayerRef);
            _isInteracting = false;
            // Animationの処理

            _interactTimer = 0;
            _ownerPlayerRef = PlayerRef.None;
            _interactable.EndInteract();
        }

        private bool CheckInteractEnd()
        {
            if (_interactTimer >= _interactTime)
                return true;

            return !_pterodactylInteractable.IsAlive;
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new PterodactylInteractEffect
            {
                _interactTime = _interactTime,
                _pterodactylInteractable = _pterodactylInteractable
            };
        }
    }
}