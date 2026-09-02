using InGame.Common;
using InGame.Player;
using InGame.Player.Ability;
using UnityEngine;

namespace InGame.Exhibit
{
    public class AbilityStampingAttack : AbilityBase
    {
        [Header("Timing")]
        [SerializeField] private float _attackStartGroundDistance;
        [SerializeField] private float _attackedFriezeTime;
        [Header("Animation")]
        [SerializeField] private AnimationClip _fallAnimation;
        [SerializeField] private AnimationClip _attackAnimation;
        [SerializeField] private AnimationClip _landingAnimation;

        private StampingState _stampingState;
        private float _endTime;

        private AnimationClipPlayer _animationClipPlayer;
        private PlayerMovement _playerMovement;
        protected override void OnStart()
        {
            _stampingState = StampingState.Falling;
            _animationClipPlayer.PlayClip(_fallAnimation);
            _playerMovement.IgnoreMoveInput = true;
            _playerMovement.IgnoreEvasionInput = true;

            Debug.Log("ジャンプ切り！！");
        }
        protected override void OnUpdate(float deltaTime)
        {
            switch (_stampingState)
            {
                case StampingState.Falling:
                    Falling();
                    break;

                case StampingState.Attacking:
                    Attacking();
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
            if (_playerMovement.GroundDistance < _attackStartGroundDistance)
            {
                Debug.Log("攻撃！！");
                PlayAnimation(_attackAnimation);
                _stampingState = StampingState.Attacking;
            }
        }

        /// <summary>
        /// 攻撃中の処理
        /// </summary>
        private void Attacking()
        {
            if (_playerMovement.IsGround)
            {
                Debug.Log("着地！！");
                PlayAnimation(_landingAnimation);
                _endTime = Runner.SimulationTime + _attackedFriezeTime;
                _stampingState = StampingState.Landing;
            }
        }

        /// <summary>
        /// 着地時の処理
        /// </summary>
        private void Landing()
        {
            if (Runner.SimulationTime > _endTime)
            {
                Debug.Log("終了！！");
                _playerMovement.IgnoreMoveInput = false;
                _playerMovement.IgnoreEvasionInput = false;
                RequestEndAbility();
            }
        }

        private void PlayAnimation(AnimationClip clip)
        {
            if (!_animationClipPlayer || !clip) return;

            _animationClipPlayer.PlayClip(clip);
        }
        public override void SetPlayerComponent(GameObject player)
        {
            _playerMovement = player.GetComponent<PlayerMovement>();
            _animationClipPlayer = player.GetComponent<AnimationClipPlayer>();
        }

        public enum StampingState
        {
            Falling,  // 落ちる
            Attacking, // 攻撃
            Landing   // 着地
        }
    }
}