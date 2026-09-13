using System.Collections.Generic;
using Fusion;
using InGame.Common;
using InGame.Health;
using InGame.Player;
using InGame.Player.Ability;
using September.Common;
using September.InGame.Effect;
using UnityEngine;

namespace InGame.Exhibit
{
    public class AbilityStampingAttack : AbilityBase
    {
        [Header("Time")]
        [SerializeField] private float _attackedFreezeTime;
        [Header("Animation")]
        [SerializeField] private AnimationClip _attackAnimation;
        [SerializeField] private AnimationClip _fallAnimation;
        [SerializeField] private AnimationClip _landingAnimation;
        [Header("Attack")]
        [SerializeField] private NetworkObject _impactEffectPrefab;
        [SerializeField] private EffectType _stampEffectType;
        [SerializeField] private float _impactEffectOffset = 1f;
        [SerializeField] private Vector3 _impactEffectRotationOffset = Vector3.zero;
        [SerializeField] private float _impactSize = 1f;
        [SerializeField] private float _attackRange;
        [SerializeField] private int _damageAmount;
        [SerializeField] private float _knockBackPower;

        private NetworkObject _impactEffectObject;
        private List<NetworkObject> _attackTargetPlayers = new();
        private StampingState _stampingState;
        private float _slashEndTime; //�U�艺�낵�I������
        private float _endTime; //�A�r���e�B�̏I������

        private GameObject _playerObject;
        private AnimationClipPlayer _animationClipPlayer;
        private AnimationClipPlayerManager _animationClipPlayerManager;
        private PlayerMovement _playerMovement;
        private EffectSpawner _effectSpawner;
        private EffectID _effectId;

        protected override void OnStart()
        {
            _stampingState = StampingState.Swing;
            _playerMovement.IgnoreMoveInput = true;
            _playerMovement.IgnoreEvasionInput = true;

            _animationClipPlayer.PlayClip(_attackAnimation);
            _animationClipPlayerManager.EnableFallMotion = false;

            _slashEndTime = Runner.SimulationTime + _attackAnimation.length;

            //�����ȊO���U���Ώۂɂ���
            _attackTargetPlayers.Clear();
            foreach (var player in PlayerDatabase.Instance.PlayerObjectDic)
            {
                if (player.Value.gameObject == _playerObject)
                    continue;

                _attackTargetPlayers.Add(player.Value);
            }
            if (!_effectSpawner)
                _effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();

        }

        protected override void OnUpdate(float deltaTime)
        {
            switch (_stampingState)
            {
                case StampingState.Swing:
                    Swing();
                    break;
                case StampingState.Falling:
                    Falling();
                    break;
                case StampingState.Landing:
                    Landing();
                    break;
            }
        }

        /// <summary>
        /// �U�艺�낵���̏���
        /// </summary>
        private void Swing()
        {
            //�U�艺�낵���I�������痎���Ɉڍs����
            if (Runner.SimulationTime >= _slashEndTime)
            {
                _animationClipPlayer.PlayClipLoop(_fallAnimation);
                _stampingState = StampingState.Falling;
                var excaliburTransform = GetExcaliburTransform();
                if (excaliburTransform != null)
                {
                    _effectId = _effectSpawner.RequestPlayLoopEffect(
                        _stampEffectType,
                        excaliburTransform.position,
                        Quaternion.identity,
                        Parameter.Owner.transform);
                }
            }

            if (_playerMovement.IsGround)
                OnLanded();
        }

        /// <summary>
        /// �������̏���
        /// </summary>
        private void Falling()
        {
            if (_playerMovement.IsGround)
            {
                _animationClipPlayer.StopClip(_fallAnimation);
                OnLanded();
            }
        }

        /// <summary>
        /// ���n���̏���
        /// </summary>
        private void Landing()
        {
            if (Runner.SimulationTime > _endTime)
            {
                if (_impactEffectObject != null)
                {
                    Runner.Despawn(_impactEffectObject);
                    _impactEffectObject = null;
                }

                _playerMovement.IgnoreMoveInput = false;
                _playerMovement.IgnoreEvasionInput = false;
                _animationClipPlayerManager.EnableFallMotion = true;
                RequestEndAbility();
            }
        }

        /// <summary>
        /// ���n���̏���
        /// </summary>
        private void OnLanded()
        {
            _animationClipPlayer.PlayClip(_landingAnimation);
            _endTime = Runner.SimulationTime + _attackedFreezeTime;

            _effectSpawner.StopEffect(_effectId);

            Vector3 feetPosition = _playerMovement.MoveCapsuleCollider.bounds.min + Vector3.up * 0.1f;
            var forward = _playerObject.transform.forward.normalized;
            Vector3 spawnPosition = feetPosition + forward * _impactEffectOffset;
            _impactEffectObject = Runner.Spawn(
                _impactEffectPrefab,
                spawnPosition,
                _playerObject.transform.rotation * Quaternion.Euler(_impactEffectRotationOffset),
                Parameter.Owner.InputAuthority);
            _impactEffectObject.transform.localScale = Vector3.one * _impactSize;

            ApplyImpactDamage(spawnPosition);

            _stampingState = StampingState.Landing;
        }

        private void ApplyImpactDamage(Vector3 impactPosition)
        {
            HitboxDebugUtility.DrawWireSphere(impactPosition, _attackRange, Color.red);

            foreach (NetworkObject player in _attackTargetPlayers)
            {
                if ((impactPosition - player.transform.position).sqrMagnitude >= _attackRange * _attackRange)
                    continue;

                if (player.TryGetComponent(out IDamageable damageable))
                {
                    var hitData = new HitData(
                        HitActionType.Damage,
                        _damageAmount,
                        _playerMovement.Object.InputAuthority,
                        damageable.OwnerPlayerRef);
                    damageable.TakeHit(ref hitData);
                }

                if (player.TryGetComponent(out PlayerMovement movement))
                {
                    var direction = movement.transform.position - impactPosition;
                    float distance = direction.magnitude;
                    float power = _knockBackPower / Mathf.Max(distance, 0.1f);
                    movement.AddFlyingVelocity(direction.normalized * power);
                }
            }
        }

        public override void SetPlayerComponent(GameObject player)
        {
            _playerObject = player;
            _playerMovement = player.GetComponent<PlayerMovement>();
            _animationClipPlayer = player.GetComponent<AnimationClipPlayer>();
            _animationClipPlayerManager = player.GetComponent<AnimationClipPlayerManager>();
        }

        private Transform GetExcaliburTransform()
        {
            var equipmentManager =
                _playerObject.GetComponent<PlayerEquipmentManager>();

            if (equipmentManager == null)
                return null;

            foreach (var equipment in equipmentManager.CurrentEquipments.Values)
            {
                if (equipment.Type == EquipmentType.Armory &&
                    equipment.ClonedObject != null)
                {
                    return equipment.ClonedObject.transform;
                }
            }

            return null;
        }

        public enum StampingState
        {
            Swing,//�U�艺�낵
            Falling,  // ������
            Landing   // ���n
        }
    }
}
