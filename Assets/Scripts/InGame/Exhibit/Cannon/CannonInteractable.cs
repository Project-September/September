using Fusion;
using UnityEngine;

namespace September.InGame.Exhibit
{
	public class CannonInteractable : ProjectileInteractableBase
	{
		[SerializeField] private CannonAimRenderer _cannonAimRenderer;

		public override void Spawned()
		{
			base.Spawned();
			_cannonAimRenderer.RenderActive(false);
		}

		protected override void EffectActive(PlayerRef currentPlayer, bool isActive)
		{
			base.EffectActive(currentPlayer, isActive);
			_cannonAimRenderer.RenderActive(isActive);
		}

		[Rpc]
		private void RPC_EffectActive(PlayerRef currentPlayer, bool isActive)
		{
			_cannonAimRenderer.RenderActive(isActive);
			
			if(Runner.LocalPlayer == currentPlayer)
			{
				_launcher.IsRenderLine = isActive;
			}
		}
	}
}