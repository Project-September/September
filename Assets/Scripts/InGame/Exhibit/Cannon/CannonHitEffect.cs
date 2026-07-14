using Fusion;
using InGame.Health;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace September.InGame.Exhibit
{
	[System.Serializable]
	public class CannonHitEffect : IProjectileHitEffect
	{
		[SerializeField] private GameObject _explosionParticlePrefab;
		[SerializeField] private float _radius;
		[SerializeField] private int _damage;
		[SerializeField] private float _nockBackPower;
		[SerializeField] private LayerMask _hitLayer;
		private GameObject _explosionParticle;

		public void PlayEffect(Vector3 position, Quaternion rotation)
		{
			if (_explosionParticle == null) _explosionParticle = Object.Instantiate(_explosionParticlePrefab);
			// 着弾時のエフェクト
			_explosionParticle.transform.position = position;
			_explosionParticle.transform.rotation = rotation;

			var particle = _explosionParticle.GetComponent<ParticleSystem>();
			if (!particle) return;
			particle.Play(true);
		}

		public void Hit(Vector3 position, Quaternion rotation, GameObject hitObject, PlayerRef usePlayer)
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