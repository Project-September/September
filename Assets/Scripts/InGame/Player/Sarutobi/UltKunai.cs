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

        [SerializeField] private AnimationClipPlayer _animationClipPlayer;
        [SerializeField] private AnimationClip _idleClip;

        [SerializeField] private float _hitStartTime;
        [SerializeField] private float _hitEndTime;
        [SerializeField] private float _hitRadius;
        [SerializeField] private LayerMask _hitLayerMask;
        [SerializeField] private LayerMask _groundLayerMask;
        [SerializeField] private int _damage;

        public event Action OnThrow;

        private bool _isAiming;
        private Vector3 _targetPosition;

        private TickTimer _hitStartTimer;
        private TickTimer _hitEndTimer;

        private readonly Collider[] _hits = new Collider[10];
        private readonly List<Collider> _alreadyHits = new();
        private bool _isHitChecked;

        private const float MeshRotateDuration = 0.3f;

        public void StartEffect()
        {
            _alreadyHits.Clear();
            _isAiming = true;
            _isHitChecked = false;
            RPC_ShowAimTarget();


            DOTween.To(() => new Vector3(0f, 0, 0f), _ =>
            {
                float angle = Quaternion.LookRotation(_aimEffect.position - _meshRoot.position).eulerAngles.y;
                _meshRoot.rotation = Quaternion.Euler(0f, angle, 0f);
            }, Vector3.zero, MeshRotateDuration);
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

            if (!HasInputAuthority || !_isAiming) return;

            if (!_animationClipPlayer.IsPlayingTargetClip(_idleClip))
            {
                _animationClipPlayer.PlayClip(_idleClip);
            }

            if (GetInput(out PlayerInput input))
            {
                float angle = Quaternion.LookRotation(_aimEffect.position - _meshRoot.position).eulerAngles.y;
                _meshRoot.rotation = Quaternion.Euler(new Vector3(0f, angle, 0f));

                Vector3 pos = GetThrowPos();
                RPC_SendTargetPosition(pos);
                if (input.Buttons.IsSet(_throwButton))
                {
                    RPC_HideAimTarget();
                    RPC_Throw(pos);
                    _meshRoot.DOLocalRotate(Vector3.zero, MeshRotateDuration);
                    HitboxDebugUtility.DrawWireSphere(pos, _hitRadius, Color.red, 10f);
                    _isAiming = false;
                }
            }
        }

        private Vector3 GetThrowPos()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            Physics.Raycast(ray, out RaycastHit hit, _groundLayerMask);
            return hit.point;
        }

        [Rpc]
        private void RPC_Throw(Vector3 targetPosition)
        {
            OnThrow?.Invoke();

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
        private void RPC_HideAimTarget()
        {
            _aimEffect.gameObject.SetActive(false);
        }

        [Rpc]
        private void RPC_ShowAimTarget()
        {
            _aimEffect.gameObject.SetActive(true);
        }

        [Rpc]
        private void RPC_SendTargetPosition(Vector3 position)
        {
            _aimEffect.transform.position = position + _aimEffectOffset;
        }
    }
}
