using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Common;
using September.Common;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InGame.Player.Ability.Effect
{
    public class AbilityGrapplingHook : NetworkBehaviour, IAfterTick
    {
        [Header("Ability")]
        [SerializeField] private SplineContainer _grappleableSplinePrefab;
        [SerializeField] private GameObject _targetUIPrefab;
        [SerializeField] private float _coolTime;
        [Header("Grappling Hook")]
        [SerializeField] private MinMaxRange _distanceRange;
        [SerializeField] private float _maxAngle;
        [SerializeField] private float _distanceReflectionRate;
        [SerializeField] private float _angleReflectionRate;
        [Header("Jump")]
        [SerializeField] private float _shotWaitTime;
        [SerializeField] private float _pullingSpeed;
        [SerializeField] private Vector3 _pullLastForce;
        [SerializeField] private float _landingDuration;
        [Header("AnimClip")]
        [SerializeField] private AnimationClip _animShot;
        [SerializeField] private AnimationClip _animShotWait;
        [SerializeField] private AnimationClip _animMoveStart;
        [SerializeField] private AnimationClip _animMoveLoop;
        [SerializeField] private AnimationClip _animLanding;
        [Header("WireDisplay")] 
        [SerializeField] private Transform _handSocket;

        private PlayerManager _playerManager;
        private PlayerMovement _playerMovement;
        private AnimationClipPlayer _clipPlayer;
        private AnimationClipPlayerManager _clipPlayerManager;
        private SplineContainer _grappleableSpline;
        private Transform _targetUI;
        private Camera _mainCamera;
        private LineRenderer _wireLine;
        
        private GrappleStateType _grappleState = GrappleStateType.ShotWait;
        private float _jumpTimer;
        private Vector3 _targetPosition;
        private Vector3 _startPosition;
        private float _distanceMag;
        
        [Networked] private AbilityStateType AbilityState { get; set; } = AbilityStateType.Ready;
        [Networked] private NetworkButtons PreviousButtons { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                _playerMovement = GetComponent<PlayerMovement>();
                _playerManager = GetComponent<PlayerManager>();
                _clipPlayer = GetComponent<AnimationClipPlayer>();
                _clipPlayerManager = GetComponent<AnimationClipPlayerManager>();
            }
            
            if (HasInputAuthority)
            {
                _grappleableSpline = Instantiate(_grappleableSplinePrefab);
                _grappleableSpline.transform.position = Vector3.zero;
                _targetUI = Instantiate(_targetUIPrefab).GetComponentInChildren<Image>().transform;
                _mainCamera = Camera.main;
                _wireLine = GetComponent<LineRenderer>();
                _wireLine.enabled = false;
            }
        }

        public override void FixedUpdateNetwork()
        {
            GetInput<PlayerInput>(out var input);
            
            // input authority で判定
            if (HasInputAuthority)
            {
                if (AbilityState == AbilityStateType.Ready)
                {
                    bool canUse = FindGrappleablePosition(out var position);
                    DisplayTargetUI(canUse, position);

                    if (canUse && input.Buttons.WasPressed(PreviousButtons, PlayerButtons.Ability1))
                    {
                        RPC_GrappleStart(position);
                        _targetUI.gameObject.SetActive(false);
                    }
                }
            }
            
            // state authority で移動とクールダウン
            if (HasStateAuthority)
            {
                if (AbilityState == AbilityStateType.Active)
                {
                    // 発動中Tick
                    if (_grappleState == GrappleStateType.ShotWait)
                    {
                        ShotWaitTick();
                    }
                    else if (_grappleState == GrappleStateType.Jumping)
                    {
                        JumpingTick();
                    }
                    else if (_grappleState == GrappleStateType.Landing)
                    {
                        LandingTick();
                    }
                    
                    _playerMovement.SetRotationDirection(_targetPosition - _startPosition);
                }
                else
                {
                    
                }
            }
        }

        public void AfterTick()
        {
            PreviousButtons = GetInput<PlayerInput>().GetValueOrDefault().Buttons;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        void RPC_GrappleStart(Vector3 targetPosition)
        {
            AbilityState = AbilityStateType.Active;
            _grappleState = GrappleStateType.Shot;
            _targetPosition = targetPosition + Vector3.up * 0.05f;
            _startPosition = transform.position;
            _distanceMag = Vector3.Distance(_startPosition, _targetPosition);
            _jumpTimer = 0;
            Shot().Forget();
            
            _playerManager.SetControlState(PlayerManager.PlayerControlState.ForcedControl);
        }

        async UniTask Shot()
        {
            await PlayClipAndWait(_animShot);
            _grappleState = GrappleStateType.ShotWait;
            _clipPlayer.PlayClip(_animShotWait);
            _wireLine.enabled = true;
        }

        void ShotWaitTick()
        {
            _jumpTimer += Runner.DeltaTime;
            
            DisplayWire(_handSocket.position + (_targetPosition - _handSocket.position) * _jumpTimer / _shotWaitTime);
                    
            if (_jumpTimer >= _shotWaitTime)
            {
                _grappleState = GrappleStateType.PreJump;
                _jumpTimer = 0;
                _clipPlayerManager.EnableFallMotion = false;

                PreJump().Forget();
            }
        }

        async UniTask PreJump()
        {
            await PlayClipAndWait(_animMoveStart);
            _grappleState = GrappleStateType.Jumping;
            _clipPlayer.PlayClip(_animMoveLoop);
        }

        void JumpingTick()
        {
            _jumpTimer += Runner.DeltaTime;
            float t = Math.Clamp(_jumpTimer * _pullingSpeed / _distanceMag, 0, 1);
            
            transform.position = Vector3.Lerp(_startPosition, _targetPosition, t);
            
            DisplayWire(_targetPosition);

            if (t >= 1)
            {
                _grappleState = GrappleStateType.Landing;
                _jumpTimer = 0;
                _playerMovement.KnockBack(transform.rotation * _pullLastForce, 0.2f).Forget();
                _wireLine.enabled = false;
                _clipPlayerManager.EnableFallMotion = true;
            }
        }

        void LandingTick()
        {
            if (_playerMovement.IsGround)
            {
                if (_jumpTimer == 0)
                {
                    _clipPlayer.PlayClip(_animLanding);
                }
                
                _jumpTimer += Runner.DeltaTime;
            }

            if (_jumpTimer >= _landingDuration)
            {
                AbilityState = AbilityStateType.Ready;
                _playerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
                _jumpTimer = 0;
            }
        }

        bool FindGrappleablePosition(out Vector3 position)
        {
            position = Vector3.zero;
            if (!HasInputAuthority || !_grappleableSpline || !_grappleableSpline.Splines.Any()) return false;
            
            var splines = _grappleableSpline.Splines;

            // 粗い間隔で最もポイントが低い点を見つける
            Spline minSpline = null;
            float minT = float.MaxValue;
            float minPoint = float.MaxValue;
            
            foreach (var t1 in splines)
            {
                if (!GetMinPoint(t1, new MinMaxRange(0, 1), out var t, out _, out var newPoint)) continue;

                if (minPoint > newPoint)
                {
                    minSpline = t1;
                    minT = t;
                    minPoint = newPoint;
                }
            }
            
            if (minSpline == null) return false;
            
            // そのポイント周辺で最もポイントが低い点を探す
            if (!GetMinPoint(minSpline, new MinMaxRange(minT - 0.05f, minT + 0.05f), out _, out var ansPosition, out _)) Debug.Log("nanikaga okasii");
            
            position = ansPosition;
            
            return true;
        }

        /// <summary> pointの評価値を取得 </summary>
        bool GetEvaluatePoint(Vector3 position, out float point)
        {
            point = float.MaxValue;
            Vector3 posDiff = position - transform.position;

            // 距離判定
            if (posDiff.sqrMagnitude < _distanceRange.Min * _distanceRange.Min || posDiff.sqrMagnitude > _distanceRange.Max * _distanceRange.Max)
            {
                return false;
            }

            // 角度判定
            float angle = Vector3.Angle(_mainCamera.transform.forward, position - _mainCamera.transform.position);
            
            if (angle > _maxAngle)
            {
                return false;
            }
            
            point = posDiff.magnitude * _distanceReflectionRate + angle * _angleReflectionRate;
            
            return true;
        }

        bool GetMinPoint(Spline spline, MinMaxRange tRange, out float t, out Vector3 position, out float point, int resolution = 10, int iterations = 2)
        {
            t = -1;
            position = Vector3.zero;
            point = float.MaxValue;
            
            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    float currentT = tRange.Min + (tRange.Max - tRange.Min) * (j / (float)resolution);
                    if (!spline.Evaluate(currentT, out var pos, out _, out _)) continue;
                    if (!GetEvaluatePoint(pos, out var newPoint)) continue;
                    
                    if (point > newPoint)
                    {
                        t = currentT;
                        position = pos;
                        point = newPoint;
                    }
                }

                if (t < 0) return false;

                float tRangeIntervalHalf = (tRange.Max - tRange.Min) * 0.5f;
                tRange = new(t - tRangeIntervalHalf, t + tRangeIntervalHalf);
            }

            return point < float.MaxValue;
        }

        void DisplayTargetUI(bool display, Vector3 worldPos)
        {
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            if (!display || screenPos.z < 0)
            {
                _targetUI.gameObject.SetActive(false);
                return;
            }
            
            _targetUI.gameObject.SetActive(true);
            _targetUI.position = screenPos;
        }

        void DisplayWire(Vector3 otherPos)
        {
            _wireLine.SetPosition(0, _handSocket.position);
            _wireLine.SetPosition(1, otherPos);
        }

        async UniTask PlayClipAndWait(AnimationClip clip)
        {
            _clipPlayer.PlayClip(clip);
            await UniTask.Delay(TimeSpan.FromSeconds(clip.length), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        private enum AbilityStateType
        {
            Ready,
            Active,
            Cooldown
        }

        private enum GrappleStateType
        {
            Shot,
            ShotWait,
            PreJump,
            Jumping,
            Landing
        }

        [System.Serializable]
        public struct MinMaxRange 
        {
            public MinMaxRange(float min, float max)
            {
                Min  = min;
                Max = max;
            }
            
            public float Min;
            public float Max;

#if UNITY_EDITOR
            [CustomPropertyDrawer(typeof(MinMaxRange))]
            public class MinMaxRangeDrawer : PropertyDrawer
            {
                public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
                {
                    var minProp = property.FindPropertyRelative("Min");
                    var maxProp = property.FindPropertyRelative("Max");
                    
                    EditorGUI.BeginProperty(position, label, property);
                    
                    position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
                    
                    float[] values = { minProp.floatValue, maxProp.floatValue };
                    EditorGUI.MultiFloatField(position, new[]{ new GUIContent("Min"), new GUIContent("Max")}, values);
                    minProp.floatValue = values[0];
                    maxProp.floatValue = values[1];
                    
                    EditorGUI.EndProperty();
                }
            }
#endif
        }
    }
}