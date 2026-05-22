using System.Linq;
using InGame.Interact;
using September.Common;
using UnityEngine;

namespace September.InGame.Interact
{
    /// <summary>
    /// 複数のInteractEffectを同時実行する
    /// </summary>
    public class CompositeInteractEffect : CharacterInteractEffectBase
    {
        [SerializeReference, SubclassSelector] private CharacterInteractEffectBase[] _interactEffects;

        private IInteractableContext _context;

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            _context = context;

            foreach (var effect in _interactEffects)
            {
                if (!effect.IsExecutable(_context.CharacterType)) continue;
                effect.OnInteractStart(context, target);
            }
        }

        public override void OnInteractUpdate(float deltaTime) 
        {
            foreach (var effect in _interactEffects)
            {
                if (!effect.IsExecutable(_context.CharacterType)) continue;
                effect.OnInteractUpdate(deltaTime);
            }
        }
        
        public override void OnInteractLateUpdate(float deltaTime) 
        {
            foreach (var effect in _interactEffects)
            {
                if (!effect.IsExecutable(_context.CharacterType)) continue;
                effect.OnInteractLateUpdate(deltaTime);
            }
        }
        
        public override void OnInteractFixedUpdate() 
        {
            foreach (var effect in _interactEffects)
            {
                if (!effect.IsExecutable(_context.CharacterType)) continue;
                effect.OnInteractFixedUpdate();
            }
        }
        
        public override void OnInteractFixedNetworkUpdate(PlayerInput playerInput) 
        {
            foreach (var effect in _interactEffects)
            {
                if (!effect.IsExecutable(_context.CharacterType)) continue;
                effect.OnInteractFixedNetworkUpdate(playerInput);
            }
        }
        
        public override void OnInteractCollisionStay(Collision collision) 
        {
            foreach (var effect in _interactEffects)
            {
                if (!effect.IsExecutable(_context.CharacterType)) continue;
                effect.OnInteractCollisionStay(collision);
            }
        }
        
        public override void OnInteractEnd() 
        {
            foreach (var effect in _interactEffects)
            {
                if (!effect.IsExecutable(_context.CharacterType)) continue;
                effect.OnInteractEnd();
            }
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new CompositeInteractEffect
            {
                _interactEffects = _interactEffects.Select(x => x.Clone()).ToArray()
            };
        }
    }
}
