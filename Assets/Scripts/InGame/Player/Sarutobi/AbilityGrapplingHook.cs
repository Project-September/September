using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Common;
using September.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

namespace InGame.Player.Sarutobi
{
    public class AbilityGrapplingHook : NetworkBehaviour, IAfterTick
    {
        [Header("Ability")]
        [SerializeField] private GameObject _targetUIPrefab;
        [SerializeField] private float _cooldown;
        [Header("Grappling Hook")]
        [SerializeField] private MinMaxRange _distanceRange;
        [SerializeField] private float _maxAngle;
        [SerializeField] private float _distanceReflectionRate;
        [SerializeField] private float _angleReflectionRate;
        [SerializeField] private float _wireSpeed;
        [Header("Jump")]
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
        [SerializeField] private Material _wireMaterial;
        [SerializeField] private float _wireWidth;

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
        private float _wireTimer;
        private Vector3 _targetPosition;
        private Vector3 _startPosition;
        private float _distanceMag;
        private NetworkButtons PreviousButtons { get; set; }
        
        [Networked] private AbilityStateType AbilityState { get; set; } = AbilityStateType.Ready;
        [Networked, HideInInspector] public TickTimer Cooldown { get; set; }

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
                _playerMovement = GetComponent<PlayerMovement>();
                _grappleableSpline = GrapplingSpline.I.GrapplingTargetSpline;
                _targetUI = Instantiate(_targetUIPrefab).GetComponentInChildren<Image>().transform;
                _mainCamera = Camera.main;
            }
            
            var wireObject = new GameObject("GrapplingWire");
            _wireLine = wireObject.AddComponent<LineRenderer>();
            _wireLine.material = _wireMaterial;
            _wireLine.startWidth = _wireWidth;
            _wireLine.endWidth = _wireWidth;
            _wireLine.enabled = false;
        }

        public override void FixedUpdateNetwork()
        {
            GetInput<PlayerInput>(out var input);
            
            // input authority で判定
            if (HasInputAuthority)
            {
                _targetUI.gameObject.SetActive(false);
                
                // Abilityの状態と入力受付がされているときに判定に入る
                if (AbilityState == AbilityStateType.Ready && GameInput.I.Player.Ability1.enabled)
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
                else if (AbilityState == AbilityStateType.Cooldown && Cooldown.ExpiredOrNotRunning(Runner))
                {
                    AbilityState = AbilityStateType.Ready;
                }
            }
        }

        public void AfterTick()
        {
            PreviousButtons = GetInput<PlayerInput>().GetValueOrDefault().Buttons;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        void RPC_GrappleStart(Vector3 targetPosition)
        {
            if (!HasStateAuthority)
            {
                _targetPosition = targetPosition + Vector3.up * 0.05f;
                _startPosition = transform.position;
                _distanceMag = Vector3.Distance(_startPosition, _targetPosition);
                return;
            }
            
            AbilityState = AbilityStateType.Active;
            _grappleState = GrappleStateType.Shot;
            _targetPosition = targetPosition + Vector3.up * 0.05f;
            _startPosition = transform.position;
            _distanceMag = Vector3.Distance(_startPosition, _targetPosition);
            _jumpTimer = 0;
            Shot().Forget();
            
            _playerManager.SetControlState(PlayerManager.PlayerControlState.ForcedControl);
            
            Cooldown = TickTimer.CreateFromSeconds(Runner, _cooldown);
            
            // ボーナスカウントを更新する
            PlayerDatabase db = PlayerDatabase.Instance;
            if (!db.PlayerDataDic.TryGet(Object.InputAuthority, out SessionPlayerData playerData)) 
                return;
            if (playerData.CharacterType != CharacterType.Sarutobi) 
                return;
            
            if (HasStateAuthority)
            {
                db.Server_AddGrapplingHook(Object.InputAuthority);
            }
        }

        async UniTask Shot()
        {
            var endType = await _clipPlayer.PlayClipAndWait(_animShot);

            if (endType != EndClipType.Complete)
            {
                GrappleEnd();
            }
            
            _grappleState = GrappleStateType.ShotWait;
            _clipPlayer.PlayClip(_animShotWait);
            RPC_DisplayWireStart();
        }

        void ShotWaitTick()
        {
            _jumpTimer += Runner.DeltaTime;
                    
            if (_jumpTimer >= _distanceMag / _wireSpeed)
            {
                _grappleState = GrappleStateType.PreJump;
                _jumpTimer = 0;
                _clipPlayerManager.EnableFallMotion = false;

                PreJump().Forget();
            }
        }

        async UniTask PreJump()
        {
            var endType = await _clipPlayer.PlayClipAndWait(_animMoveStart);

            if (endType != EndClipType.Complete)
            {
                GrappleEnd();
            }
            
            _grappleState = GrappleStateType.Jumping;
            _clipPlayer.PlayClip(_animMoveLoop);
        }

        void JumpingTick()
        {
            _jumpTimer += Runner.DeltaTime;
            float t = Math.Clamp(_jumpTimer * _pullingSpeed / _distanceMag, 0, 1);
            
            transform.position = Vector3.Lerp(_startPosition, _targetPosition, t);

            if (t >= 1)
            {
                _grappleState = GrappleStateType.Landing;
                _jumpTimer = 0;
                _playerMovement.KnockBack(transform.rotation * _pullLastForce, 0.2f).Forget();
                RPC_DisplayWireEnd();
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
                GrappleEnd();
            }
        }

        void GrappleEnd()
        {
            AbilityState = AbilityStateType.Cooldown;
            _playerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
            _jumpTimer = 0;
            RPC_DisplayWireEnd();
            _clipPlayerManager.EnableFallMotion = true;
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
            
            // 障害物判定　カプセルの中心から同じRadiusの球でTargetまでCast
            Vector3 halfHeight = (_playerMovement.MoveCapsuleCollider.height * 0.5f + _playerMovement.MoveCapsuleCollider.radius) * Vector3.up;
            
            if (Physics.CheckCapsule(transform.position + halfHeight, position + halfHeight, _playerMovement.MoveCapsuleCollider.radius, _playerMovement.GroundLayer))
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

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        void RPC_DisplayWireStart()
        {
            _wireLine.enabled = true;
            _wireTimer = 0;
        }

        private void Update()
        {
            if (_wireLine.enabled)
            {
                _wireTimer += Time.deltaTime;
                float t = Math.Clamp(_wireTimer * _wireSpeed / _distanceMag, 0, 1);
                SetWirePosition(Vector3.Lerp(_startPosition, _targetPosition, t));
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        void RPC_DisplayWireEnd()
        {
            _wireLine.enabled = false;
        }

        void SetWirePosition(Vector3 otherPos)
        {
            _wireLine.SetPosition(0, _handSocket.position);
            _wireLine.SetPosition(1, otherPos);
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