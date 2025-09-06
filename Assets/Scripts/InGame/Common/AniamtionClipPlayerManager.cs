using InGame.Common;
using InGame.Player;
using UnityEngine;

namespace  InGame.Common
{
    /// <summary>
    /// ClipPlayerをコントロールするクラス
    /// </summary>
    public class AnimationClipPlayerManager : MonoBehaviour
    {
        [SerializeField] private AnimationClipPlayer _animationClipPlayer;
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private AnimationClip _jumpOver;
        [SerializeField] private AnimationClip _fallDown;
        
        private bool _isVaultingLastFrame = false;
        
        private void LateUpdate()
        {
            if (_animationClipPlayer)
            {
                var maxSpeed = _playerMovement.DashMoveSpeed;
                var walkSpeed = _playerMovement.WalkSpeed;
                var moveSpeed = _playerMovement.MoveVelocity.magnitude;
                var weight = 0f;
                if (moveSpeed <= walkSpeed)
                {
                    // 0..Walk -> 0..1
                    weight = Mathf.InverseLerp(0f, walkSpeed, moveSpeed);
                }
                else
                {
                    // Walk..Max -> 1..2
                    weight = Mathf.InverseLerp(walkSpeed, maxSpeed, moveSpeed) + 1f;
                }
                
                _animationClipPlayer.SetLocoWeight(weight);

                if (_playerMovement.DoingVault && !_isVaultingLastFrame)
                {   //上り始めたらVaultアニメーションを再生
                    _isVaultingLastFrame = true;
                    _animationClipPlayer.SetTopPriorityClip(_jumpOver);
                }
                else if (!_playerMovement.DoingVault && _isVaultingLastFrame)
                {   //Vaultが終わったらTopPriorityClipを解除
                    _isVaultingLastFrame = false;
                    _animationClipPlayer.SetTopPriorityClip(null);
                }
                
                //上るアニメーション中ではなくIsGroundedがfalseになったらFallDownアニメーションを再生
                if (!_animationClipPlayer.IsPlayingTargetClip(_jumpOver) && !_playerMovement.IsGroundNet)
                {
                    _animationClipPlayer.SetTopPriorityClip(_fallDown);
                }
                else if (_playerMovement.IsGroundNet && _animationClipPlayer.IsPlayingTargetClip(_fallDown))
                {   //地面に着地したらFallDownアニメーションを解除
                    _animationClipPlayer.SetTopPriorityClip(null);
                }
            }
        }
    }
}
