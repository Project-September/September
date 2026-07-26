using Fusion;
using UnityEngine;

namespace September.InGame.Exhibit
{
	[DefaultExecutionOrder(100)]
	public class CannonInteractable : ProjectileInteractableBase
	{
		[SerializeField] private CannonAimRenderer _cannonAimRenderer;

		public override void Spawned()
		{
			base.Spawned();
			_cannonAimRenderer.RenderActive(false);
		}

		public override void Render()
		{
			base.Render();
			_cannonAimRenderer.RenderUpdate();	
		}

		protected override void EffectActive(PlayerRef currentPlayer, bool isActive)
		{
			base.EffectActive(currentPlayer, isActive);
			_cannonAimRenderer.RenderActive(isActive);
		}
	}
}