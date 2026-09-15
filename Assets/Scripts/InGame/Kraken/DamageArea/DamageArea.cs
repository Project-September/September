using System;
using System.Collections.Generic;
using Fusion;
using InGame.Health;
using UnityEngine;

namespace September.InGame.Kraken
{
	[RequireComponent(typeof(Rigidbody), typeof(Collider))]
	public class DamageArea : NetworkBehaviour
	{
		[SerializeField] private float _damageInterval;
		[SerializeField] private int _damage;
		[SerializeField] private ParticleSystem[] _poisonEffects;
		private TickTimer _tickTimer;
		private readonly List<IDamageable> _damageableObjects = new();
		private bool enable = false;
		private PlayerRef _playerRef;

		public override void Spawned()
		{
			base.Spawned();
			EffectActive(false);
		}

		public void EnableDamageArea(PlayerRef player)
		{
			_playerRef = player;
			enable = true;
			_tickTimer = TickTimer.CreateFromSeconds(Runner, _damageInterval);
			RPC_EffectActive(true);
		}

		public void DisableDamageArea()
		{
			_playerRef = default;
			enable = false;
			_tickTimer = default;
			RPC_EffectActive(false);
		}

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();
			if (!HasStateAuthority || !enable) return;
			if (!_tickTimer.Expired(Runner)) return;

			_tickTimer = TickTimer.CreateFromSeconds(Runner, _damageInterval);
			foreach (var damageableObject in _damageableObjects)
			{
				var hit = new HitData(HitActionType.RangedDamage, _damage, _playerRef, damageableObject.OwnerPlayerRef);
				damageableObject.TakeHit(ref hit);
			}
		}

		[Rpc]
		void RPC_EffectActive(bool active)
		{
			EffectActive(active);
		}

		void EffectActive(bool active)
		{
			foreach (var poisonEffect in _poisonEffects)
			{
				//poisonEffect.gameObject.SetActive(active);
				if(active)
					poisonEffect.Play();
				else
				{
					poisonEffect.Stop();
				}
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			var damageable = other.GetComponentInParent<IDamageable>();
			if (damageable == null || _damageableObjects.Contains(damageable)) return;
			_damageableObjects.Add(damageable);
		}

		private void OnTriggerExit(Collider other)
		{
			var damageable = other.GetComponentInParent<IDamageable>();
			if (damageable == null) return;
			_damageableObjects.Remove(damageable);
		}
	}
}