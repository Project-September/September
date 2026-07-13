using System;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using UniRx.Toolkit;
using UnityEngine;
using Object = UnityEngine.Object;

namespace September.InGame.Exhibit
{
	public class ProjectileLauncher : NetworkBehaviour
	{
		[SerializeField] private int _baseDamage;
		[SerializeField] private Transform _projectileSpawnPoint;
		[SerializeField] private Projectile _projectilePrefab;
		[SerializeField] private NetworkObject _projectileEffectPrefab;
		[SerializeField] private LineRenderer _lineRenderer;
		[SerializeField] private float _simulationStepTime = 0.1f;
		[SerializeField] private float _lifeTime = 10f;
		[SerializeField] private Vector3 _gravity = new(0, -9.81f, 0);
		[SerializeField] private float _projectileVelocity;
		[SerializeField] private LayerMask _hitLayer;

		[Header("Hit時の処理")] [SerializeReference] [SubclassSelector]
		private IProjectileHitEffect _projectileHitEffect;

		public bool IsRenderLine;
		private Tick _lastTrajectoryUpdateTick;
		private Vector3[] _linePositions;

		public Vector3 HitPosition
		{
			get
			{
				BuildTrajectory();
				return _linePositions[LastPositionIndex];
			}
		}

		public Vector3 HitNormal { get; private set; }
		[Networked] private int LastPositionIndex { get; set; }
		[Networked] private PlayerRef _playerRef { get; set; }
		[Networked] private ProjectileData CurrentProjectileData { get; set; }

		/// <summary>
		///     投射物が着弾した際のevent。引数は着弾位置と着弾時の回転。
		/// </summary>
		public event Action<Vector3, Quaternion> OnHit;

		public struct ProjectileData : INetworkStruct
		{
			public Vector3 StartPosition;
			public Vector3 InitialVelocity;
			public Vector3 CurrentPosition;
			public Vector3 CurrentForward;
			public Vector3 Gravity;
			public float Timer;
			public NetworkBool HasHit;
		}

		public override void Spawned()
		{
			base.Spawned();
			_linePositions = new Vector3[(int)(_lifeTime / _simulationStepTime)];
		}

		public override void Render()
		{
			if (IsRenderLine) RenderLine();
			else RefreshLineRenderer();
		}

		/// <summary>
		///     投射物を発射する
		/// </summary>
		public void Fire()
		{
			CurrentProjectileData = new ProjectileData
			{
				StartPosition = _projectileSpawnPoint.position,
				InitialVelocity = _projectileSpawnPoint.forward * _projectileVelocity,
				CurrentPosition = _projectileSpawnPoint.position,
				Gravity = _gravity,
				Timer = 0f,
				HasHit = false
			};

			// 投射物の管理は個々のPrefabに任せる。Hit時のコールバックで処理。
			Runner.Spawn(_projectilePrefab, onBeforeSpawned: (runner, obj) =>
			{
				var projectile = obj.GetComponent<Projectile>();
				RPC_InitializedProjectile(projectile);
				_projectileHitEffect.Hit(projectile.transform.position, projectile.transform.rotation, obj.gameObject,
					_playerRef);
				RPC_Fire(projectile);
			});
		}

		/// <summary>
		///     事前に計算された軌道に沿って線描画する
		/// </summary>
		private void RenderLine()
		{
			BuildTrajectory();
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
			// 同Tick内で更新済みなら更新しない
			if (Runner.Tick == _lastTrajectoryUpdateTick) return;
			_lastTrajectoryUpdateTick = Runner.Tick;

			for (var i = 0; i < _linePositions.Length; i++)
			{
				// 次の地点の計算
				var time = i * _simulationStepTime;
				var pos = Projectile.CalculateTrajectoryPosition(_projectileSpawnPoint.position,
					_projectileSpawnPoint.transform.forward * _projectileVelocity, _gravity, time);
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

		private void RefreshLineRenderer()
		{
			_lineRenderer.positionCount = 0;
		}

		[Rpc]
		private void RPC_InitializedProjectile(Projectile projectile)
		{
			projectile.Initialized(_projectileEffectPrefab.gameObject, _projectileHitEffect, _hitLayer);
		}

		[Rpc]
		private void RPC_Fire(Projectile projectile)
		{
			projectile.Fire(CurrentProjectileData, _playerRef, (position, rotation, hitObject) =>
			{
				RPC_PlayHitEffect(position, rotation);
				OnHit?.Invoke(position, rotation);
			});
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_PlayHitEffect(Vector3 point, Quaternion rotation)
		{
			_projectileHitEffect.PlayHitEffect(point, rotation);
		}
	}

	public interface IProjectileHitEffect
	{
		void Hit(Vector3 position, Quaternion rotation, GameObject hitObject, PlayerRef usePlayer);
		void PlayHitEffect(Vector3 position, Quaternion rotation);
	}
}