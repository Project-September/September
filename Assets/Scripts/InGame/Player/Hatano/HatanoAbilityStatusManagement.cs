using System.Collections.Generic;
using Fusion;
using Cysharp.Threading.Tasks;
using InGame.Common;
using UnityEngine;
using September.Common;

namespace InGame.Player.Hatano
{
    /// <summary>
    /// ハタノ・エリクソンのAbility選択状態を管理する
    /// </summary>
    public class HatanoAbilityStatusManagement : NetworkBehaviour
    {
        [SerializeField] private AnimationClipPlayer _animClipPlayer;
        [SerializeField] private HatanoChangeAnimationController _changeAnimation;
        [Header("切替アニメーション（D→L）"), SerializeField] private AnimationClip _changeDLClip;
        [Header("切替アニメーション（L→D）"), SerializeField] private AnimationClip _changeLDClip;
        [Header("切替アニメーション（構えD→L）"), SerializeField] private AnimationClip _changeAimDLClip;
        [Header("切替アニメーション（構えL→D）"), SerializeField] private AnimationClip _changeAimLDClip;
        [Header("レーザー銃"), SerializeField] private GameObject _laser;
        [Header("二丁拳銃"), SerializeField] private List<GameObject> _doubles;
        [Header("現在の選択中のAbility")]
        [Networked] private HatanoAbilityStatus _abilityStatus {get; set;}
        public HatanoAbilityStatus AbilityStatus => _abilityStatus;
        public HatanoAbilityStatus _lastAbilityStatus;

        private HatanoAbilityStatusUIManager _abilityStatusUIManager;
        private AimCameraController _aimCameraController;
        private bool _isChangeAbilityInput; //Abilityの変更入力
        private PlayerManager _playerManager;
        private PlayerMovement _playerMovement;
        [Networked] public NetworkBool IsChangingWeapon { get; private set; }

        private void Awake()
        {
            _abilityStatusUIManager = GetComponent<HatanoAbilityStatusUIManager>();
            _aimCameraController = GetComponent<AimCameraController>();
            _playerManager = GetComponent<PlayerManager>();
            _playerMovement = GetComponent<PlayerMovement>();
        }

        public override void Spawned()
        {
            if (HasStateAuthority)
                _abilityStatus = HatanoAbilityStatus.DoubleBarreledGun;
            _lastAbilityStatus = HatanoAbilityStatus.None;
        }

        public override void Render()
        {
            if (_animClipPlayer.IsValid && _abilityStatus != _lastAbilityStatus)
            {
                _lastAbilityStatus = _abilityStatus;

                // UI更新
                _abilityStatusUIManager.SelectedAbilityUITextChanged(_abilityStatus);
                _changeAnimation.ChangeMoveAnimation(_abilityStatus);
            }
        }

        public override void FixedUpdateNetwork()
        {
            // 入力がなかったら処理を行わない
            if (!GetInput<PlayerInput>(out var input)) return;

            // 切替前の射撃 Ability が終了した後も、エイム解除入力は処理する。
            if (HasStateAuthority && IsChangingWeapon && _aimCameraController.IsAim
                && !input.Buttons.IsSet(PlayerButtons.Ability2)
                && _playerManager.CurrentPlayerControlState == PlayerManager.PlayerControlState.Normal)
            {
                _aimCameraController.RPC_NormalCamera();
                _aimCameraController.RPC_CrosshairToggleChange(false);
                _animClipPlayer.SetAim(false);
            }

            if (!HasInputAuthority) return;

            if (input.Buttons.IsSet(PlayerButtons.Ability1) && !_isChangeAbilityInput)
            {
                _isChangeAbilityInput = true;
                if (input.Buttons.IsSet(PlayerButtons.Ultimate) || !CanChangeWeapon()) return;

                // アビリティの変更
                if (HasStateAuthority)
                {
                    ChangeAbilityStatus();
                }
                else
                {
                    RPC_ChangeAbilityStatus();
                }
            }

            if (!input.Buttons.IsSet(PlayerButtons.Ability1) && _isChangeAbilityInput)
            {
                _isChangeAbilityInput = false;
            }
        }

        private bool CanChangeWeapon()
        {
            return _playerManager.CurrentPlayerControlState == PlayerManager.PlayerControlState.Normal
                   && !_playerManager.IsStun && _playerManager.IsMovable && !IsChangingWeapon
                   && !_playerMovement.IsEvading && !_playerMovement.DoingVault;
        }

        private void ChangeAbilityStatus()
        {
            if (!CanChangeWeapon()) return;
            if (GetInput<PlayerInput>(out var input) && input.Buttons.IsSet(PlayerButtons.Ultimate)) return;
            // 状態変更後に旧武器の終了処理が走っても、切替開始時の構えを保持する。
            var wasAiming = _aimCameraController.IsAim
                || (GetInput<PlayerInput>(out var aimInput) && aimInput.Buttons.IsSet(PlayerButtons.Ability2));
            _abilityStatus = GetNextHatanoAbilityStatus();
            ChangeAbility(_abilityStatus, wasAiming).Forget();
        }

        /// <summary>
        /// 現在のアビリティに応じてアビリティの変更を行う
        /// </summary>
        /// <returns>変更後のアビリティ</returns>
        private HatanoAbilityStatus GetNextHatanoAbilityStatus()
        {
            return _abilityStatus switch
            {
                HatanoAbilityStatus.DoubleBarreledGun => HatanoAbilityStatus.LaserGun,
                HatanoAbilityStatus.LaserGun => HatanoAbilityStatus.DoubleBarreledGun
            };
        }

        /// <summary>
        /// アビリティの変更を行う
        /// </summary>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_ChangeAbilityStatus()
        {
            ChangeAbilityStatus();
        }

        /// <summary>
        /// アビリティの変更
        /// </summary>
        /// <param name="status">変更後のアビリティ</param>
        private async UniTask ChangeAbility(HatanoAbilityStatus status, bool wasAiming)
        {
            var clip = status == HatanoAbilityStatus.LaserGun
                ? (wasAiming ? _changeAimDLClip : _changeDLClip)
                : (wasAiming ? _changeAimLDClip : _changeLDClip);
            if (clip == null) return;

            IsChangingWeapon = true;
            _playerManager.SetMovementInputBlocked(true);
            try
            {
                _animClipPlayer.PlayOnUpperBody(null);
                // 確定した装備の切替を StateAuthority から全員へ通知する。
                await _animClipPlayer.PlayClipAndWait(clip);
            }
            finally
            {
                if (Object != null && Object.IsValid && HasStateAuthority)
                {
                    IsChangingWeapon = false;
                    _playerManager.SetMovementInputBlocked(false);
                }
            }
        }
    }
}
