using System;
using System.Collections.Generic;
using Fusion;
using InGame.Health;
using UnityEngine;

namespace September.InGame.Kraken
{
	public class DamageArea : NetworkBehaviour
	{
		[SerializeField] private float _damageInterval;
		[SerializeField] private int _damage;
		[SerializeField] private ParticleSystem[] _poisonEffects;
		[SerializeField] private Vector3 _hitAreaOffset;
		[SerializeField] private Vector3 _hitAreaSize;
		[SerializeField] private LayerMask _hitLayer;
		private TickTimer _tickTimer;
		private readonly List<IDamageable> _damageableObjects = new();
		private bool _enable = false;
		private PlayerRef _playerRef;

		public override void Spawned()
		{
			base.Spawned();
			EffectActive(false);
		}

		public void EnableDamageArea(PlayerRef player)
		{
			_playerRef = player;
			_enable = true;
			_tickTimer = TickTimer.CreateFromSeconds(Runner, _damageInterval);
			RPC_EffectActive(true);
		}

		public void DisableDamageArea()
		{
			_playerRef = default;
			_enable = false;
			_tickTimer = default;
			RPC_EffectActive(false);
		}

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();
			if (!HasStateAuthority || !_enable) return;
			if (!_tickTimer.Expired(Runner)) return;
			// タイマーの更新
			_tickTimer = TickTimer.CreateFromSeconds(Runner, _damageInterval);
			FindDamageableObjects(_damageableObjects);
			// ダメージ処理
			foreach (var damageableObject in _damageableObjects)
			{
				var hit = new HitData(HitActionType.RangedDamage, _damage, _playerRef, damageableObject.OwnerPlayerRef);
				damageableObject.TakeHit(ref hit);
			}
		}

		private void FindDamageableObjects(List<IDamageable> damageableObjects)
		{
			damageableObjects.Clear();
			var cols = Physics.OverlapBox(transform.position + _hitAreaOffset, _hitAreaSize * .5f, transform.rotation, _hitLayer);
			foreach (var col in cols)
			{
				var damageable = col.GetComponentInParent<IDamageable>();
				if (damageable == null) continue;
				damageableObjects.Add(damageable);
			}
		}

		[Rpc]
		private void RPC_EffectActive(bool active)
		{
			EffectActive(active);
		}

		private void EffectActive(bool active)
		{
			foreach (var poisonEffect in _poisonEffects)
			{
				if(active)
					poisonEffect.Play();
				else
					poisonEffect.Stop();
			}
		}
		
		private void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(transform.position+ _hitAreaOffset, _hitAreaSize);
		}
	}
}