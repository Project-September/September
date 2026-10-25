using InGame.Common;
using InGame.Player.Hatano;
using UnityEngine;

namespace September.InGame.Player.Hatano
{
    public class HatanoAimIKController : MonoBehaviour
    {
        [SerializeField] private AimIKController _aimIKController;
        [SerializeField] private HatanoAbilityStatusManagement _abilityManager;
        [SerializeField] private AnimationClipPlayer _animClipPlayer;
        [SerializeField] private AimCameraController _aimCamera;
        [SerializeField] private LayerMask _aimMask;
        [SerializeField] private float _aimSpeed = 5f;

        private void Update()
        {
            // 武器切り替えモーションが開始したらweightを下げる。終了する少し前に解除
            if (_abilityManager.IsChangingWeapon && _abilityManager.WeaponChangeRemainingDuration > 1f / _aimSpeed)
            {
                _aimIKController.Weight = Mathf.MoveTowards(_aimIKController.Weight, 0f, _aimSpeed * Time.deltaTime);
            }
            else
            {
                // エイム中であればweightを上げる
                _aimIKController.Weight = Mathf.MoveTowards(_aimIKController.Weight, _animClipPlayer.AimBlendWeight, _aimSpeed * Time.deltaTime);
            }

            _aimIKController.Target = _aimCamera.AimOrigin + _aimCamera.AimDirection * 1000f;
        }
    }
}
