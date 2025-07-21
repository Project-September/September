using System.Collections.Generic;
using Fusion;
using InGame.Health;
using InGame.Interact;
using InGame.Player;
using September.Common;
using September.InGame.Common;
using UnityEngine;


namespace InGame.Exhibit
{
    public class TyrannoInteractEffect : CharacterInteractEffectBase
    {
        [SerializeField] private float _interactTime;
        [SerializeField] private Transform _getOffPoint;
        [SerializeField] private TyrannoInteractable _tyrannoInteractable;
        
        private PlayerRef _ownerPlayerRef;
        private PlayerManager _ownerPlayerManager;
        private NetworkRunner _networkRunner;
        private bool _isInteracting;
        private float _interactTimer;
        private InteractableBase _interactable;

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            _networkRunner = target.Runner;
            _interactable = target;
            var charaType = context.CharacterType;
            var playerRef = PlayerRef.FromEncoded(context.Interactor);
            if (charaType == CharacterType.OkabeWright)
            {
                GetOn(playerRef);
            }
        }
        public override void OnInteractFixedNetworkUpdate(PlayerInput playerInput)
        {
            if (_isInteracting)
            {
                _interactTimer += _networkRunner.DeltaTime;
            }
            
            if (CheckInteractEnd())
            {
                GetOff();
            }
            _tyrannoInteractable.OnInteractFixedUpdate(playerInput,_networkRunner.DeltaTime);
        }

   
        private bool CheckInteractEnd()
        {
            if (_interactTimer > _interactTime) return true;
            return !_tyrannoInteractable.IsAlive;
        }

        private void GetOn(PlayerRef ownerPlayerRef)
        {
            _ownerPlayerRef = ownerPlayerRef;
            _ownerPlayerManager = StaticServiceLocator.Instance.Get<InGameManager>()
                .PlayerDataDic[_ownerPlayerRef].GetComponent<PlayerManager>();
            _ownerPlayerManager.SetControlState(PlayerManager.PlayerControlState.ForcedControl);
            _ownerPlayerManager.RPC_SetColliderActive(false);
            _ownerPlayerManager.RPC_SetMeshActive(false);
            _tyrannoInteractable.GetOn(ownerPlayerRef);
            _isInteracting = true;
            _tyrannoInteractable.IsInteractingAnimationTrigger(_isInteracting);
        }

        private void GetOff()
        {
            _ownerPlayerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
            _ownerPlayerManager.RPC_SetColliderActive(true);
            _ownerPlayerManager.RPC_SetMeshActive(true);
            _ownerPlayerManager.transform.position = _getOffPoint.position;
            _tyrannoInteractable.GetOff(_ownerPlayerRef);
            _isInteracting = false;
            _tyrannoInteractable.IsInteractingAnimationTrigger(_isInteracting);
            _interactTimer = 0;
            _ownerPlayerRef = PlayerRef.None;
            _interactable.EndInteract();
        }
        
        public override CharacterInteractEffectBase Clone()
        {
            return new TyrannoInteractEffect()
            {
                _interactTime = _interactTime,
                _getOffPoint = _getOffPoint,
                _tyrannoInteractable = _tyrannoInteractable,
            };
        }
    }
}