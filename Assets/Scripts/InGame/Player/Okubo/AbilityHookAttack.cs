using System.Collections.Generic;
using Fusion;
using InGame.Health;
using September.Common;
using UnityEngine;

namespace InGame.Player.Okubo
{
    public class AbilityHookAttack : NetworkBehaviour
    {
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private PlayerInputManager _playerInputManager;
        [SerializeField] private Material _wireMaterial;
        [SerializeField] private Transform _hookOrigin;
        [SerializeField] private float _stretchSpeed;
        [SerializeField] private float _pullSpeed;
        [SerializeField] private float _wireLength;
        [SerializeField] private float _stretchedWaitTime = 0.3f;
        [SerializeField] private float _coolDownTime = 1.0f;
        [SerializeField] private float _wireThickness;
        [SerializeField] private float _hitRadius;
        [SerializeField] private Transform _wireCyl;
        [SerializeField] private Transform _hookObject;
        [SerializeField] private int _damageAmount;

        private HookAttackState _currentState;
        private float _currentHookLength;
        private float _waitTimer;
        /// <summary>PlayerMovementなどのキャッシュ用 </summary>
        private Dictionary<PlayerRef, HookTargetData> _targetData = new();
        private PlayerRef _ownerRef;

        public override void Spawned()
        {
            _wireCyl.gameObject.SetActive(false);
            _ownerRef = _ownerRef = Object.InputAuthority;
        }

        public override void FixedUpdateNetwork()
        {
            _playerInputManager.GetPlayerInput(out var input);

            if (!HasInputAuthority) return;

            switch (_currentState)
            {
                case HookAttackState.Idol:
                    //フック攻撃開始
                    if (input.Buttons.IsSet(PlayerButtons.Ability2))
                        ChangeState(HookAttackState.Stretching);
                    break;
                case HookAttackState.Stretching:
                    OnStretching();
                    break;
                case HookAttackState.Pulling:
                    OnPulling();
                    break;
                case HookAttackState.Stretched:
                case HookAttackState.CoolDown:
                    //待機処理
                    OnWait();
                    break;
            }
        }
        private void ChangeState(HookAttackState state)
        {
            _currentState = state;

            switch (state)
            {
                case HookAttackState.Idol:
                    _playerMovement.IgnoreMoveInput = false;
                    break;
                //フック攻撃初期化
                case HookAttackState.Stretching:
                    _wireCyl.gameObject.SetActive(true);
                    _targetData.Clear();
                    _playerMovement.IgnoreMoveInput = true;
                    _currentHookLength = 0;
                    break;
                case HookAttackState.Stretched:
                    _waitTimer = _stretchedWaitTime;
                    break;
                case HookAttackState.Pulling:
                    foreach (var kv in _targetData)
                    {
                        if (!kv.Value.IsTarget) continue;
                        Debug.Log(kv.Value);
                        RPC_HookStart(kv.Key);
                    }
                    break;

                case HookAttackState.CoolDown:
                    foreach (var kv in _targetData)
                    {
                        if (!kv.Value.IsTarget) continue;
                        RPC_HookEnd(kv.Key);
                        kv.Value.IsTarget = false;
                    }
                    _wireCyl.gameObject.SetActive(false);
                    _waitTimer = _coolDownTime;
                    break;
            }
        }

        private void OnStretching()
        {
            _currentHookLength += _wireLength / _stretchSpeed * Runner.DeltaTime;

            //最大の長さまで伸びた
            if (_currentHookLength >= _wireLength)
            {
                _currentHookLength = _wireLength;
                ChangeState(HookAttackState.Stretched);
            }

            UpdateHookLength(_currentHookLength, this.transform.forward);
            GetHitPlayer(_currentHookLength, this.transform.forward);
        }

        private void OnPulling()
        {
            _currentHookLength -= _wireLength / _pullSpeed * Runner.DeltaTime;

            if (_currentHookLength <= 0)
            {
                _currentHookLength = 0;
                ChangeState(HookAttackState.CoolDown);
            }

            UpdateHookLength(_currentHookLength, transform.forward);

            var hookSqr = (this.transform.position - _hookObject.transform.position).sqrMagnitude;
            foreach (var kv in _targetData)
            {
                if (kv.Value.IsHookFollow) continue;
                if (!PlayerDatabase.Instance.PlayerObjectDic.TryGet(kv.Key, out var obj)) continue;
                var targetSqer = (this.transform.position - obj.transform.position).sqrMagnitude;

                if (targetSqer > hookSqr)
                {
                    RPC_HookFollow(kv.Key);
                    _targetData[kv.Key].IsHookFollow = true;
                }
            }

        }

        private void OnWait()
        {
            _waitTimer -= Runner.DeltaTime;

            if (_waitTimer > 0)
                return;

            switch (_currentState)
            {
                case HookAttackState.Stretched:
                    ChangeState(HookAttackState.Pulling);
                    break;

                case HookAttackState.CoolDown:
                    ChangeState(HookAttackState.Idol);
                    break;
            }
        }

        private void UpdateHookLength(float length, Vector3 direction)
        {
            direction = direction.normalized;

            // 長さ変更
            Vector3 scale = _wireCyl.localScale;
            scale.y = length * 0.5f; // Cylinderは高さ2が基準
            _wireCyl.localScale = scale;

            // 中心位置を始点から length/2 の位置へ
            _wireCyl.position = _hookOrigin.transform.position + direction * (length * 0.5f);

            // 向きを合わせる
            _wireCyl.up = direction;
        }

        private void GetHitPlayer(float length, Vector3 direction)
        {
            Vector3 position = _hookOrigin.transform.position + direction * length;
            var hitObjects = Physics.OverlapSphere(position, _hitRadius);

            foreach (var obj in hitObjects)
            {
                GameObject hitObject = obj.transform.root.gameObject;
                if (hitObject == this.gameObject || !hitObject.CompareTag("Player")) continue;

                //ヒットしたオブジェクトからPrayerRefを取得
                foreach (var pair in PlayerDatabase.Instance.PlayerObjectDic)
                {
                    if (pair.Value.gameObject == hitObject)
                    {
                        if (_targetData.ContainsKey(pair.Key)) continue;

                        //ターゲットデータに入れる
                        var target = TryGetTargetData(pair.Key);
                        if (target == null) continue;
                        //フック攻撃対象にする
                        target.IsTarget = true;

                        //ダメージ処理
                        if (pair.Value.TryGetComponent(out IDamageable damageable))
                        {
                            var hitData = new HitData(HitActionType.Damage, _damageAmount, _ownerRef, damageable.OwnerPlayerRef);
                            damageable.TakeHit(ref hitData);
                        }
                        break; ;
                    }
                }
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_HookStart(PlayerRef playerRef)
        {
            _wireCyl.gameObject.SetActive(true);
            var targetData = TryGetTargetData(playerRef);
            if (targetData == null || !targetData.PlayerObject.HasStateAuthority) return;

            Debug.Log("StartHook");
            targetData.PlayerMovement.OnStartHook();
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_HookFollow(PlayerRef playerRef)
        {
            var targetData = TryGetTargetData(playerRef);
            if (targetData == null || !targetData.PlayerObject.HasStateAuthority) return;

            targetData.PlayerMovement.OnHookFollow(_hookObject);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]   
        public void RPC_HookEnd(PlayerRef playerRef)
        {
            _wireCyl.gameObject.SetActive(false);
            var targetData = TryGetTargetData(playerRef);
            if (targetData == null || !targetData.PlayerObject.HasStateAuthority) return;

            targetData.PlayerMovement.OnEndHook();
        }

        /// <summary>
        /// RPCで飛んだ先にTargetDataがない時の対策
        /// </summary>
        private HookTargetData TryGetTargetData(PlayerRef playerRef)
        {
            if (_targetData.TryGetValue(playerRef, out var targetData)) return targetData;

            if (!CreateTargetData(playerRef)) return null;

            return _targetData[playerRef];
        }

        private bool CreateTargetData(PlayerRef playerRef)
        {
            if (!PlayerDatabase.Instance.PlayerObjectDic.TryGet(playerRef, out var playerObject)) return false;

            //ターゲットに入れる
            _targetData.Add(playerRef, new HookTargetData(playerRef, playerObject));
            return true;
        }

        public enum HookAttackState
        {
            Idol, Stretching, Stretched, Pulling, CoolDown
        }

        public class HookTargetData
        {
            public PlayerRef Player { get; private set; }
            public NetworkObject PlayerObject { get; private set; }
            private PlayerMovement _playerMovement;
            public PlayerMovement PlayerMovement
            {
                get
                {
                    if (_playerMovement == null)
                    {
                        _playerMovement = PlayerObject.GetComponentInChildren<PlayerMovement>();
                    }

                    return _playerMovement;
                }
            }
            public bool IsHookFollow;
            public bool IsTarget;

            public HookTargetData(PlayerRef playerRef, NetworkObject playerObject)
            {
                Player = playerRef;
                PlayerObject = playerObject;
                IsHookFollow = false;
                IsTarget = false;
            }
        }
    }
}
