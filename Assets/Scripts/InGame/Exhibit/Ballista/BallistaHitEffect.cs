using Fusion;
using InGame.Health;
using UnityEngine;

namespace September.InGame.Exhibit
{
	public class BallistaHitEffect : IProjectileHitEffect
	{
		[SerializeField] private int _baseDamage;
		[SerializeField] private ParticleSystem _hitEffectPrefab;
		private ParticleSystem _hitParticle;
		
		public void Initialize()
		{
			if(!_hitEffectPrefab) return;
			_hitParticle = Object.Instantiate(_hitEffectPrefab);
		}

		public void Hit(Vector3 hitPos, Vector3 normal, GameObject hitObject, PlayerRef usePlayer)
		{
			var damageable = hitObject.transform.GetComponentInParent<IDamageable>();
			if(damageable == null) return;
			var hitData = new HitData(HitActionType.Damage, _baseDamage, usePlayer, damageable.OwnerPlayerRef);
			damageable.TakeHit(ref hitData);
		}

		public void PlayEffect(Vector3 hitPos, Vector3 normal)
		{
			if(!_hitEffectPrefab) return;
			
			_hitParticle.transform.position = hitPos;
			_hitParticle.transform.rotation = Quaternion.LookRotation(normal);
			_hitParticle.Play();
		}

		public void DrawGizmos(Vector3 hitPos, Vector3 normal)
		{
			// 
		}
	}
}