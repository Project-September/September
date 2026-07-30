using Fusion;
using InGame.Player;
using September.Common;
using Unity.Mathematics;
using UnityEngine;

namespace September.InGame.Exhibit
{
    public class BallistaMove : NetworkBehaviour, IProjectileMovement
    {
       [SerializeField] private Transform _barrel;
       [SerializeField] private Transform _rotateBase;
       [SerializeField] private CameraController _cameraController;
       [SerializeField] private LayerMask _layerMask;
       [SerializeField] private float _sens;
       [SerializeField] private float _padSens;
       [SerializeField] private float _playerOffset = 3;
       [Header("CameraAngleLimit")]
       [SerializeField] private bool _useYawLimit;
       [SerializeField] private Vector2 _pitchLimit = new Vector2(-90f, 90f);
       [SerializeField] private Vector2 _yawLimit = new Vector2(-90f, 90f);
       [SerializeField] private Vector3 _baseUp;
       [SerializeField] private Vector3 _barrelRight;
       [SerializeField] private float _baseYaw;
       [SerializeField] private float _basePitch;

       private Quaternion _barrelDefaultLocalRotation;
       private Quaternion _baseDefaultLocalRotation;
       [Networked] private NetworkObject PlayerObject { get; set; }
       [Networked] private float Pitch { get; set; }
       [Networked] private float Yaw { get; set; }

       public override void Spawned()
       {
          _baseDefaultLocalRotation =  Quaternion.AngleAxis(_baseYaw, _baseUp) * _rotateBase.localRotation;
          _barrelDefaultLocalRotation = _barrel.localRotation; ;

          _cameraController.Init(true);
          _baseYaw = _cameraController.CameraYaw;
          _basePitch = _cameraController.CameraPitch;
          
          Debug.Log($"pitch: {_cameraController.CameraPitch} yaw: {_cameraController.CameraYaw}");
       }

       public override void Render()
       {
          _rotateBase.localRotation = Quaternion.AngleAxis(Yaw, _baseUp) * _baseDefaultLocalRotation;
          _barrel.localRotation = _barrelDefaultLocalRotation * Quaternion.AngleAxis(Pitch, _barrelRight);
       }

       public void Initialize(NetworkObject playerObject, PlayerRef playerRef)
       {
          PlayerObject = playerObject;
          _cameraController.SetCameraRotate(_basePitch, _baseYaw);
       }

       public void MoveUpdate(PlayerInput input)
       {   
          RotateCamera(input);
          
          var cameraForward = _cameraController.GetCameraForward();
          if (Physics.Raycast(_cameraController.GetCameraPosition(), cameraForward, out var hit, 100, _layerMask))
          {
             var direction = hit.point - _rotateBase.transform.position;
             var euler = (Quaternion.LookRotation(direction.normalized)).eulerAngles;
             Debug.Log(euler);
             var defaultYaw = _baseDefaultLocalRotation.eulerAngles;
             Pitch = euler.x;
             Yaw = defaultYaw.y + euler.y;
             Debug.Log($"pitch: {Pitch}, yaw: {Yaw}");
          }
          else
          {
             var cameraEuler = Quaternion.LookRotation(cameraForward).eulerAngles;
             Pitch = cameraEuler.x;
             Yaw = cameraEuler.y;
          }

          UpdatePlayerPosition();
       }

       void RotateCamera(PlayerInput input)
       {
          _cameraController.RotateCamera(input.LookDirection, Runner.DeltaTime);
          var pitch = _cameraController.CameraPitch;
          var yaw = Mathf.DeltaAngle(0, _cameraController.CameraYaw);
          pitch = Mathf.Clamp(pitch, _pitchLimit.x, _pitchLimit.y);
          yaw = Mathf.Clamp(yaw, _yawLimit.x, _yawLimit.y);
          _cameraController.SetCameraRotate(pitch, yaw);
       }

       void UpdatePlayerPosition()
       {
          var quaternion = Quaternion.AngleAxis(Yaw + _baseYaw, Vector3.up);
          PlayerObject.transform.rotation = quaternion;
          PlayerObject.transform.position = _rotateBase.position + quaternion * (Vector3.back * _playerOffset);
       }
       public void Refresh()
       { 
          _cameraController.CameraReset();
       }
    }
}