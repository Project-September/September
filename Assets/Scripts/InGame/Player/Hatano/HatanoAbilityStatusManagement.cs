using System.Collections.Generic;
using CRISound;
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
        private HatanoWeaponController _weaponController;
        private bool _isWeaponChangeAimWeightInitialized;
        private bool _isWeaponChangeBlendComplete;
        private bool _shouldReconcileWeaponChangeAimAfterCompletion;
        private float _weaponChangeAimWeight;
        private float _weaponChangeOutputWeight;
        [Networked] public NetworkBool IsChangingWeapon { get; private set; }
        [Networked] private NetworkBool IsWeaponChangeAiming { get; set; }
        [Networked] private NetworkBool IsWeaponChangeBlendingOut { get; set; }

        private void Awake()
        {
            _abilityStatusUIManager = GetComponent<HatanoAbilityStatusUIManager>();
            _aimCameraController = GetComponent<AimCameraController>();
            _playerManager = GetComponent<PlayerManager>();
            _playerMovement = GetComponent<PlayerMovement>();
            _weaponController = GetComponentInChildren<HatanoWeaponController>(true);
        }

        public override void Spawned()
        {
            if (HasStateAuthority)
                _abilityStatus = HatanoAbilityStatus.DoubleBarreledGun;
            _lastAbilityStatus = HatanoAbilityStatus.None;
        }

        public override void Render()
        {
            UpdateWeaponChangeAimWeight();

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

            if (HasStateAuthority && (IsChangingWeapon || _shouldReconcileWeaponChangeAimAfterCompletion))
            {
                var isNormalControl = _playerManager.CurrentPlayerControlState
                    == PlayerManager.PlayerControlState.Normal;
                var isAimHeld = input.Buttons.IsSet(PlayerButtons.Ability2);
                var shouldAimDuringChange = isAimHeld && isNormalControl;
                var wasAimingDuringChange = IsWeaponChangeAiming;
                IsWeaponChangeAiming = shouldAimDuringChange;

                if (wasAimingDuringChange && !shouldAimDuringChange)
                {
                    if (_aimCameraController.IsAim)
                    {
                        _aimCameraController.RPC_NormalCamera();
                        _aimCameraController.RPC_CrosshairToggleChange(false);
                    }
                    _animClipPlayer.SetAim(false);
                    _changeAnimation.StopAimPoseAnimation();
                }

                if (!IsChangingWeapon)
                {
                    _shouldReconcileWeaponChangeAimAfterCompletion = false;
                    IsWeaponChangeAiming = false;
                }
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
            RPC_PlayWeaponChangeSound();
            ChangeAbility(_abilityStatus, wasAiming).Forget();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayWeaponChangeSound()
        {
            if (!CuePlayAtomExPlayer.Instance.IsReady) return;
            var cue = SoundCues.SE.Hatano_Aim;
            CRIAudio.PlaySE(transform.position, cue.Sheet, cue.Name);
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
            var normalClip = status == HatanoAbilityStatus.LaserGun ? _changeDLClip : _changeLDClip;
            var aimClip = status == HatanoAbilityStatus.LaserGun ? _changeAimDLClip : _changeAimLDClip;
            if (normalClip == null || aimClip == null) return;

            var blendEndType = EndClipType.Failed;
            IsChangingWeapon = true;
            IsWeaponChangeAiming = wasAiming;
            IsWeaponChangeBlendingOut = false;
            _isWeaponChangeAimWeightInitialized = false;
            _isWeaponChangeBlendComplete = false;
            _playerManager.SetMovementInputBlocked(true);
            try
            {
                _animClipPlayer.PlayOnUpperBody(null);
                if (!_animClipPlayer.AttachControllableBlend(
                        normalClip,
                        aimClip,
                        LayerInfo.LayerType.UpperBody,
                        wasAiming ? 1f : 0f))
                    return;

                await UniTask.WaitUntil(
                    () => !_animClipPlayer.IsControllableBlendAttached(LayerInfo.LayerType.UpperBody)
                          || _animClipPlayer.IsControllableBlendComplete(LayerInfo.LayerType.UpperBody),
                    cancellationToken: this.GetCancellationTokenOnDestroy());
                blendEndType = _animClipPlayer.IsControllableBlendAttached(LayerInfo.LayerType.UpperBody)
                    ? EndClipType.Complete
                    : EndClipType.Interrupted;
                _isWeaponChangeBlendComplete = blendEndType == EndClipType.Complete;
                _playerManager.SetMovementInputBlocked(false);

                if (blendEndType == EndClipType.Complete && !(bool)IsWeaponChangeAiming)
                {
                    IsWeaponChangeBlendingOut = true;
                    await UniTask.WaitUntil(
                        () => (bool)IsWeaponChangeAiming
                              || !_animClipPlayer.IsControllableBlendAttached(LayerInfo.LayerType.UpperBody)
                              || Mathf.Approximately(_weaponChangeOutputWeight, 0f),
                        cancellationToken: this.GetCancellationTokenOnDestroy());
                }
            }
            finally
            {
                if (Object != null && Object.IsValid && HasStateAuthority)
                {
                    IsWeaponChangeBlendingOut = false;
                    if (blendEndType == EndClipType.Complete && (bool)IsWeaponChangeAiming)
                        CompleteWeaponChangeAnimation();
                    else
                        _animClipPlayer.DetachControllableBlend(LayerInfo.LayerType.UpperBody);
                    if (_weaponController != null)
                        _weaponController.RPC_ApplyAbilityWeapon(_abilityStatus);
                    // Playable の終了は入力 Tick の直前にも発生する。構えを引き継いだ場合は、
                    // 切替完了後の最初の入力 Tick でもう一度照合して同時解除を取りこぼさない。
                    _shouldReconcileWeaponChangeAimAfterCompletion =
                        blendEndType == EndClipType.Complete && (bool)IsWeaponChangeAiming;
                    if (!_shouldReconcileWeaponChangeAimAfterCompletion)
                        IsWeaponChangeAiming = false;
                    IsChangingWeapon = false;
                    _isWeaponChangeBlendComplete = false;
                    _playerManager.SetMovementInputBlocked(false);
                }
            }
        }

        private void UpdateWeaponChangeAimWeight()
        {
            var isBlendAttached = IsChangingWeapon
                && _animClipPlayer.IsControllableBlendAttached(LayerInfo.LayerType.UpperBody);
            if (!isBlendAttached)
            {
                _isWeaponChangeAimWeightInitialized = false;
                return;
            }

            var targetAimWeight = (bool)IsWeaponChangeAiming ? 1f : 0f;
            var targetOutputWeight = (bool)IsWeaponChangeBlendingOut ? 0f : 1f;
            if (!_isWeaponChangeAimWeightInitialized || _animClipPlayer.AimBlendDuration <= 0f)
            {
                _weaponChangeAimWeight = targetAimWeight;
                _weaponChangeOutputWeight = targetOutputWeight;
                _isWeaponChangeAimWeightInitialized = true;
            }
            else
            {
                var maxDelta = Time.deltaTime / _animClipPlayer.AimBlendDuration;
                _weaponChangeAimWeight = Mathf.MoveTowards(
                    _weaponChangeAimWeight,
                    targetAimWeight,
                    maxDelta);
                _weaponChangeOutputWeight = Mathf.MoveTowards(
                    _weaponChangeOutputWeight,
                    targetOutputWeight,
                    maxDelta);
            }

            _animClipPlayer.SetControllableBlendWeights(
                LayerInfo.LayerType.UpperBody,
                _weaponChangeAimWeight,
                _weaponChangeOutputWeight);
        }

        /// <summary>
        /// 武器切替アニメーション終了時に、現在のエイム状態へ上半身モーションを合わせる。
        /// </summary>
        public void CompleteWeaponChangeAnimation()
        {
            if (!HasStateAuthority) return;

            // 片方の終了イベントでは確定せず、取り付けた2クリップの再生完了まで構えへ移行しない。
            if (IsChangingWeapon && !_isWeaponChangeBlendComplete) return;

            var shouldAim = _playerManager.CurrentPlayerControlState == PlayerManager.PlayerControlState.Normal
                && (IsChangingWeapon ? (bool)IsWeaponChangeAiming : _aimCameraController.IsAim);
            if (shouldAim)
            {
                _changeAnimation.ChangeAimPoseAnimation(_abilityStatus);
            }
        }
    }
}
