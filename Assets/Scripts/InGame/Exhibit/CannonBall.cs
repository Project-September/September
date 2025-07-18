using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Addons.Physics;
using InGame.Health;
using InGame.Interact;
using InGame.Player;
using September.Common;
using UniRx;
using UnityEngine;

namespace InGame.Exhibit
{
    public class CannonBall : NetworkBehaviour
    {
        [Header("参照")]
        [SerializeField] private GameObject _model;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private NetworkObject _networkObject;
        [SerializeField] private NetworkRigidbody3D _networkRigidbody3D;
        [SerializeField] private InteractableBase _interactableBase;

        [Header("設定")]
        [SerializeField] private float _power = 10f;
        [SerializeField] private float _upwardForce = 5f;
        [SerializeField] private int _damageAmount = 10;
        [SerializeField] private Vector3 _startPosition;
        [SerializeField] private float _maxFlightTime = 3f; // 飛行最大時間（秒）
        
        [SerializeField] private bool _isLaunching;
        [SerializeField] private float _launchElapsedTime;
        private Transform _currentOwnerTransform;
        private int _equippedInteractor;
        private MeleeHitboxExecutor _meleeHitboxExecutor;

        public event Action OnCannonBallHit;

        public override void Spawned()
        {
            _networkRigidbody3D.RBIsKinematic = true;
            _meleeHitboxExecutor = new MeleeHitboxExecutor(new List<Transform>() { _model.transform }, hitboxRadius: _model.transform.localScale.x * 0.5f);

            Observable.EveryUpdate()
                .Select(_ => _interactableBase.IsInCooldown())
                .DistinctUntilChanged()
                .Subscribe(inCoolDown =>
                {
                    Debug.Log("クールダウン状態: " + inCoolDown);
                    Rpc_SetCannonBallVisible(!inCoolDown); // クールダウン中は非表示
                }).AddTo(this);
            
            _meleeHitboxExecutor.OnHit += hit =>
            {
                var didHitSomething = false;
                if (hit.gameObject == _model) return; // 自分自身には当たらないように
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    if (damageable.OwnerPlayerRef != PlayerRef.FromEncoded(_equippedInteractor))
                    {
                        var hitData = new HitData(HitActionType.Damage, _damageAmount, PlayerRef.FromEncoded(_equippedInteractor), damageable.OwnerPlayerRef);
                        damageable.TakeHit(ref hitData);
                        didHitSomething = true;
                    }
                }
                else
                {
                    if (hit.gameObject.GetComponentInParent<NetworkObject>()?.InputAuthority == _networkObject.InputAuthority)
                        return; // 自分のものには当たらないように
                    // IDamageable ではないが何かに当たった場合もヒット扱い
                    didHitSomething = true;
                }

                if (didHitSomething)
                {
                    Debug.Log( $"{hit.gameObject.name} にヒット");
                    Rpc_ResetCannonBall(); // 全体にリセット
                    OnCannonBallHit?.Invoke();
                }
            };
        }

        public void EquipToInteractor(int interactor)
        {
            var playerRef = PlayerRef.FromEncoded(interactor);
            _networkObject.AssignInputAuthority(playerRef);
            Rpc_SetModelParent(interactor);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_SetModelParent(int interactor)
        {
            Debug.Log(interactor);
            if (interactor == -1)
            {
                _model.transform.SetParent(transform);
                _model.transform.localPosition = Vector3.zero;
                _currentOwnerTransform = null;
                _equippedInteractor = -1;
                return;
            }
            
            var playerRef = PlayerRef.FromEncoded(interactor);
            if (Runner.TryGetPlayerObject(playerRef, out var playerObj))
            {
                var hand = playerObj.GetComponentInChildren<HandSocket>()?.Sockets.FirstOrDefault();
                if (hand)
                {
                    _model.transform.SetParent(hand);
                    _model.transform.localPosition = Vector3.zero;
                    _model.transform.localRotation = Quaternion.identity;
                    _currentOwnerTransform = playerObj.transform;
                    _equippedInteractor = interactor;
                }
            }
        }

        private void Update()
        {
            
            if (Runner?.IsServer == false) return;
            
            if (_isLaunching && HasStateAuthority)
            {
                _meleeHitboxExecutor.Tick(Time.deltaTime);

                _launchElapsedTime += Time.deltaTime;
                if (_launchElapsedTime > _maxFlightTime)
                {
                    Rpc_ResetCannonBall();
                    OnCannonBallHit?.Invoke(); // optional: 飛行時間オーバーでのエフェクトなど
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput(out PlayerInput input) || _isLaunching) return;

            if (input.Buttons.IsSet(PlayerButtons.Attack))
            {
                _networkRigidbody3D.Teleport(_model.transform.position);
                _networkRigidbody3D.RBIsKinematic = false;

                // 斜め上方向に投げるベクトルを作成
                var forward = _currentOwnerTransform.forward;
                var upward = _currentOwnerTransform.up;
                var throwDir = (forward + upward * _upwardForce).normalized;
                _rigidbody.AddForce(throwDir * _power, ForceMode.Impulse);

                Rpc_SetModelParent(-1);
                Rpc_Launch();
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_Launch()
        {
            _isLaunching = true;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_ResetCannonBall()
        {
            _networkRigidbody3D.RBIsKinematic = true;
            _networkRigidbody3D.Teleport(_startPosition);
            
            Rpc_SetModelParent(-1);
            _meleeHitboxExecutor.Init();
            _isLaunching = false;
            _launchElapsedTime = 0f;
            _networkObject.RemoveInputAuthority();
            Rpc_SetCannonBallVisible(false);
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_SetCannonBallVisible(bool isVisible)
        {
            if (_model != null)
            {
                _model.SetActive(isVisible);
            }
        }
    }
}
