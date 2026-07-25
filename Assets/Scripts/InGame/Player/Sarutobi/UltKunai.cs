using System;
using System.Collections.Generic;
using DG.Tweening;
using Fusion;
using InGame.Common;
using InGame.Health;
using September.Common;
using September.InGame.Effect;
using Unity.Mathematics;
using UnityEngine;

namespace InGame.Player.Sarutobi
{
    public class UltKunai : NetworkBehaviour
    {
        [SerializeField] private PlayerButtons _throwButton = PlayerButtons.Attack;
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private EffectType _kunaiEffect;
        [SerializeField] private Transform _aimEffect;
        [SerializeField] private Vector3 _aimEffectOffset = new(0f, 0.01f, 0f);
        [SerializeField] private Transform _meshRoot;
        [SerializeField] private PlayerMovement _playerMovement;

        [SerializeField] private AnimationClipPlayer _animationClipPlayer;
        [SerializeField] private AnimationClip _idleClip;

        [SerializeField] private float _hitStartTime;
        [SerializeField] private float _hitEndTime;
        [SerializeField] private float _hitRadius;
        [SerializeField] private LayerMask _hitLayerMask;
        [SerializeField] private LayerMask _groundLayerMask;
        [SerializeField] private int _damage;

        [Networked] private bool IsAiming {get; set;}

        public event Action OnThrow;

        private Vector3 _targetPosition;

        private TickTimer _hitStartTimer;
        private TickTimer _hitEndTimer;

        private readonly Collider[] _hits = new Collider[10];
        private readonly List<Collider> _alreadyHits = new();
        private bool _isHitChecked;

        private const float MeshRotateDuration = 0.3f;

        [Networked] public float MeshRotationRatio { get; set; }

        public void StartEffect()
        {
            _alreadyHits.Clear();
            IsAiming = true;
            _isHitChecked = false;
            RPC_ShowAimTarget();

            _hitStartTimer = default;
            _hitEndTimer = default;

            DOTween.To(() => 0f, v => MeshRotationRatio = v, 1f, MeshRotateDuration);
        }

        private void HitCheck()
        {
            int count = Physics.OverlapSphereNonAlloc(_targetPosition, _hitRadius, _hits, _hitLayerMask);

            for (int i = 0; i < count; i++)
            {
                if (_alreadyHits.Contains(_hits[i])) continue;

                _alreadyHits.Add(_hits[i]);

                IDamageable damageable = _hits[i].GetComponentInParent<IDamageable>();
                if (damageable == null) continue;

                HitData hitData = new(HitActionType.Damage, _damage, Object.InputAuthority, damageable.OwnerPlayerRef);
                damageable.TakeHit(ref hitData);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority)
            {
                if (_hitStartTimer.Expired(Runner) && !_isHitChecked)
                {
                    // 最低一回はヒット検出を行う
                    HitCheck();
                    _isHitChecked = true;
                }
                else if (_hitEndTimer.Expired(Runner) && !_hitStartTimer.Expired(Runner))
                {
                    HitCheck();
                }
            }

            if (GetInput(out PlayerInput input))
            {
                if (MeshRotationRatio > 0f) RotatePlayer(input.DesiredLookDirection);

                if (!IsAiming) return;

                Vector3 pos = GetThrowPos(input.CameraPosition, input.DesiredLookDirection);
                var aimPosition = pos + _aimEffectOffset;
                _aimEffect.transform.position = aimPosition;

                if (!_animationClipPlayer.IsPlayingTargetClip(_idleClip))
                {
                    _animationClipPlayer.PlayClip(_idleClip);
                }

                if (input.Buttons.IsSet(_throwButton))
                {
                    HitboxDebugUtility.DrawWireSphere(pos, _hitRadius, Color.red, 10f);
                    _meshRoot.DOLocalRotate(Vector3.zero, MeshRotateDuration);
                    IsAiming = false;
                    DOTween.To(() => MeshRotationRatio, x => MeshRotationRatio = x, 0f, MeshRotateDuration);
                    if (HasInputAuthority) RPC_Throw(pos);
                }
            }
        }

        private void RotatePlayer(Vector3 desiredLookDirection)
        {
            _playerMovement.SetRotationDirection(desiredLookDirection);
        }

        private Vector3 GetThrowPos(Vector3 cameraPosition, Vector3 lookDirection)
        {
            var ray = new Ray(cameraPosition, lookDirection);
            Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, _groundLayerMask);
            return hit.point;
        }

        [Rpc]
        private void RPC_Throw(Vector3 targetPosition)
        {
            OnThrow?.Invoke();

            _aimEffect.gameObject.SetActive(false);

            if (HasStateAuthority)
            {
                _targetPosition = targetPosition;

                _hitStartTimer = TickTimer.CreateFromSeconds(Runner, _hitStartTime);
                _hitEndTimer = TickTimer.CreateFromSeconds(Runner, _hitEndTime);

                StaticServiceLocator.Instance.Get<EffectSpawner>()
                    .RequestPlayOneShotEffect(_kunaiEffect, targetPosition, quaternion.identity);
            }
        }

        [Rpc]
        private void RPC_ShowAimTarget()
        {
            _aimEffect.gameObject.SetActive(true);
        }
    }
}
