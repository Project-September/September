using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using InGame.Interact;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace InGame.Exhibit
{
	public class CannonInteractable : NetworkBehaviour
	{
		[Header("移動関連")] [SerializeField] private Transform _cannonBase;
		[SerializeField] private Transform _cannonBarrel;
		[SerializeField] private float _baseRotateSpeed;
		[SerializeField] private float _barrelRotateSpeed;

		// minをxとして、maxをyとして扱う
		[SerializeField] private Vector2 _rotateAngleLimitX = new(-90, 90);
		[SerializeField] private Vector2 _rotateAngleLimitY = new(-90, 90);

		[Header("砲弾に関する設定")] [SerializeField] private Transform _muzzle;
		[SerializeField] private float _simulationStepTime = 0.1f;
		[SerializeField] private float _lifeTime = 10f;
		[SerializeField] private Vector3 _gravity = new(0, -9.81f, 0);
		[SerializeField] private float _projectileVelocity;
		[SerializeField] private int _maxAmmo;
		[SerializeField] private float _reloadTime;
		[SerializeField] private float _radius;
		[SerializeField] private LayerMask _hitLayer;
		[SerializeField] private int _baseDamage = 10;

		[Header("エフェクト設定")] [SerializeField] private GameObject _aimPositionEffect;
		[SerializeField] private NetworkObject _fireParticlePrefab;
		[SerializeField] private NetworkObject _explosionParticlePrefab;
		[SerializeField] private LineRenderer _lineRenderer;
		[SerializeField] private float _aimPositionOffset = .5f;

		[Header("その他")] [SerializeField] private Transform _waitCharacterTransform;
		[SerializeField] private CameraController _cameraController;
		[SerializeField] private InteractableBase _interactable;

		private PlayerManager _currentUsePlayer;
		private Quaternion _baseDefaultRotation;
		private Quaternion _barrelDefaultRotation;
		private UniTask _fireUniTask;
		private int _currentAmmo;
		private Vector3[] _linePositions;
		private int _lastTick = -1;
		private GameObject AimPositionEffect { get; set; }
		private Vector3 HitNormal { get; set; }
		
		[Networked] private PlayerRef CurrentUsePlayerRef { get; set; }
		[Networked] private NetworkObject FireParticle { get; set; }
		[Networked] private NetworkObject ExplosionParticle { get; set; }
		[Networked] private Quaternion BaseRotation { get; set; }
		[Networked] private Quaternion BarrelRotation { get; set; }
		[Networked] private TickTimer LastFireTime { get; set; }
		[Networked] private int LastPositionIndex { get; set; }

		public override void Spawned()
		{
			base.Spawned();
			_linePositions = new Vector3[(int)(_lifeTime / _simulationStepTime)];
			_baseDefaultRotation = _cannonBase.localRotation;
			_barrelDefaultRotation = _cannonBarrel.localRotation;
			BaseRotation = _cannonBase.localRotation;
			BarrelRotation = _cannonBarrel.localRotation;
			AimPositionEffect = Instantiate(_aimPositionEffect);
			AimPositionEffect.SetActive(false);
			AimPositionEffect.transform.localScale = Vector3.one * (_radius * 2);

			if (Runner.IsServer)
			{
				FireParticle = Runner.Spawn(_fireParticlePrefab, Vector3.zero, Quaternion.identity);
				ExplosionParticle = Runner.Spawn(_explosionParticlePrefab, Vector3.zero, Quaternion.identity);
				RPC_ParticleActive(FireParticle, false);
				RPC_ParticleActive(ExplosionParticle, false);
				//RPC_SetActive(AimPositionEffect, false);
			}
		}

		public override void Render()
		{
			base.Render();
			if (_lastTick != Runner.Tick.Raw)
			{
				_lastTick = Runner.Tick.Raw;
				BuildTrajectory();
			}
			// 放物線の作成
			if (Runner.LocalPlayer == CurrentUsePlayerRef)
			{
				CreateLine();
			}
			
			// 着弾地点の描画
			AimPositionEffect.transform.position = GetTargetPoint() + HitNormal * _aimPositionOffset;
			AimPositionEffect.transform.up = HitNormal.normalized;
			// キャノンの回転
			_cannonBarrel.localRotation = BarrelRotation;
			_cannonBase.localRotation = BaseRotation;
		}

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();
		}
		
		[Rpc(RpcSources.All, RpcTargets.All)]
		public void RPC_InteractStart(PlayerRef playerRef)
		{
			AimPositionEffect.SetActive(true);
			// インタラクト中のプレイヤーに対する初期化処理
			if(playerRef != Runner.LocalPlayer) return;
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		public void RPC_InteractEnd(PlayerRef playerRef)
		{
			AimPositionEffect.SetActive(false);
			// インタラクト中のプレイヤーに対する終了処理
			if(playerRef != Runner.LocalPlayer) return;
		}

		/// <summary>
		///     Hostのみで実行されるインタラクト開始時の初期化処理
		/// </summary>
		public void OnInteractStart(PlayerRef playerRef)
		{
			// インタラクトの機能を一時的に無効化する
			_interactable.ForceSetInteractable = false;

			CurrentUsePlayerRef = playerRef;
			RPC_SetCameraPriority(CurrentUsePlayerRef, 15);
			//RPC_SetActive(AimPositionEffect, true);
			Object.AssignInputAuthority(CurrentUsePlayerRef);

			_currentAmmo = _maxAmmo;
			// プレイヤーの取得
			_currentUsePlayer = Runner.GetPlayerObject(CurrentUsePlayerRef).GetComponent<PlayerManager>();
			if (!_currentUsePlayer) return;
			PlayerActive(false);
			_currentUsePlayer.SetWarpTarget(_waitCharacterTransform.position, _waitCharacterTransform.rotation);

			// Playerがダメージを受けた際にInteractを終了する
			_currentUsePlayer.GetComponent<PlayerHealth>().OnHitTaken += PlayerHitTaken;
			RPC_InteractStart(playerRef);
		}

		/// <summary>
		///     インタラクト中の処理
		/// </summary>
		public void OnInteractFixedNetworkUpdate(PlayerInput input)
		{
			base.FixedUpdateNetwork();

			// キャノンの回転内部処理
			CannonRotate(input.MoveDirection.x, input.LookDirection.y);
			_currentUsePlayer?.SetWarpTarget(_waitCharacterTransform.position, _waitCharacterTransform.rotation);

			// 射撃処理
			if (input.Buttons.IsSet(PlayerButtons.Attack) && LastFireTime.ExpiredOrNotRunning(Runner) &&
			    _currentAmmo > 0)
			{
				RPC_Fire();
				LastFireTime = TickTimer.CreateFromSeconds(Runner, _reloadTime);
				_currentAmmo -= 1;
				if (_currentAmmo <= 0) OnInteractEnd();
			}
		}

		/// <summary>
		///     インタラクト終了時の処理
		/// </summary>
		public void OnInteractEnd()
		{
			// クールダウン処理
			var chara = PlayerDatabase.Instance.PlayerDataDic[CurrentUsePlayerRef].CharacterType;
			var time = _interactable.CooldownTimeDictionary.Dictionary.TryGetValue(CharacterType.All, out var all)
				? all
				: _interactable.CooldownTimeDictionary.Dictionary.GetValueOrDefault(chara, 0f);
			_interactable.SetCooldown(time);
			_interactable.ForceSetInteractable = true;

			RPC_Refresh();
			_currentUsePlayer?.SetWarpTarget(_waitCharacterTransform.position, _waitCharacterTransform.rotation);
			if (!_currentUsePlayer) return;
			Object.RemoveInputAuthority();
			PlayerActive(true);
			RPC_SetCameraPriority(CurrentUsePlayerRef, 5);
			if (_currentUsePlayer) _currentUsePlayer.GetComponent<PlayerHealth>().OnHitTaken -= PlayerHitTaken;
			_currentUsePlayer = null;
			CurrentUsePlayerRef = default;
			
			RPC_InteractEnd(CurrentUsePlayerRef);
			//RPC_SetActive(AimPositionEffect, false);

			// 現在砲弾が発射中であった場合、一部エフェクトの終了処理を待つ
			// UniTask.Void(async () =>
			// {
			// 	if (_fireUniTask.Status == UniTaskStatus.Pending)
			// 		await _fireUniTask;
			// 	
			// });
			_interactable.EndInteract();
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_Refresh()
		{
			_lineRenderer.positionCount = 0;
			BarrelRotation = _barrelDefaultRotation;
			BaseRotation = _baseDefaultRotation;
		}

		/// <summary>
		/// 事前に計算された軌道に沿って線描画する
		/// </summary>
		private void CreateLine()
		{
			_lineRenderer.positionCount = LastPositionIndex + 1;
			_lineRenderer.SetPositions(_linePositions);
		}

		/// <summary>
		///     放物線の軌道を計算する。
		///     障害物に当たった場合、そこを最終地点とする。
		///     結果は_linePositionsと_lastPositionIndexに保存される。
		/// </summary>
		private void BuildTrajectory()
		{
			for (var i = 0; i < _linePositions.Length; i++)
			{
				// 次の地点の計算
				var time = i * _simulationStepTime;
				var pos = CalculateTrajectoryPosition(_muzzle.position,
					_muzzle.transform.forward * _projectileVelocity, _gravity, time);
				_linePositions[i] = pos;

				if (i == 0) continue;
				// 弾が移動予定の位置までRayを飛ばし、当たり判定を確認する
				var ray = new Ray(_linePositions[i - 1], _linePositions[i] - _linePositions[i - 1]);

				// 障害物が存在した場合、その地点を最終地点とする。
				if (Physics.Raycast(ray, out var hit, Vector3.Distance(_linePositions[i - 1], _linePositions[i])))
				{
					_linePositions[i] = hit.point;
					LastPositionIndex = i;
					HitNormal = hit.normal;
					return;
				}
			}

			LastPositionIndex = _linePositions.Length - 1;
			HitNormal = Vector3.up;
		}

		private void PlayerActive(bool isActive)
		{
			if (Runner.IsServer)
			{
				RPC_SetCameraPriority(CurrentUsePlayerRef, isActive ? 0 : 15);
				_currentUsePlayer.RPC_SetControlState(isActive
					? PlayerManager.PlayerControlState.Normal
					: PlayerManager.PlayerControlState.ForcedControl);
				_currentUsePlayer.RPC_SetUseGrav(isActive);
				_currentUsePlayer.GetComponent<Rigidbody>().isKinematic = !isActive;
			}
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_Fire()
		{
			_fireUniTask = Fire();
		}

		private async UniTask Fire()
		{
			if (!Runner.IsServer) return;
			var timer = 0f;

			// 発射向きなどの情報を保持しておく
			var muzzlePosition = _muzzle.position;
			var muzzleForward = _muzzle.transform.forward;
			var ballSpeed = _projectileVelocity;
			var endPos = GetTargetPoint();

			RPC_ParticleActive(FireParticle, true);

			// 砲弾の位置をUpdateする
			while (timer < _lifeTime)
			{
				timer += Runner.DeltaTime;
				var currentPos = CalculateTrajectoryPosition(muzzlePosition,
					muzzleForward * ballSpeed, _gravity, timer);
				var nextPos = CalculateTrajectoryPosition(muzzlePosition,
					muzzleForward * ballSpeed, _gravity, timer + Runner.DeltaTime);

				// 弾の着弾確認
				var movement = nextPos - currentPos;
				if (Physics.Raycast(currentPos, movement.normalized, out var hit, movement.magnitude, _hitLayer))
				{
					endPos = hit.point;
					break;
				}
				
				// particleの更新
				FireParticle.transform.position = currentPos;
				FireParticle.transform.forward = movement.normalized;

				await UniTask.WaitForSeconds(Runner.DeltaTime);
			}

			RPC_ParticleActive(FireParticle, false);

			RPC_Explosion(endPos, Quaternion.identity);
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_Explosion(Vector3 position, Quaternion rotation)
		{
			Explosion(position, rotation);
		}

		private void Explosion(Vector3 position, Quaternion rotation)
		{
			// 着弾時のエフェクト
			ExplosionParticle.transform.position = position;
			ExplosionParticle.transform.rotation = rotation;
			RPC_ParticleActive(ExplosionParticle, true);
			UniTask.Void(async () =>
			{
				var particle = ExplosionParticle.GetComponent<ParticleSystem>();
				if (!particle) return;
				particle.Play(true);
				while (particle.isPlaying)
					await UniTask.Yield();
				RPC_ParticleActive(ExplosionParticle, false);
			});

			var cols = Physics.OverlapSphere(position, _radius, _hitLayer); // TODO:当たり判定統一するかも

			// ダメージ処理
			foreach (var col in cols)
			{
				var damageable = col.GetComponentInParent<IDamageable>();
				if (damageable == null) continue;
				if (damageable.OwnerPlayerRef == CurrentUsePlayerRef) continue;
				var hitData = new HitData(HitActionType.Damage, _baseDamage, CurrentUsePlayerRef,
					damageable.OwnerPlayerRef);
				damageable.TakeHit(ref hitData);
			}
		}

		private void CannonRotate(float baseRotateInput, float barrelRotateInput)
		{
			// 土台の回転
			var currentBaseAxis = _cannonBase.localEulerAngles.y +
			                      _baseRotateSpeed * baseRotateInput;
			currentBaseAxis = WrapAngle(currentBaseAxis);
			BaseRotation = _baseDefaultRotation * Quaternion.Euler(0,
				Mathf.Clamp(currentBaseAxis, _rotateAngleLimitX.x, _rotateAngleLimitX.y), 0);

			// 砲身の回転
			var currentBarrelAxis = _cannonBarrel.localEulerAngles.x +
			                        _barrelRotateSpeed * barrelRotateInput;
			currentBarrelAxis = WrapAngle(currentBarrelAxis);
			BarrelRotation = _cannonBase.localRotation * Quaternion.Euler(
				Mathf.Clamp(currentBarrelAxis, _rotateAngleLimitY.x, _rotateAngleLimitY.y), 0, 0);
		}

		private void PlayerHitTaken(HitData hitData)
		{
			OnInteractEnd();
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_ParticleActive(NetworkObject obj, bool isActive)
		{
			var particle = obj.GetComponentInChildren<ParticleSystem>();

			if (isActive)
				particle.Play();
			else
				particle.Stop(
					true,
					ParticleSystemStopBehavior.StopEmittingAndClear);
		}

		#region Helper

		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_SetCameraPriority(PlayerRef playerRef, int priority)
		{
			if (Runner.LocalPlayer != playerRef) return;
			_cameraController.SetCameraPriority(priority);
		}

		private Vector3 GetTargetPoint()
		{
			return _linePositions[LastPositionIndex];
		}

		/// <summary>
		///     角度を-180~180に変換する
		/// </summary>
		private float WrapAngle(float angle)
		{
			angle %= 360;
			if (angle > 180) angle -= 360;
			return angle;
		}


		/// <summary>
		///     特定の時間での放物線位置を計算する
		/// </summary>
		/// <returns>入力時間の時の位置</returns>
		private Vector3 CalculateTrajectoryPosition(Vector3 startPos, Vector3 velocity, Vector3 gravity, float time)
		{
			return startPos + velocity * time + 0.5f * gravity * (time * time);
		}

		#endregion

		#region Gizmos

		private void OnDrawGizmos()
		{
			if (!Object || !Object.IsValid) return;
			// 当たり判定の可視化
			var colliderPos = GetTargetPoint();
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(colliderPos, _radius);
		}

		#endregion
	}
}