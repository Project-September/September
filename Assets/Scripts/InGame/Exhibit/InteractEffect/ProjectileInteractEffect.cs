using System;
using Fusion;
using InGame.Interact;
using September.Common;
using UnityEngine;

namespace September.InGame.Exhibit
{
	[Serializable]
	public class BallistaInteractEffect:CharacterInteractEffectBase
	{
		[SerializeField] private ProjectileInteractableBase _ballistaInteractable;
		public BallistaInteractEffect(ProjectileInteractableBase interactable)
		{
			_ballistaInteractable = interactable;
		}

		public BallistaInteractEffect()
		{
			
		}
		
		public override void OnInteractStart(IInteractableContext context, InteractableBase target)
		{
			var playerRef = PlayerRef.FromEncoded(context.Interactor);
			_ballistaInteractable.InteractStart(playerRef);
		}

		public override void OnInteractFixedNetworkUpdate(PlayerInput playerInput)
		{
			base.OnInteractFixedNetworkUpdate(playerInput);
			_ballistaInteractable.InteractFixedNetworkUpdate(playerInput);
		}

		public override CharacterInteractEffectBase Clone()
		{
			return new BallistaInteractEffect(_ballistaInteractable);
		}
		
	}
}