using System;
using Fusion;
using InGame.Interact;
using September.Common;
using September.Common.Attribute;
using September.Common.Input;
using UnityEngine;

namespace September.InGame.Mountable
{
    /// <summary>
    /// ボタン入力でマウントを解除する効果
    /// </summary>
    public class MountableManualGetOffInteractEffect : CharacterInteractEffectBase
    {
        [SerializeField, RequireInterface(typeof(IMountable))] private MonoBehaviour _targetMountable;
        
        private IMountable _mountable;
        private InputWrapper _interact;
        private PlayerRef _currentOwner;

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            if (_mountable == null)
            {
                if (_targetMountable is not IMountable mountable)
                {
                    throw new InvalidOperationException($"Target must be a IMountable: {(target ? target.ToString() : "Null")}");
                }
                
                _mountable = mountable;
            }
            
            _currentOwner = PlayerRef.FromEncoded(context.Interactor);
        }

        public override void OnInteractFixedNetworkUpdate(PlayerInput playerInput)
        {
            if (_mountable == null) return;
            
            _interact.SetInput(playerInput.Buttons.IsSet(PlayerButtons.Interact));

            if (_interact.IsJustPressed)
            {
                _mountable.GetOff(_currentOwner);
            }
        }

        public override CharacterInteractEffectBase Clone()
        {
            return MemberwiseClone() as CharacterInteractEffectBase;
        }
    }
}