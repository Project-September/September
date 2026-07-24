using System;
using Fusion;
using InGame.Player.Sarutobi;
using UnityEngine;
using UnityEngine.Playables;

namespace InGame.Player.Ability.Effect
{
    [Serializable]
    public class AbilitySarutobiUlt : AbilityUltBase
    {
        [Header("必殺技の発動時間")]
        [SerializeField] private float _duration = 10;
        
        [Header("追加するクナイの本数")]
        [SerializeField] private int _addProjectileCount = 2;

        [SerializeField] private float _jumpHeight = 8f;
        [SerializeField] private float _jumpStartTime;
        [SerializeField] private float _jumpEndTime;
        [SerializeField] private float _landStartTime;
        [SerializeField] private float _landEndTime;

        [SerializeField] private CameraController _cameraController;
        [SerializeField] private Vector3 _cameraOffset;
        [SerializeField] private float _cameraResetDuration = 0.5f;
        [SerializeField] private UltKunai _ultKunai;

        [SerializeField] private PlayableDirector _playableDirector;
        [SerializeField] private PlayableAsset _endSequence;
        [SerializeField] private PlayerManager _playerManager;

        private ThrowKunai _throwKunai;

        private Vector3 _originalPosition;
        private Vector3 _endPosition;

        private bool _isLanding;
        private PlayableAsset _originalAsset;

        protected override bool ManualCutInEnd => true;

        protected override void OnStartEffect()
        {
            var player = Parameter.Owner;

            // コンポーネント取得。見つからなかったらreturn
            if (!_throwKunai && !player.TryGetComponent(out _throwKunai)) return;

            _throwKunai.SetBulletCount(1 + _addProjectileCount);

            _ultKunai.StartEffect();
            _ultKunai.OnThrow += StartLanding;
        }

        protected override void OnCutInStart()
        {
            NetworkObject player = Parameter.Owner;
            _playerManager.RPC_SetPositionLock(true);
            _playerManager.RPC_SetControlState(PlayerManager.PlayerControlState.ForcedControl);
            _originalPosition = player.transform.position;
            _endPosition = player.transform.position + Vector3.up * _jumpHeight;
            _cameraController.ChangeOffset(_cameraOffset, 0f);
        }

        protected override void OnCutInUpdate(float deltaTime)
        {
            if (_playableDirector.state != PlayState.Playing)
            {
                if (!_isLanding) return;

                _playableDirector.playableAsset = _originalAsset;
                EndCutIn();
                return;
            }

            if (!_isLanding)
            {
                float jumpDuration = _jumpEndTime - _jumpStartTime;
                float t = Mathf.Clamp01((float)_playableDirector.time - _jumpStartTime) / jumpDuration;
                MovePosition(Vector3.Lerp(_originalPosition, _endPosition, t));
            }
            else {
                float landDuration = _landEndTime - _landStartTime;
                float t = Mathf.Clamp01((float)_playableDirector.time - _landStartTime) / landDuration;
                MovePosition(Vector3.Lerp(_endPosition, _originalPosition, t));
            }
        }

        private void MovePosition(Vector3 targetPosition)
        {
            NetworkObject player = Parameter.Owner;
            player.transform.position = targetPosition;
        }

        private void StartLanding()
        {
            _ultKunai.OnThrow -= StartLanding;

            _originalAsset = _playableDirector.playableAsset;
            _playableDirector.playableAsset = _endSequence;
            _playableDirector.Play();
            _isLanding = true;
        }

        protected override void OnCutInEnd()
        {
            _cameraController.ResetOffset(_cameraResetDuration);
            _playerManager.RPC_SetPositionLock(false);
            _playerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
            _isLanding = false;
        }

        protected override void OnUpdateUlt(float deltaTime)
        {
            if (TimeSinceCutInEnd > _duration)
            {
                _throwKunai?.SetBulletCount(1);
                RequestEndAbility();
            }
        }
    }
}
