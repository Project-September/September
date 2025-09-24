using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
using InGame.Common;
using InGame.Health;
using September.Common;
using UnityEngine;

namespace InGame.Player.Sarutobi
{
    public class ThrowKunai : NetworkBehaviour, IAfterTick
    {
        [SerializeField] private float _cooldown;
        [SerializeField] private int _damage;
        [SerializeField] private int _ogreDamage;
        [SerializeField] private float _maxDistance;
        [SerializeField] private LayerMask _hitLayer;
        [SerializeField] private Transform _muzzleTf;
        [SerializeField] private Vector3 _stanceCameraOffset;
        [SerializeField] private float _changeOffsetDuration;
        [Header("AnimationClip")]
        [SerializeField] private AnimationClip _stance;
        [SerializeField] private AnimationClip _stanceLoop;
        [SerializeField] private AnimationClip _throw;
        [Header("Particle")]
        [SerializeField] private ParticleSystem _kunaiParticle;
        [SerializeField] private float _kunaiSpeed;
        [SerializeField] private ParticleSystem _bulletMark;
        [Header("UI")]
        [SerializeField] private GameObject _crosshairPrefab;

        private Camera _mainCamera;
        private PlayerManager _playerManager;
        private AnimationClipPlayer _clipPlayer;
        private PlayerMovement _movement;
        private CameraController _cameraController;
        private AbilityGrapplingHook _grapplingHook;
        private GameObject _crosshair;
        private float _cooldownTimer;
        private NetworkButtons PreviousButtons { get; set; }

        private bool CanThrow => _cooldownTimer <= 0 && State == KunaiStateType.Ready && _grapplingHook &&
                                 _grapplingHook.AbilityState != AbilityGrapplingHook.AbilityStateType.Active;

        [Networked, HideInInspector] public KunaiStateType State { get; private set; } = KunaiStateType.Idol;

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                _playerManager = GetComponent<PlayerManager>();
                _clipPlayer = GetComponent<AnimationClipPlayer>();
                _movement = GetComponent<PlayerMovement>();
                _grapplingHook = GetComponent<AbilityGrapplingHook>();
                _grapplingHook.OnAbilityStart += EndStance;
            }

            if (HasInputAuthority)
            {
                _mainCamera = Camera.main;
                _cameraController = GetComponent<CameraController>();
                if (!_grapplingHook) _grapplingHook = GetComponent<AbilityGrapplingHook>();
                _crosshair = Instantiate(_crosshairPrefab);
                _crosshair.SetActive(false);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (HasStateAuthority && _grapplingHook)
            {
                _grapplingHook.OnAbilityStart -= EndStance;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput<PlayerInput>(out var input)) return;
            
            if (State == KunaiStateType.Idol && input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Ability2)
                                             && _grapplingHook && _grapplingHook.AbilityState != AbilityGrapplingHook.AbilityStateType.Active)
            {
                StartStance().Forget();
            }
            if (input.Buttons.WasReleased(PreviousButtons, PlayerButtons.Ability2))
            {
                EndStance();
            }
            else if (input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Attack) && CanThrow)
            {
                Throw().Forget();
            }

            if (State == KunaiStateType.Ready || State == KunaiStateType.Stance)
            {
                _movement.SetRotationDirection(HasInputAuthority ? _mainCamera.transform.forward : input.DesiredLookDirection);
            }
            
            SetCooldownTimer();
        }

        public void AfterTick()
        {
            PreviousButtons = GetInput<PlayerInput>().GetValueOrDefault().Buttons;
        }

        async UniTask StartStance()
        {
            if (HasInputAuthority)
            {
                _cameraController.ChangeOffset(_stanceCameraOffset, _changeOffsetDuration);
                _crosshair.SetActive(true);
            }
            if (!HasStateAuthority) return;

            State = KunaiStateType.Stance;
            _playerManager.SetControlState(PlayerManager.PlayerControlState.InputLocked);
            var endType = await _clipPlayer.PlayClipAndWait(_stance, this.GetCancellationTokenOnDestroy());

            if (endType == EndClipType.Complete)
            {
                State = KunaiStateType.Ready;
                _clipPlayer.PlayClip(_stanceLoop);
            }
            else
            {
                EndStance();
            }
        }

        async UniTask Throw()
        {
            if (HasInputAuthority)
            {
                KunaiTargetDetection();
            }

            if (!HasStateAuthority) return;
            State = KunaiStateType.Stance;
            var endType = await _clipPlayer.PlayClipAndWait(_throw, this.GetCancellationTokenOnDestroy());

            if (endType == EndClipType.Complete)
            {
                StartStance().Forget();
            }
        }

        void EndStance()
        {
            if (HasInputAuthority)
            {
                _cameraController.ResetOffset(_changeOffsetDuration);
                _crosshair.SetActive(false);
            }
            else
            {
                Rpc_EndStance();
            }
            
            _playerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
            State = KunaiStateType.Idol;
            
            if (!_clipPlayer.StopClip(_stanceLoop))
            {
                if (!_clipPlayer.StopClip(_stance))
                {
                    _clipPlayer.StopClip(_throw);
                }
            }
        }

        // state authority から強制終了したときにLocalに反映する
        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        void Rpc_EndStance()
        {
            _cameraController.ResetOffset(_changeOffsetDuration);
            _crosshair.SetActive(false);
        }

        // Local で投げる位置の判定をとる
        void KunaiTargetDetection()
        {
            if (!HasInputAuthority) return;
            var hit = Physics.Raycast(_mainCamera.transform.position, _mainCamera.transform.forward, out var hitInfo, _maxDistance, _hitLayer);
            Rpc_KunaiBulletDetection(hit ? hitInfo.point : _mainCamera.transform.position + _mainCamera.transform.forward * _maxDistance);
        }
        
        /// <summary> input authority から target position を取得し、クナイの判定をとる </summary>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        void Rpc_KunaiBulletDetection(Vector3 targetPos)
        {
            var muzzleHit = Physics.Raycast(_muzzleTf.position, targetPos - _muzzleTf.position, out var muzzleHitInfo, _maxDistance, _hitLayer);

            if (muzzleHit)
            {
                var damageable = muzzleHitInfo.collider.GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    // ダメージ処理
                    bool enableData = PlayerDatabase.Instance.PlayerDataDic.TryGet(Object.InputAuthority, out var sessionData);
                    var hitData = new HitData(HitActionType.Damage,
                        enableData ? sessionData.IsOgre ? _ogreDamage : _damage : _damage, Object.InputAuthority,
                        damageable.OwnerPlayerRef, null, damageable);
                    damageable.TakeHit(ref hitData);
                }
            }
            
            // 各クライアントでParticleの再生
            RPC_PlayKunaiThrowParticle(muzzleHit ? muzzleHitInfo.point : targetPos,
                muzzleHit ? muzzleHitInfo.distance : Vector3.Distance(_muzzleTf.transform.position, targetPos),
                muzzleHitInfo.normal, muzzleHit);
        }

        [Rpc]
        void RPC_PlayKunaiThrowParticle(Vector3 hitPos, float hitDistance, Vector3 hitNormal, bool isHit)
        {
            float duration = hitDistance / _kunaiSpeed;
            ParticlePool.Play(_kunaiParticle, _muzzleTf.position, Quaternion.LookRotation(hitPos - _muzzleTf.position), duration + 2f)
                .transform.DOMove(hitPos, duration);
            if (isHit) ParticlePool.Play(_bulletMark, hitPos, Quaternion.LookRotation(hitNormal));
        }

        void SetCooldownTimer()
        {
            if (_cooldownTimer > 0) _cooldownTimer -= Runner.DeltaTime;
        }

        public enum KunaiStateType
        {
            Idol,
            Stance,
            Ready
        }
    }
}
