using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace InGame.Exhibit
{
	public class CannonInteractable : NetworkBehaviour
	{
		[SerializeField] private Transform _muzzle;
		[Header("移動関連")] [SerializeField] private Transform _cannonBase;
		[SerializeField] private Transform _cannonBarrel;
		[SerializeField] private float _baseRotateSpeed;

		[SerializeField] private float _barrelRotateSpeed;

		// minをxとして、maxをyとして扱う
		[SerializeField] private Vector2 _angleLimitX = new(-90, 90);
		[SerializeField] private Vector2 _angleLimitY = new(-90, 90);

		[Header("描画")] [SerializeField] private Transform _aimHitViewObject;
		[SerializeField] private LineRenderer _lineRenderer;
		[SerializeField] private NetworkObject _fireParticlePrefab;
		[SerializeField] private NetworkObject _explosionParticlePrefab;

		[Header("射撃")] [SerializeField] private LayerMask _playerHitMask;
		[SerializeField] private float _simulationStepTime = 0.1f;
		[SerializeField] private Vector3 _gravity;
		[SerializeField] private float _ballSpeed;
		[SerializeField] private int _maxAmmo;
		[SerializeField] private float _reloadTime;
		[SerializeField] private float _radius;
		public int _damage = 10;

		[Header("PlayerCharacter")] [SerializeField]
		private Transform _waitCharacterTransform;

		public PlayerRef _ownerPlayerRef;

		private PlayerManager _currentUsePlayer;
		private Quaternion _baseDefaultRotation;
		private Quaternion _barrelDefaultRotation;
		private int _currentAmmo;
		private Vector3[] _linePositions = new Vector3[32];
		private NetworkObject _fireParticle;
		private NetworkObject _explosionParticle;
		private NetworkObject _aimObj;

		[Networked] private Quaternion _baseRotation { get; set; }
		[Networked] private Quaternion _barrelRotation { get; set; }

		[Networked] public bool IsActive { get; private set; }
		[Networked] private TickTimer LastFireTime { get; set; }
		[Networked] private int _lastPositionIndex { get; set; }

		#region Networked Methods

		public override void Spawned()
		{
			base.Spawned();
			_baseDefaultRotation = _cannonBase.localRotation;
			_barrelDefaultRotation = _cannonBarrel.localRotation;
			_baseRotation = _cannonBase.localRotation;
			_barrelRotation = _cannonBarrel.localRotation;
			_fireParticle = Runner.Spawn(_fireParticlePrefab, Vector3.zero, Quaternion.identity);
			_explosionParticle = Runner.Spawn(_explosionParticlePrefab, Vector3.zero, Quaternion.identity);
		}

		public override void Render()
		{
			base.Render();
			CreateLine();
			_cannonBarrel.localRotation = _barrelRotation;
			_cannonBase.localRotation = _baseRotation;
		}


		public void OnInteractFixedNetworkUpdate(PlayerInput input)
		{
			base.FixedUpdateNetwork();
			//if (!GetInput<PlayerInput>(out var input)) return;

			// キャノンの回転処理
			CannonRotate(input.MoveDirection.x, input.LookDirection.y);
			_currentUsePlayer?.SetWarpTarget(_waitCharacterTransform.position, _waitCharacterTransform.rotation);

			// 射撃処理
			if (input.Buttons.IsSet(PlayerButtons.Attack) && LastFireTime.ExpiredOrNotRunning(Runner) &&
			    _currentAmmo > 0)
			{
				RPC_Fire();
				LastFireTime = TickTimer.CreateFromSeconds(Runner, _reloadTime);
				_currentAmmo -= 1;
				if (_currentAmmo <= 0) Invoke(nameof(OnInteractEnd), .1f);
			}
		}

		#endregion
		
		/// <summary>
		///     インタラクト開始時の初期化処理
		/// </summary>
		public void OnInteractStart(PlayerRef playerRef)
		{
			IsActive = true;
			Object.AssignInputAuthority(playerRef);
			_currentAmmo = _maxAmmo;

			// プレイヤーの取得
			_currentUsePlayer = Runner.GetPlayerObject(playerRef).GetComponent<PlayerManager>();
			if (_currentUsePlayer == null) return;
			RPC_PlayerUnActive();
			_currentUsePlayer.SetWarpTarget(_waitCharacterTransform.position, _waitCharacterTransform.rotation);
		}

		/// <summary>
		///     インタラクト終了時の処理
		/// </summary>
		public void OnInteractEnd()
		{
			IsActive = false;
			Refresh();
			if (_currentUsePlayer == null) return;
			Object.RemoveInputAuthority();
			PlayerActive(true);
			_currentUsePlayer = null;

			IsActive = false;
		}


		private void Refresh()
		{
			_lineRenderer.positionCount = 0;
			_cannonBarrel.rotation = _barrelDefaultRotation;
			_cannonBase.rotation = _baseDefaultRotation;
		}

		private void CreateLine()
		{
			if (!IsActive) return;
			BuildTrajectory(); // 描画位置の計算
			_lineRenderer.positionCount = _lastPositionIndex + 1;
			_lineRenderer.SetPositions(_linePositions);

			var endPos = _linePositions[_lastPositionIndex];
			_aimHitViewObject.position = endPos;
			_aimHitViewObject.gameObject.SetActive(true);
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
					_lastPositionIndex = i;
					return;
				}
			}

			_lastPositionIndex = _linePositions.Length - 1;
		}

		private void PlayerActive(bool isActive)
		{
			if (Runner.IsServer)
			{
				_currentUsePlayer.RPC_SetControlState(isActive
					? PlayerManager.PlayerControlState.Normal
					: PlayerManager.PlayerControlState.ForcedControl);
				_currentUsePlayer.RPC_SetUseGrav(isActive);
			}
		}

		[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
		private void RPC_PlayerUnActive()
		{
			PlayerActive(false);
		}

		[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
		private void RPC_Fire()
		{
			_ = Fire();
		}

		private async UniTask Fire()
		{
			if (!Runner.IsServer) return;
			var timer = 0f;

			// 発射向きなどの情報を保持しておく
			var muzzlePosition = _muzzle.position;
			var muzzleForward = _muzzle.transform.forward;
			var ballSpeed = _ballSpeed;
			var endPos = _linePositions[_lastPositionIndex];
			var endTime = _simulationStepTime * _lastPositionIndex;
			
			RPC_ParticleRenderActive(_fireParticle, true);

			// 弾丸の位置をUpdateする
			while (timer < endTime)
			{
				timer += Runner.DeltaTime;
				var pos = TrajectoryCalculator(muzzlePosition,
					muzzleForward * ballSpeed, 9.8f, timer);
				//弾の位置更新
				_fireParticle.transform.position = pos;

				// シミュレーション終了条件
				if ((endPos - pos).magnitude < 0.1f) // TODO:計算負荷
					break;

				await UniTask.WaitForSeconds(Runner.DeltaTime);
			}

			RPC_ParticleRenderActive(_fireParticle, false);

			// 着弾時のエフェクト
			var explosionEffect = Runner.Spawn(_explosionParticlePrefab, endPos, Quaternion.identity);
			Destroy(explosionEffect.gameObject, 3f);
			Runner.Despawn(explosionEffect);

			var cols = Physics.OverlapSphere(endPos, _radius, _playerHitMask); // TODO:当たり判定統一するかも
			// ダメージ処理
			foreach (var col in cols)
			{
				Debug.Log("Hit: " + col.name);
				var damageable = col.GetComponentInParent<IDamageable>();
				if (damageable == null) continue;
				if (damageable.OwnerPlayerRef == _ownerPlayerRef) continue;
				var hitData = new HitData(HitActionType.Damage, _damage, _ownerPlayerRef, damageable.OwnerPlayerRef);
				damageable.TakeHit(ref hitData);
			}
		}

		private void CannonRotate(float baseRotateInput, float barrelRotateInput)
		{
			// 土台の回転
			var currentBaseAxis = _cannonBase.localEulerAngles.y +
			                      _baseRotateSpeed * baseRotateInput;
			currentBaseAxis = WarpAngle(currentBaseAxis);
			_baseRotation = _baseDefaultRotation * Quaternion.Euler(0,
				Mathf.Clamp(currentBaseAxis, _angleLimitY.x, _angleLimitY.y), 0);

			// 砲身の回転
			var currentBarrelAxis = _cannonBarrel.localEulerAngles.x +
			                        _barrelRotateSpeed * barrelRotateInput;
			currentBarrelAxis = WarpAngle(currentBarrelAxis);
			_barrelRotation = _cannonBase.localRotation * Quaternion.Euler(
				Mathf.Clamp(currentBarrelAxis, _angleLimitX.x, _angleLimitX.y), 0, 0);
		}
		
		[Rpc(RpcSources.All, RpcTargets.All)]
		void RPC_ParticleRenderActive(NetworkObject obj, bool isActive)
		{
			obj.gameObject.SetActive(isActive);
			// var particle = obj.GetComponentInChildren<ParticleSystem>();
			// if (IsActive)
			// {
			// 	particle.Play();
			// }
			// else
			// {
			// 	particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			// }
		}

		#region Math Helper

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
	}
}