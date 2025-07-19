using Fusion;
using InGame.Interact;
using UnityEngine;

namespace InGame.Exhibit
{
    public class EquipCannonBallEffect : CharacterInteractEffectBase
    {
        [SerializeField] private CannonBall _canonBallModel;
        [SerializeField] private NetworkObject _networkObject;

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            target.IsInteractable = false;
            
            _canonBallModel.EquipToInteractor(context.Interactor);
            _canonBallModel.OnCannonBallHit += (() =>
            {
                target.IsInteractable = true;
                target.LastInteractTime = target.Runner.SimulationTime;
            });
        }
        
        public override CharacterInteractEffectBase Clone()
        {
            return new EquipCannonBallEffect
            {
                _canonBallModel = _canonBallModel,
                _networkObject = _networkObject
            };
        }
    }
}
