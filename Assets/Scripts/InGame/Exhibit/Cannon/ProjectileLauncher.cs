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
		[SerializeField] private Transform _projectileSpawnPoint;
		[SerializeField] private Projectile _projectilePrefab;
		[SerializeField] private NetworkObject _projectileEffectPrefab;
		[SerializeField] private LineRenderer _lineRenderer;
		[SerializeField] private float _simulationStepTime = 0.1f;
		[SerializeField] private float _lifeTime = 10f;
		[SerializeField] private Vector3 _gravity = new(0, -9.81f, 0);
		[SerializeField] private float _projectileVelocity;
		[SerializeField] private LayerMask _hitLayer;
		[SerializeField] private int _baseDamage;

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
			_projectileHitEffect.Initialize();
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
		public void Fire(PlayerRef usePlayerRef)
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
			
			Runner.Spawn(_projectilePrefab, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation,
				onBeforeSpawned: (runner, obj) =>
				{
					var projectile = obj.GetComponent<Projectile>();

					RPC_InitializedProjectile(projectile);

					projectile.Fire(CurrentProjectileData, usePlayerRef, (position, rotation, hitObject) =>
					{
						var normal = rotation * Vector3.forward;
						if (Runner.IsServer)
							_projectileHitEffect.Hit(position, normal, hitObject,
								usePlayerRef);
						RPC_PlayEffect(position, normal);
					});
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
			projectile.Initialized(_projectileEffectPrefab.gameObject, _hitLayer);
		}

		[Rpc]
		private void RPC_PlayEffect(Vector3 position, Vector3 normal)
		{
			_projectileHitEffect.PlayEffect(position, normal);
		}

		#region Gizmos

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if (!Object || !Object.IsValid) return;
			// 当たり判定の可視化
			_projectileHitEffect.DrawGizmos(HitPosition, HitNormal);
		}
#endif

		#endregion
	}

	public interface IProjectileHitEffect
	{
		void Initialize();
		
		/// <summary>
		///     ProjectileHit時にサーバで上のゲームロジック処理
		/// </summary>
		void Hit(Vector3 hitPos, Vector3 normal, GameObject hitObject, PlayerRef usePlayer);

		/// <summary>
		///     ProjectileHit時に全クライアントで行う処理
		/// </summary>
		void PlayEffect(Vector3 hitPos, Vector3 normal);

		void DrawGizmos(Vector3 hitPos, Vector3 normal);
	}
}