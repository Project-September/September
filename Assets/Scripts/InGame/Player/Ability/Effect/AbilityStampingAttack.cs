using System.Collections.Generic;
using Fusion;
using InGame.Common;
using InGame.Health;
using InGame.Player;
using InGame.Player.Ability;
using September.Common;
using UnityEngine;

namespace InGame.Exhibit
{
    public class AbilityStampingAttack : AbilityBase
    {
        [Header("Timing")]
        [SerializeField] private float _attackStartGroundDistance;
        [SerializeField] private float _attackedFriezeTime;
        [Header("Animation")]
        [SerializeField] private AnimationClip _attackAnimation;
        [SerializeField] private AnimationClip _fallAnimation;
        [SerializeField] private AnimationClip _landingAnimation;
        [Header("Attack")]
        [SerializeField] private NetworkObject _shockwavePrefab;
        [SerializeField] private float _shockwaveMinSize;
        [SerializeField] private float _shockwaveScaleDuration;
        [SerializeField] private float _attackRange;
        [SerializeField] private int _damageAmount;
        [SerializeField] private float _knockBackPower;

        private NetworkObject _shockwaveObject;
        private List<NetworkObject> _unattachedPlayers = new();
        private StampingState _stampingState;
        private float _slashEndTime; //振り下ろし終了時間
        private float _landedTime;
        private float _endTime;

        private GameObject _playerObject;
        private AnimationClipPlayer _animationClipPlayer;
        private AnimationClipPlayerManager _animationClipPlayerManager;
        private PlayerMovement _playerMovement;

        protected override void OnStart()
        {
            _stampingState = StampingState.Falling;
            _playerMovement.IgnoreMoveInput = true;
            _playerMovement.IgnoreEvasionInput = true;
            _animationClipPlayerManager.SetIgnoreFallAnimation(true);

            _unattachedPlayers.Clear();
            foreach (var player in PlayerDatabase.Instance.PlayerObjectDic)
            {
                if (player.Value.gameObject == _playerObject)
                    continue;

                Debug.Log($"Target {player.Value.gameObject.name}");
                _unattachedPlayers.Add(player.Value);
            }

            _animationClipPlayer.PlayClip(_attackAnimation);
            _slashEndTime = Runner.SimulationTime + _attackAnimation.length;
        }

        protected override void OnUpdate(float deltaTime)
        {
            switch (_stampingState)
            {
                case StampingState.Falling:
                    Falling();
                    break;

                case StampingState.Landing:
                    Landing();
                    break;
            }
        }

        /// <summary>
        /// 落下中の処理
        /// </summary>
        private void Falling()
        {
            if (Runner.SimulationTime >= _slashEndTime)
            {
                _animationClipPlayer.PlayClipLoop(_fallAnimation);
            }

            if (_playerMovement.IsGround)
            {
                if (Runner.SimulationTime >= _slashEndTime)
                {
                    _animationClipPlayer.StopClip(_fallAnimation);
                }

                _animationClipPlayer.PlayClip(_landingAnimation);
                _endTime = Runner.SimulationTime + Mathf.Max(_shockwaveScaleDuration, _attackedFriezeTime);
                _landedTime = Runner.SimulationTime;

                Vector3 feetPosition = _playerMovement.MoveCapsuleCollider.bounds.min + Vector3.up * 0.1f;
                _shockwaveObject = Runner.Spawn(_shockwavePrefab, feetPosition);
                _shockwaveObject.transform.localScale = Vector3.one * _shockwaveMinSize;

                _stampingState = StampingState.Landing;
            }
        }

        /// <summary>
        /// 着地時の処理
        /// </summary>
        private void Landing()
        {
            float t = Mathf.InverseLerp(_landedTime, _landedTime + _shockwaveScaleDuration, Runner.SimulationTime);
            float scale = Mathf.Lerp(_shockwaveMinSize, _attackRange * 2, t);
            _shockwaveObject.transform.localScale = Vector3.one * scale;
            float attackRange = scale / 2;

            foreach (var player in _unattachedPlayers)
            {
                //衝撃波に触れた
                if ((_playerObject.transform.position - player.transform.position).sqrMagnitude < attackRange)
                {
                    _unattachedPlayers.Remove(player);

                    //ダメージ処理
                    if (player.TryGetComponent(out IDamageable damageable))
                    {
                        var hitData = new HitData(HitActionType.Damage, _damageAmount, _playerMovement.Object.InputAuthority, damageable.OwnerPlayerRef);
                        damageable.TakeHit(ref hitData);
                    }

                    //吹き飛ばす処理
                    if (player.TryGetComponent(out PlayerMovement movement))
                    {
                        var dir = movement.transform.position - _playerObject.transform.position;
                        var distance = dir.magnitude;

                        var power = _knockBackPower / Mathf.Max(distance, 0.1f);

                        movement.AddFlyingVelocity(dir.normalized * power);
                    }
                }
            }


            if (Runner.SimulationTime > _endTime)
            {
                _playerMovement.IgnoreMoveInput = false;
                _playerMovement.IgnoreEvasionInput = false;
                _animationClipPlayerManager.SetIgnoreFallAnimation(false);
                Runner.Despawn(_shockwaveObject);
                RequestEndAbility();
            }
        }

        public override void SetPlayerComponent(GameObject player)
        {
            _playerObject = player;
            _playerMovement = player.GetComponent<PlayerMovement>();
            _animationClipPlayer = player.GetComponent<AnimationClipPlayer>();
            _animationClipPlayerManager = player.GetComponent<AnimationClipPlayerManager>();
        }

        public enum StampingState
        {
            Falling,  // 落ちる
            Landing   // 着地
        }
    }
}