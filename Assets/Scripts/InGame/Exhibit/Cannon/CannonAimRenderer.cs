using Fusion;
using UnityEngine;

namespace September.InGame.Exhibit
{
	public class CannonAimRenderer : MonoBehaviour
	{
		[SerializeField] private ProjectileLauncher _projectileLauncher;
		[SerializeField] private GameObject AimPositionEffectPrefab;
		[SerializeField] private float AimPositionOffset = 0.2f;
		private GameObject AimPositionEffect;
		private bool IsActive = false;

		public void RenderUpdate()
		{
			if(!IsActive) return;
			AimPositionEffect.transform.position =
				_projectileLauncher.HitPosition + _projectileLauncher.HitNormal * AimPositionOffset;
			AimPositionEffect.transform.up = _projectileLauncher.HitNormal;
		}

		/// <summary>
		/// 初期化時処理
		/// </summary>
		/// <param name="size">EffectSize</param>
		public void Initialize(float size)
		{
			if(!AimPositionEffect)
			{
				AimPositionEffect = Instantiate(AimPositionEffectPrefab, transform);
				AimPositionEffect.SetActive(false);
			}
			AimPositionEffect.transform.localScale = new Vector3(size, size, size);
		}
		
		public void RenderActive(bool active)
		{
			AimPositionEffect?.SetActive(active);
			IsActive = active;
		}
	}
}