using System.Collections.Generic;
using Fusion;
using InGame.Common;
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

        private NetworkObject _shockwaveObject;
        private List<NetworkObject> _unattackedPlayers = new();
        private StampingState _stampingState;
        private float _slashEndTime; //êUÇËâ∫ÇÎÇµèIóπéûä‘
        private float _landedTime;
        private float _endTime;

        private AnimationClipPlayer _animationClipPlayer;
        private AnimationClipPlayerManager _animationClipPlayerManager;
        private PlayerMovement _playerMovement;

        protected override void OnStart()
        {
            _stampingState = StampingState.Falling;
            _playerMovement.IgnoreMoveInput = true;
            _playerMovement.IgnoreEvasionInput = true;
            _animationClipPlayerManager.SetIgnoreFallAnimation(true);

            _unattackedPlayers.Clear();
            foreach (var player in PlayerDatabase.Instance.PlayerObjectDic)
            {
                _unattackedPlayers.Add(player.Value);
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
        /// óéâ∫íÜÇÃèàóù
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
        /// íÖínéûÇÃèàóù
        /// </summary>
        private void Landing()
        {
            float t = Mathf.InverseLerp(_landedTime, _landedTime + _shockwaveScaleDuration, Runner.SimulationTime);
            float scale = Mathf.Lerp(_shockwaveMinSize, _attackRange * 2, t);
            _shockwaveObject.transform.localScale = Vector3.one * scale;

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
            _playerMovement = player.GetComponent<PlayerMovement>();
            _animationClipPlayer = player.GetComponent<AnimationClipPlayer>();
            _animationClipPlayerManager = player.GetComponent<AnimationClipPlayerManager>();
        }

        public enum StampingState
        {
            Falling,  // óéÇøÇÈ
            Landing   // íÖín
        }
    }
}