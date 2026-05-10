using InGame.Interact;
using September.Common;
using UnityEngine;

namespace September.InGame.Mountable
{
    /// <summary>
    /// 複数のInteractEffectを同時実行する
    /// </summary>
    public class AggregateInteractEffect : CharacterInteractEffectBase
    {
        [SerializeReference, SubclassSelector] private CharacterInteractEffectBase[] _interactEffects;
        
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            foreach (var effect in _interactEffects)
            {
                effect.OnInteractStart(context, target);
            }
        }

        public override void OnInteractUpdate(float deltaTime) 
        {
            foreach (var effect in _interactEffects)
            {
                effect.OnInteractUpdate(deltaTime);
            }
        }
        
        public override void OnInteractLateUpdate(float deltaTime) 
        {
            foreach (var effect in _interactEffects)
            {
                effect.OnInteractLateUpdate(deltaTime);
            }
        }
        
        public override void OnInteractFixedUpdate() 
        {
            foreach (var effect in _interactEffects)
            {
                effect.OnInteractFixedUpdate();
            }
        }
        
        public override void OnInteractFixedNetworkUpdate(PlayerInput playerInput) 
        {
            foreach (var effect in _interactEffects)
            {
                effect.OnInteractFixedNetworkUpdate(playerInput);
            }
        }
        
        public override void OnInteractCollisionStay(Collision collision) 
        {
            foreach (var effect in _interactEffects)
            {
                effect.OnInteractCollisionStay(collision);
            }
        }
        
        public override void OnInteractEnd() 
        {
            foreach (var effect in _interactEffects)
            {
                effect.OnInteractEnd();
            }
        }

        public override CharacterInteractEffectBase Clone()
        {
            return MemberwiseClone() as CharacterInteractEffectBase;
        }
    }
}