using System;
using System.Collections.Generic;
using Cinemachine;
using Fusion;
using InGame.Health;
using InGame.Interact;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace September.InGame.Exhibit
{
	// 他のNetworkBehaviourより遅く実行する
	public class ProjectileInteractableBase : NetworkBehaviour
	{
		[SerializeField] protected CinemachineVirtualCamera _cameraController;
		[SerializeField] protected InteractableBase _interactable;
		[SerializeField] protected Animator _animator;
		[SerializeField] protected NetworkMecanimAnimator _networkAnimator;

		[Header("reload設定")] [SerializeReference] [SubclassSelector]
		private IFireController _fireBulletController;

		[Header("レティクル設定")] [SerializeReference] [SubclassSelector]
		private IReticuleEffect _reticuleEffect;

		protected ProjectileLauncher _launcher;
		protected IProjectileMovement _move;
		protected PlayerManager _usingPlayer;

		[Networked] private NetworkButtons _attackButton { get; set; }
		[Networked] protected PlayerRef CurrentUsePlayerRef { get; set; }
		[Networked] protected TickTimer LastFireTimer { get; set; }
		[Networked] protected TickTimer WaitExitTimer { get; set; }
		[Networked] private TickTimer InteractEndLockTimer { get; set; }

		public override void Spawned()
		{
			base.Spawned();
			_launcher = GetComponent<ProjectileLauncher>();
			_move = GetComponent<IProjectileMovement>();
			_reticuleEffect?.Init();
		}

		public override void Render()
		{
			base.Render();
			_reticuleEffect?.Render();
		}

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();
			if (CurrentUsePlayerRef.IsNone) return;

			if (!GetInput(out PlayerInput input))
				return;
			_move.Update(input);

			// 射撃処理
			if (input.Buttons.WasPressed(_attackButton, PlayerButtons.Attack) &&
			    LastFireTimer.ExpiredOrNotRunning(Runner))
			{
				Fire();
				if (Runner.IsForward) PlayFireAnimation();
			}

			_attackButton = input.Buttons;

			CheckInteractEnd(input);
		}

		/// <summary>
		///     Hostのみで実行されるインタラクト開始時の初期化処理
		/// </summary>
		public virtual void InteractStart(PlayerRef playerRef)
		{
			// プレイヤーの取得
			CurrentUsePlayerRef = playerRef;
			_usingPlayer = Runner.GetPlayerObject(CurrentUsePlayerRef).GetComponent<PlayerManager>();

			// インタラクトの機能を一時的に無効化する
			_interactable.ForceSetInteractable = false;

			RPC_SetCameraPriority(CurrentUsePlayerRef, 15);
			RPC_StartAnimation(true);

			Object.AssignInputAuthority(CurrentUsePlayerRef);

			// 使用中のプレイヤーに対する処理
			if (!_usingPlayer) return;
			PlayerActive(false);
			_move.InitializeStateAuthority(_usingPlayer.Object, playerRef);
			_fireBulletController.Init();

			// Playerがダメージを受けた際にInteractを終了する
			_usingPlayer.GetComponent<PlayerHealth>().OnHitTaken += PlayerHitTaken;

			// インタラクトして1秒後からインタラクト解除可能にする
			InteractEndLockTimer = TickTimer.CreateFromSeconds(Runner, 1f);
			
			// 全てのクライアントで必要な初期化処理を行う
			RPC_AllClientInit(CurrentUsePlayerRef, true);
		}

		protected virtual void Fire()
		{
			if (!HasStateAuthority) return;
			// 発射処理はHostが扱う
			_launcher.Fire(CurrentUsePlayerRef);

			_fireBulletController.Fire();

			if (!_fireBulletController.IsUsable())
			{
				var timer = TickTimer.CreateFromSeconds(Runner, 1f);
				WaitExitTimer = timer;
				LastFireTimer = timer;
			}
			else
			{
				LastFireTimer = _fireBulletController.GetNextFireTimer(Runner);
			}
		}

		protected virtual void CheckInteractEnd(PlayerInput input)
		{
			if (!HasStateAuthority) return;

			if (input.Buttons.IsSet(PlayerButtons.Interact) && InteractEndLockTimer.ExpiredOrNotRunning(Runner))
				InteractEnd();

			if (WaitExitTimer.Expired(Runner))
			{
				WaitExitTimer = TickTimer.None;
				InteractEnd();
			}
		}

		/// <summary>
		///     インタラクト終了時の処理(Hostのみ)
		/// </summary>
		public void InteractEnd()
		{
			SetCooldown();
			_move.Reset();
			RPC_StartAnimation(false);
			Object.RemoveInputAuthority();
			RPC_SetCameraPriority(CurrentUsePlayerRef, 5);

			// 使用中のプレイヤークライアントに対しての
			if (!_usingPlayer) return;
			PlayerActive(true);
			RPC_AllClientInit(CurrentUsePlayerRef, false);
			_usingPlayer.GetComponent<PlayerHealth>().OnHitTaken -= PlayerHitTaken;

			_usingPlayer = null;
			CurrentUsePlayerRef = default;
			_interactable.EndInteract();
		}

		private void PlayerActive(bool isActive)
		{
			if (Runner.IsServer)
			{
				RPC_SetCameraPriority(CurrentUsePlayerRef, isActive ? 0 : 15);
				_usingPlayer.RPC_SetControlState(isActive
					? PlayerManager.PlayerControlState.Normal
					: PlayerManager.PlayerControlState.ForcedControl);
				_usingPlayer.RPC_SetUseGrav(isActive);
			}
		}

		private void SetCooldown()
		{
			// クールダウン処理
			var chara = PlayerDatabase.Instance.PlayerDataDic[CurrentUsePlayerRef].CharacterType;
			var time = _interactable.CooldownTimeDictionary.Dictionary.TryGetValue(CharacterType.All, out var all)
				? all
				: _interactable.CooldownTimeDictionary.Dictionary.GetValueOrDefault(chara, 0f);
			_interactable.SetCooldown(time);
			_interactable.ForceSetInteractable = true;
		}

		[Rpc]
		private void RPC_AllClientInit(PlayerRef currentPlayer, bool isActive)
		{
			EffectActive(currentPlayer, isActive);
			_move.Initialize();
		}

		protected virtual void EffectActive(PlayerRef currentPlayer, bool isActive)
		{
			_reticuleEffect.AllClientEffectActive(isActive);
			if (Runner.LocalPlayer == currentPlayer) _reticuleEffect?.SetActive(isActive);
		}

		private void PlayerHitTaken(HitData hitData)
		{
			InteractEnd();
		}

		[Rpc]
		private void RPC_StartAnimation(bool isActive)
		{
			if(!_animator) return;
			_animator.SetBool("IsStart", isActive);
		}

		private void PlayFireAnimation()
		{
			if(!_networkAnimator) return;
			_networkAnimator?.SetTrigger("Fire", true);
		}

		#region Helper

		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_SetCameraPriority(PlayerRef playerRef, int priority)
		{
			if (Runner.LocalPlayer != playerRef) return;
			_cameraController.Priority = priority;
			_cameraController.MoveToTopOfPrioritySubqueue();
		}

		#endregion
	}

	public interface IProjectileMovement
	{
		public void InitializeStateAuthority(NetworkObject playerObject, PlayerRef playerRef);
		public void Initialize();
		public void Update(PlayerInput input);
		public void Reset();
	}

	public interface IFireController
	{
		void Init();
		void Fire();
		bool IsUsable();
		TickTimer GetNextFireTimer(NetworkRunner runner);
	}

	public interface IReticuleEffect
	{
		void Init();
		void Render();
		void SetActive(bool active);
		void AllClientEffectActive(bool active);
	}

	[Serializable]
	public class UseReload : IFireController
	{
		[SerializeField] private int _maxAmmo;
		[SerializeField] private float _fireRate;
		[SerializeField] private float _reloadTime;
		private int _currentAmmo;
		private bool _isReloading;


		public void Init()
		{
			_currentAmmo = _maxAmmo;
			_isReloading = false;
		}

		public void Fire()
		{
			_currentAmmo--;
			if (_currentAmmo == 0) _isReloading = true;
		}

		public bool IsUsable()
		{
			return true;
		}

		public TickTimer GetNextFireTimer(NetworkRunner runner)
		{
			if (_isReloading)
			{
				Init();
				return TickTimer.CreateFromSeconds(runner, _reloadTime);
			}

			return TickTimer.CreateFromSeconds(runner, _fireRate);
		}
	}

	[Serializable]
	public class NoReload : IFireController
	{
		[SerializeField] private int _maxAmmo;
		[SerializeField] private float _fireRate;
		private int _currentAmmo;

		public void Init()
		{
			_currentAmmo = _maxAmmo;
		}

		public void Fire()
		{
			_currentAmmo--;
		}

		public bool IsUsable()
		{
			return 0 < _currentAmmo;
		}

		public TickTimer GetNextFireTimer(NetworkRunner runner)
		{
			return TickTimer.CreateFromSeconds(runner, _fireRate);
		}
	}
}