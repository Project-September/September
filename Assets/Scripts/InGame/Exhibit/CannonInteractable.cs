using System;
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
		[SerializeField] private Vector3 _gravity;
		[SerializeField] private float _ballSpeed;
		[SerializeField] private int _maxAmmo;
		[SerializeField] private float _reloadTime;
		[SerializeField] private float _radius;
		[SerializeField] private LayerMask _hitLayer;
		[SerializeField] private int _baseDamage = 10;

		[Header("エフェクト設定")] [SerializeField] private NetworkObject _aimPositionViewPrefab;
		[SerializeField] private NetworkObject _fireParticlePrefab;
		[SerializeField] private NetworkObject _explosionParticlePrefab;
		[SerializeField] private LineRenderer _lineRenderer;

		[Header("その他")] [SerializeField] private Transform _waitCharacterTransform;
		[SerializeField] private CameraController _cameraController;
		[SerializeField] private InteractableBase _interactable;

		private PlayerRef _currentUsePlayerRef;
		private PlayerManager _currentUsePlayer;
		private Quaternion _baseDefaultRotation;
		private Quaternion _barrelDefaultRotation;
		private UniTask _fireUniTask;
		private int _currentAmmo;
		private readonly Vector3[] _linePositions = new Vector3[32];
		[Networked] private NetworkObject FireParticle { get; set; }
		[Networked] private NetworkObject ExplosionParticle { get; set; }
		[Networked] private NetworkObject AimPositionViewObj { get; set; }
		[Networked] private Quaternion BaseRotation { get; set; }
		[Networked] private Quaternion BarrelRotation { get; set; }
		[Networked] public float InteractCoolTime { get; set; }
		[Networked] private TickTimer LastFireTime { get; set; }
		[Networked] private int LastPositionIndex { get; set; }

		public override void Spawned()
		{
			base.Spawned();
			_baseDefaultRotation = _cannonBase.localRotation;
			_barrelDefaultRotation = _cannonBarrel.localRotation;
			BaseRotation = _cannonBase.localRotation;
			BarrelRotation = _cannonBarrel.localRotation;

			if (Runner.IsServer)
			{
				FireParticle = Runner.Spawn(_fireParticlePrefab, Vector3.zero, Quaternion.identity);
				ExplosionParticle = Runner.Spawn(_explosionParticlePrefab, Vector3.zero, Quaternion.identity);
				AimPositionViewObj = Runner.Spawn(_aimPositionViewPrefab, Vector3.zero, Quaternion.identity);
				AimPositionViewObj.transform.localScale = Vector3.one * (_radius * 2);
				RPC_SetActive(FireParticle, false);
				RPC_SetActive(ExplosionParticle, false);
				RPC_SetActive(AimPositionViewObj, false);
			}
		}

		public override void Render()
		{
			base.Render();
			// 放物線描画
			CreateLine();
			if (Runner.IsServer) AimPositionViewObj.transform.position = GetTargetPoint();

			// キャノンの回転描画
			_cannonBarrel.localRotation = BarrelRotation;
			_cannonBase.localRotation = BaseRotation;
			_cameraController.transform.rotation = BaseRotation;
		}

		/// <summary>
		///     インタラクト開始時の初期化処理
		/// </summary>
		public void OnInteractStart(PlayerRef playerRef)
		{
			_interactable.enabled = false; // インタラクトの機能を一時的に無効化する
			_currentUsePlayerRef = playerRef;
			RPC_SetCameraPriority(_currentUsePlayerRef, 15);
			Object.AssignInputAuthority(_currentUsePlayerRef);
			_currentAmmo = _maxAmmo;
			RPC_SetActive(AimPositionViewObj, true);

			// プレイヤーの取得
			_currentUsePlayer = Runner.GetPlayerObject(_currentUsePlayerRef).GetComponent<PlayerManager>();
			if (!_currentUsePlayer) return;
			PlayerActive(false);
			_currentUsePlayer.SetWarpTarget(_waitCharacterTransform.position, _waitCharacterTransform.rotation);

			// Playerがダメージを受けた際にInteractを終了する
			_currentUsePlayer.GetComponent<PlayerHealth>().OnHitTaken += PlayerHitTaken;
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
			_interactable.SetCooldown(InteractCoolTime);
			RPC_Refresh();
			_currentUsePlayer?.SetWarpTarget(_waitCharacterTransform.position, _waitCharacterTransform.rotation);
			if (!_currentUsePlayer) return;
			Object.RemoveInputAuthority();
			PlayerActive(true);
			RPC_SetCameraPriority(_currentUsePlayerRef, 5);
			if (_currentUsePlayer)
				_currentUsePlayer.GetComponent<PlayerHealth>().OnHitTaken -= PlayerHitTaken;
			_currentUsePlayer = null;
			_currentUsePlayerRef = default;
			_interactable.EndInteract();
			
			UniTask.Void(async () =>
			{
				if (_fireUniTask.Status == UniTaskStatus.Pending)
					await _fireUniTask;
				RPC_SetActive(AimPositionViewObj, false);
			});
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_Refresh()
		{
			_lineRenderer.positionCount = 0;
			_cannonBarrel.rotation = _barrelDefaultRotation;
			_cannonBase.rotation = _baseDefaultRotation;
		}

		private void CreateLine()
		{
			if (!_currentUsePlayer) return;
			BuildTrajectory(); // 描画位置の計算
			_lineRenderer.positionCount = LastPositionIndex + 1;
			_lineRenderer.SetPositions(_linePositions);
		}

		/// <summary>
		///     放物線の軌道を計算し、当たり判定を取得する。
		///     障害物に当たった場合、そこを最終地点とする。
		///     結果は_linePositionsと_lastPositionIndexに保存される。
		/// </summary>
		private void BuildTrajectory()
		{
			for (var i = 0; i < _linePositions.Length; i++)
			{
				// 次の地点の計算
				var time = i * _simulationStepTime;
				var pos = TrajectoryCalculator(_muzzle.position,
					_muzzle.transform.forward * _ballSpeed, 9.8f, time);
				_linePositions[i] = pos;

				if (i == 0) continue;
				// 当たり判定の取得
				var ray = new Ray(_linePositions[i - 1], _linePositions[i] - _linePositions[i - 1]);

				// 障害物が存在した場合、その地点を最終地点とする。
				if (Physics.Raycast(ray, out var hit, Vector3.Distance(_linePositions[i - 1], _linePositions[i])))
				{
					_lineRenderer.positionCount = i;
					_linePositions[i] = hit.point;
					LastPositionIndex = i;
					return;
				}
			}

			LastPositionIndex = _linePositions.Length - 1;
		}

		private void PlayerActive(bool isActive)
		{
			if (Runner.IsServer)
			{
				RPC_SetCameraPriority(_currentUsePlayerRef, isActive ? 0 : 15);
				_currentUsePlayer.RPC_SetControlState(isActive
					? PlayerManager.PlayerControlState.Normal
					: PlayerManager.PlayerControlState.ForcedControl);
				_currentUsePlayer.RPC_SetUseGrav(isActive);
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
			var ballSpeed = _ballSpeed;
			var endPos = GetTargetPoint();
			var endTime = _simulationStepTime * LastPositionIndex;

			RPC_SetActive(FireParticle, true);

			// 弾丸の位置をUpdateする
			while (timer < endTime)
			{
				timer += Runner.DeltaTime;
				var from = TrajectoryCalculator(muzzlePosition,
					muzzleForward * ballSpeed, 9.8f, timer);
				var to = TrajectoryCalculator(muzzlePosition,
					muzzleForward * ballSpeed, 9.8f, timer + Runner.DeltaTime);

				//弾の位置更新
				FireParticle.transform.position = from;

				// 弾の着弾確認
				var fromTo = to - from;
				if (Physics.Raycast(from, fromTo.normalized, out var hit, fromTo.sqrMagnitude, _hitLayer))
				{
					endPos = hit.point;
					break;
				}

				await UniTask.WaitForSeconds(Runner.DeltaTime);
			}

			RPC_SetActive(FireParticle, false);

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
			RPC_SetActive(ExplosionParticle, true);
			UniTask.Void(async () =>
			{
				var particle = ExplosionParticle.GetComponent<ParticleSystem>();
				if (!particle) return;
				particle.Play(true);
				while (particle.isPlaying)
					await UniTask.Yield();
				RPC_SetActive(ExplosionParticle, false);
			});

			var cols = Physics.OverlapSphere(position, _radius, _hitLayer); // TODO:当たり判定統一するかも

			// ダメージ処理
			foreach (var col in cols)
			{
				var damageable = col.GetComponentInParent<IDamageable>();
				if (damageable == null) continue;
				if (damageable.OwnerPlayerRef == _currentUsePlayerRef) continue;
				var hitData = new HitData(HitActionType.Damage, _baseDamage, _currentUsePlayerRef,
					damageable.OwnerPlayerRef);
				damageable.TakeHit(ref hitData);
			}
		}

		private void CannonRotate(float baseRotateInput, float barrelRotateInput)
		{
			// 土台の回転
			var currentBaseAxis = _cannonBase.localEulerAngles.y +
			                      _baseRotateSpeed * baseRotateInput;
			currentBaseAxis = WarpAngle(currentBaseAxis);
			BaseRotation = _baseDefaultRotation * Quaternion.Euler(0,
				Mathf.Clamp(currentBaseAxis, _rotateAngleLimitX.x, _rotateAngleLimitX.y), 0);

			// 砲身の回転
			var currentBarrelAxis = _cannonBarrel.localEulerAngles.x +
			                        _barrelRotateSpeed * barrelRotateInput;
			currentBarrelAxis = WarpAngle(currentBarrelAxis);
			BarrelRotation = _cannonBase.localRotation * Quaternion.Euler(
				Mathf.Clamp(currentBarrelAxis, _rotateAngleLimitY.x, _rotateAngleLimitY.y), 0, 0);
		}

		private void PlayerHitTaken(HitData hitData)
		{
			OnInteractEnd();
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_SetActive(NetworkObject obj, bool isActive)
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
		private float WarpAngle(float angle)
		{
			angle %= 360;
			if (angle > 180) angle -= 360;
			return angle;
		}


		/// <summary>
		///     特定の時間での放物線位置を計算する
		/// </summary>
		/// <returns>入力時間の時の位置</returns>
		private Vector3 TrajectoryCalculator(Vector3 startPos, Vector3 power, float gravity, float time)
		{
			return startPos + power * time + Vector3.down * (gravity * time * time);
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