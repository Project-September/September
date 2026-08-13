using System;
using UnityEngine;

namespace September.InGame.Exhibit
{
	[Serializable]
	public class CannonReticule : IReticuleEffect
	{
		[SerializeField] private ProjectileLauncher _projectileLauncher;
		[SerializeField] private float _hitReticuleRadius;
		[SerializeField] private GameObject AimPositionEffectPrefab;
		[SerializeField] private LineRenderer _lineRenderer;
		[SerializeField] private float AimPositionOffset = 0.2f;
		private GameObject AimPositionEffect;
		private bool isActive = false;
		public void Init()
		{
			var size = _hitReticuleRadius * 2;
			if(!AimPositionEffect)
			{
				AimPositionEffect = GameObject.Instantiate(AimPositionEffectPrefab);
				AimPositionEffect.SetActive(false);
			}
			AimPositionEffect.transform.localScale = new Vector3(size, size, size);
			AimPositionEffect.SetActive(false);
			
			LineReset();
		}

		public void Render()
		{
			if(!isActive) return;
			AimPositionEffect.transform.position =
				_projectileLauncher.HitPosition + _projectileLauncher.HitNormal * AimPositionOffset;
			AimPositionEffect.transform.up = _projectileLauncher.HitNormal;

			var span = _projectileLauncher.LinePositions;
			_lineRenderer.positionCount = span.Length;
			_lineRenderer.SetPositions(span.ToArray());
		}

		public void SetActive(bool active)
		{
			AimPositionEffect?.SetActive(active);
			isActive = active;
			if(!active)
			{
				LineReset();
			}
		}

		public void LineReset()
		{
			_lineRenderer.positionCount = 0;
		}
	}
}