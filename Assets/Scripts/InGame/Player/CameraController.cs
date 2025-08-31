using System;
using Cinemachine;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

namespace InGame.Player
{
    /// <summary> プレイヤーのカメラ操作 </summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float _sens;
        [SerializeField] private float _padSens;
        [SerializeField] private Transform _characterTf;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Transform _cameraTf;
        [SerializeField] private CinemachineVirtualCameraBase _camera;
        [SerializeField] private Vector3 _cameraOffset = new Vector3(0, 1.65f, 0);
        [Header("CameraCollision")]
        [SerializeField] private LayerMask _collideAgainst = ~0;
        [SerializeField] private float _cameraRadius;
        [Header("CameraMotion")]
        [SerializeField] private float _motionDuration;
        [SerializeField] private Ease _motionEase;
        
        // camera rotation
        Quaternion _defaultRotation;
        private float _cameraPitch;
        private float _cameraYaw;
        private bool _isInRotation;
        Tweener _rotateTweener;
        
        // camera position
        private Vector3 _currentOffset;
        private Vector3 _defaultOffset;
        Tweener _offsetTweener;

        public void Init(bool use)
        {
            if (!use) return;

            _cameraPivot = GameObject.Find("PlayerCameraPivot").transform;
            _camera = _cameraPivot.GetComponentInChildren<CinemachineVirtualCamera>();
            _cameraTf = _camera.transform;
            _camera.Priority = 10000;
            // Prefabの初期状態をデフォルトとして保存
            _defaultRotation = _cameraPivot.localRotation;
            _currentOffset = _cameraTf.localPosition;
            _defaultOffset = _cameraTf.localPosition;
        }

        private void LateUpdate()
        {
            if (_cameraPivot == null)
            {
                _cameraPivot = GameObject.Find("PlayerCameraPivot").transform;
                _camera = _cameraPivot.GetComponentInChildren<CinemachineVirtualCamera>();
                _cameraTf = _camera.transform;
                _camera.Priority = 10000;
                // Prefabの初期状態をデフォルトとして保存
                _defaultRotation = _cameraPivot.localRotation;
                _currentOffset = _cameraTf.localPosition;
                _defaultOffset = _cameraTf.localPosition;
            }

            _cameraPivot.position = _characterTf.position + _cameraOffset;
            CheckCameraDistance();
            RotateCamera(GameInput.I.Player.Look.ReadValue<Vector2>(), Time.deltaTime);
        }

        /// <summary> 入力からカメラを回転させる </summary>
        public void RotateCamera(Vector2 mouseInput, float deltaTime)
        {
            // 他で回転中なら
            if (_isInRotation) return;
            
            float sens = GameInput.I.UseDeviceType == GameInput.DeviceType.KeyboardMouse ? _sens : _padSens;
            float deltaX = mouseInput.y, deltaY = mouseInput.x;
            _cameraPitch -= deltaX * deltaTime * sens;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -90, 90);
            _cameraYaw += deltaY * sens * deltaTime;
            _cameraYaw = ToAngle(_cameraYaw);
            
            _cameraPivot.rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0);
        }
        
        /// <summary>
        /// カメラの水平方向（XZ）を返す
        /// </summary>
        public Vector3 GetCameraForward() => _cameraTf.forward.normalized;

        /// <summary>
        /// カメラの右方向（XZ）を返す
        /// </summary>
        public Vector3 GetCameraRight() => _cameraTf.right.normalized;


        /// <summary> 障害物に応じてカメラの距離を変える </summary>
        void CheckCameraDistance()
        {
            var isHit = Physics.Linecast(_cameraPivot.position, _cameraPivot.position + _cameraPivot.TransformDirection(_currentOffset),
                out var hit, _collideAgainst);
            
            if (isHit)
            {
                Vector3 sphereCenter = hit.point + hit.normal * _cameraRadius;
                _cameraTf.position = sphereCenter;
            }
            else
            {
                _cameraTf.localPosition = _currentOffset;
            }
        }

        /// <summary> カメラを指定方向に回転させる </summary>
        void SmoothRotateCameraTo(Quaternion targetWorldRotation)
        {
            _isInRotation = true;

            // 遷移途中(Pauseを含む)
            if (_rotateTweener.IsActive() && !_rotateTweener.IsComplete())
            {
                _rotateTweener.Kill();
            }
            
            Quaternion endRotation = Quaternion.LookRotation(targetWorldRotation * Vector3.forward, transform.up);

            // 現在のRotationとendRotationを入力のFloatにする
            float targetPitch = endRotation.eulerAngles.x,
                startPitch = _cameraPitch,
                targetYaw = endRotation.eulerAngles.y,
                startYaw = _cameraYaw;

            // 移動Tweenの発火
            _rotateTweener =　DOTween.To(
                () => 0f,
                n =>
                {
                    _cameraPivot.rotation = Quaternion.Euler(math.lerp(startPitch, targetPitch, n), Mathf.LerpAngle(startYaw, targetYaw, n), 0);
                    _cameraPitch = _cameraPivot.rotation.eulerAngles.x;
                    _cameraYaw = _cameraPivot.rotation.eulerAngles.y;
                },
                1f,
                _motionDuration
                )
                .OnComplete(() =>
                {
                    _cameraPitch = _cameraPivot.rotation.eulerAngles.x;
                    _cameraYaw = _cameraPivot.rotation.eulerAngles.y;
                    _isInRotation = false;
                    
                    CheckCameraDistance();
                })
                .SetUpdate(UpdateType.Late)
                .SetEase(_motionEase);
        }

        /// <summary> デフォルト位置にリセットする </summary>
        public void CameraReset()
        {
            // デフォルトのLocalQuaternionをWorldにする
            Quaternion relativeRotation = _characterTf.rotation * _defaultRotation;
            SmoothRotateCameraTo(relativeRotation);
        }

        public void SetCameraPriority(int priority)
        {
            _camera.Priority = priority;
        }

        public void ChangeOffset(Vector3 newOffset, float duration)
        {
            if (_offsetTweener.IsActive() && !_offsetTweener.IsComplete())
            {
                _offsetTweener.Kill();
            }
            
            _offsetTweener = DOTween.To(
                () => _currentOffset,
                v => _currentOffset = v,
                newOffset,
                duration
                )
                .SetUpdate(UpdateType.Late)
                .SetEase(_motionEase);
        }

        public void ResetOffset(float duration)
        {
            ChangeOffset(_defaultOffset, duration);
        }

        /// <summary> 0 <= return < 360 </summary>
        private static float ToAngle(float angle)
        {
            while (true)
            {
                if (angle >= 360) angle -= 360;
                else if (angle < 0) angle += 360;
                else return angle;
            }
        }
    }
}
