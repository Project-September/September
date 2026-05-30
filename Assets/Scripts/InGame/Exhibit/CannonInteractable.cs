using System;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Exhibit;
using InGame.Health;
using InGame.Player;
using NaughtyAttributes;
using September.Common;
using September.InGame.Common;
using UnityEngine;

namespace InGame.Exhibit
{
	public class CannonInteractable : NetworkBehaviour
	{
		[SerializeField] private Transform _muzzle;
		[Header("移動関連")] [SerializeField] 
		private Transform _cannonBase;
		[SerializeField] private Transform _cannonBarrel;
		[SerializeField] private float _baseRotateSpeed;
		[SerializeField] private float _barrelRotateSpeed;
		[SerializeField] private float _maxXAngle;
		[SerializeField] private float _minXAngle;
		[SerializeField] private float _maxYAngle;
		[SerializeField] private float _minYAngle;

		[Header("描画")] [SerializeField] private float _simulationStepTime = 0.1f;
		[SerializeField] private float _simulationEndTime = 5f;
		[SerializeField] private Vector3 _gravity;
		[SerializeField] private float _ballSpeed;

		[SerializeField] private Transform _aimHitViewObject;
		[SerializeField] private LineRenderer _lineRenderer;

		[Header("連射")] [SerializeField] private int _maxAmmo;
		[SerializeField] private float _reloadTime;

		[Header("PlayerCharacter")] [SerializeField]
		private Transform _waitCharacterTransform;

		private PlayerManager _currentUsePlayer;
		private Quaternion _baseDefaultRotation;
		private Quaternion _barrelDefaultRotation;
		
		[Networked] public bool IsActive { get; private set; }
		private int _currentAmmo;
		
		// TODO:変数適当
		[SerializeField] private LayerMask _playerHitMask;
		[SerializeField] private float _radius;
		[SerializeField] private GameObject _fireParticle;
		[SerializeField] private GameObject _explosionParticle;
		public int _damage = 10;
		public PlayerRef _ownerPlayerRef;
		[Networked] private TickTimer LastFireTime { get; set; }
		[Networked, Capacity(32)] private NetworkArray<Vector3> _linePositions => default;

		public override void Spawned()
		{
			base.Spawned();
			_baseDefaultRotation = _cannonBase.localRotation;
			_barrelDefaultRotation = _cannonBarrel.localRotation;
		}

		public override void Render()
		{
			base.Render();
			CreateLine();
		}

		private Vector3 CreateLine()
		{
			var endPos = Vector3.zero;
			if (!IsActive) return endPos;
			var loopCount = (int)(_simulationEndTime / _simulationStepTime);
			_lineRenderer.positionCount = loopCount;
			for (var i = 0; i < _linePositions.Length; i++)
			{
				// 次の地点の計算
				var time = i * _simulationStepTime;
				var pos = TrajectoryCalculator(_muzzle.position,
					_muzzle.transform.forward * _ballSpeed, 9.8f, time);
				_linePositions.Set(i, pos);

				if (i == 0) continue;
				// 当たり判定の取得
				var ray = new Ray(_linePositions[i - 1], _linePositions[i] - _linePositions[i - 1]);
				if (Physics.Raycast(ray, out var hit, Vector3.Distance(_linePositions[i - 1], _linePositions[i])))
				{
					_aimHitViewObject.position = hit.point;
					_aimHitViewObject.gameObject.SetActive(true);
					_lineRenderer.positionCount = i;
					_linePositions.Set(i, hit.point);
					endPos = _linePositions[i];
					break;
				}
			}

			_lineRenderer.SetPositions(_linePositions.ToArray());
			return endPos;
		}

		public void OnInteractStart(PlayerRef playerRef)
		{
			Debug.Log("初期化");
			IsActive = true;
			Object.AssignInputAuthority(playerRef);
			_currentAmmo = _maxAmmo;
			
			// プレイヤーの
			_currentUsePlayer = Runner.GetPlayerObject(playerRef).GetComponent<PlayerManager>();
			Debug.Log(_currentUsePlayer);
			if(_currentUsePlayer == null) return;
			PlayerActive(false);
			_currentUsePlayer.SetWarpTarget(_waitCharacterTransform.position, _waitCharacterTransform.rotation);
		}

		public void OnInteractEnd()
		{
			Debug.Log("大砲終了");
			Object.RemoveInputAuthority();
			PlayerActive(true);
			_currentUsePlayer = null;
			IsActive = false;
			_cannonBarrel.rotation = _barrelDefaultRotation;
			_cannonBase.rotation = _baseDefaultRotation;
		}

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();
			if(!GetInput<PlayerInput>(out var input))return;
			OnInteractFixedNetworkUpdate(input);
		}

		public void OnInteractFixedNetworkUpdate(PlayerInput input)
		{
			CreateLine();
			CannonRotate(input.MoveDirection.x, input.LookDirection.y);

			if (input.Buttons.IsSet(PlayerButtons.Attack) && LastFireTime.ExpiredOrNotRunning(Runner))
			{
				Debug.Log("Fire");
				Fire();
				LastFireTime = TickTimer.CreateFromSeconds(Runner, _reloadTime);
				_currentAmmo -= 1;
				if (_currentAmmo <= 0)
				{
					//終了処理
					Debug.Log("終了");
					IsActive = false;
				}
			}
		}

		private async UniTask Fire()
		{
			if (!Runner.IsServer) return;
			var timer = 0f;
			var effect = Runner.Spawn(_fireParticle, _muzzle.position, _muzzle.rotation);
			var endPos = CreateLine();
			while (timer < _simulationEndTime)
			{
				timer += Runner.DeltaTime;
				var pos = TrajectoryCalculator(_muzzle.position,
					_muzzle.transform.forward * _ballSpeed, 9.8f, timer);
				//弾の描画処理
				effect.transform.position = pos;

				if ((endPos - pos).magnitude < 0.1f) // TODO:計算負荷
				{
					Debug.Log("hit");
					break;
				}
				
				await UniTask.WaitForSeconds(Runner.DeltaTime);
			}
			Destroy(effect.gameObject);
			Runner.Despawn(effect);
			
			// 着弾時のエフェクトやダメージ処理など
			var explosionEffect = Runner.Spawn(_explosionParticle, endPos, Quaternion.identity);
			Destroy(explosionEffect.gameObject, 3f);
			Runner.Despawn(explosionEffect);
			
			var cols = Physics.OverlapSphere(endPos, _radius, _playerHitMask);
			foreach (var col in cols)
			{
				var damageable = col.GetComponentInParent<IDamageable>();
				if (damageable == null) continue;
				if(damageable.OwnerPlayerRef == _ownerPlayerRef) continue;
				var hitData = new HitData(HitActionType.Damage, _damage, _ownerPlayerRef, damageable.OwnerPlayerRef);
				damageable.TakeHit(ref hitData);
			}
		}

		private void PlayerActive(bool isActive)
		{
			_currentUsePlayer.SetControlState(isActive
				? PlayerManager.PlayerControlState.Normal
				: PlayerManager.PlayerControlState.ForcedControl);
			_currentUsePlayer.RPC_SetUseGrav(isActive);
		}

		private void CannonRotate(float baseRotateInput, float barrelRotateInput)
		{
			// 土台の回転
			var currentBaseAxis = _cannonBase.localEulerAngles.y +
			                      _baseRotateSpeed * baseRotateInput;
			currentBaseAxis = WarpAngle(currentBaseAxis);
			_cannonBase.localRotation = _baseDefaultRotation * Quaternion.Euler(0,
				Mathf.Clamp(currentBaseAxis, _minXAngle, _maxYAngle), 0);

			// 砲身の回転
			var currentBarrelAxis = _cannonBarrel.localEulerAngles.x +
			                        _barrelRotateSpeed * barrelRotateInput;
			currentBarrelAxis = WarpAngle(currentBarrelAxis);
			_cannonBarrel.localRotation =  _cannonBase.localRotation * Quaternion.Euler(
				                      Mathf.Clamp(currentBarrelAxis, _minXAngle, _maxXAngle), 0, 0);
			Debug.Log(
				$"入力: {baseRotateInput}, {barrelRotateInput} / " +
				$"回転: {_cannonBarrel.localRotation.eulerAngles}, {_cannonBase.localRotation.eulerAngles}");
		}

		private float WarpAngle(float angle)
		{
			if (angle > 180) angle -= 360;
			return angle;
		}


		/// <summary>
		///     特定の時間での放物線位置を計算する
		/// </summary>
		/// <returns>発射後、入力時間の時の位置</returns>
		private Vector3 TrajectoryCalculator(Vector3 startPos, Vector3 power, float gravity, float time)
		{
			return startPos + power * time + Vector3.down * (gravity * time * time);
		}
	}
}