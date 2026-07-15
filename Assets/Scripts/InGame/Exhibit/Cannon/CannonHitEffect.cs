using Fusion;
using InGame.Health;
using September.Common;
using UnityEngine;

namespace September.InGame.Exhibit
{
	[System.Serializable]
	public class CannonHitEffect : IProjectileHitEffect
	{
		[SerializeField] private CannonAimRenderer _cannonAimRenderer;
		[SerializeField] private GameObject _explosionParticlePrefab;
		[SerializeField] private float _radius;
		[SerializeField] private int _damage;
		[SerializeField] private LayerMask _hitLayer;
		private GameObject _explosionParticle;

		public void Initialize()
		{
			_explosionParticle = Object.Instantiate(_explosionParticlePrefab);
			_cannonAimRenderer.Initialize(_radius * 2);// 半径からObjectScaleに
		}
		
		public void PlayEffect(Vector3 position, Vector3 normal)
		{
			// 着弾時のエフェクト
			_explosionParticle.transform.position = position;
			_explosionParticle.transform.up = normal.normalized;

			var particle = _explosionParticle.GetComponent<ParticleSystem>();
			if (!particle) return;
			particle.Play(true);
		}

		public void Hit(Vector3 position, Vector3 normal, GameObject hitObject, PlayerRef usePlayer)
		{
			var colliders = Physics.OverlapSphere(position, _radius, _hitLayer); // TODO:当たり判定統一するかも
			// ダメージ処理
			foreach (var col in colliders)
			{
				var damageable = col.GetComponentInParent<IDamageable>();
				if (damageable == null) continue;
				if (damageable.OwnerPlayerRef == usePlayer) continue;
				TakeDamage(damageable, usePlayer);
			}
		}
		
		public void DrawGizmos(Vector3 position, Vector3 normal)
		{
			var colliderPos = position;
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(colliderPos, _radius);
		}

		private void TakeDamage(IDamageable damageable, PlayerRef usingPlayer)
		{
			var hitData = new HitData(HitActionType.Damage, _damage, usingPlayer,
				damageable.OwnerPlayerRef);
			PlayerDatabase.Instance.PlayerDataDic.Get(damageable.OwnerPlayerRef);
			PlayerDatabase.Instance.PlayerDataDic.Get(usingPlayer);
			
			damageable.TakeHit(ref hitData);
		}
		
	}
}