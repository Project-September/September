using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Splines;

namespace InGame.Exhibit.InteractEffect
{
    public class CarInteractable : NetworkBehaviour
    {
        [Header("Spline")]
        [SerializeField] private SplineContainer _spline;

        [Header("Move")]
        [SerializeField] private float _speed = 6f;
        [SerializeField] private Transform _target;
        
        [SerializeField] private bool _loop = false;

        [Header("減速設定")] 
        [SerializeField] private float _delayTime = 0.3f;
        [SerializeField] private float _slowdownRadius = 3f;
        [SerializeField] private float _minSpeedFactor = 0.25f;
        [SerializeField] private AnimationCurve _slowdownCurve;

        private float _delayRemaining;
        private int _lastDelayKnotIndex = -1;

        [Networked] private bool IsMoving { get; set; }
        [Networked] private float Progress {get; set;}
        private float _approxCount;

        private readonly List<Vector3> _knotWorldPositions = new();

        public override void Render()
        {
            if(_spline == null || _target == null)
                return;
            
            ApplyPose(Progress);
        }

        public override void Spawned()
        {
            if (_spline != null)
                _approxCount = ApproxLength(_spline, 200);
            CacheKnotWorldPositions();
        }

        // 位置の同期がされていない＆ホストしかインタラクトが実行されない
        public override void FixedUpdateNetwork()
        {
            if(!Object.HasStateAuthority || !IsMoving)
                return;
            
            Move();
        }

        [Rpc]
        public void RPC_OnInteractStart()
        {
            OnInteractStart();
        }

        private void OnInteractStart()
        {
            IsMoving = true;
            Progress = 0f;
        }

        private void Move()
        {
            if (_approxCount <= 0f)
                return;

            Spline spline = _spline.Spline;
            // Spline 上の現在位置と姿勢を算出する
            spline.Evaluate(Progress, out var localPos, out var localTan, out var localUp);

            Vector3 worldPos = _spline.transform.TransformPoint(localPos);

            if (_delayRemaining > 0f)
            {
                _delayRemaining -= Runner.DeltaTime;
                if (_delayRemaining < 0f) _delayRemaining = 0f;
                
                //ApplyPose(Progress);
                return;
            }

            float slowFactor = 1f;
            int nearestIndex = -1;
            float nearestDist = float.MaxValue;

            if (_knotWorldPositions.Count > 0 && _slowdownRadius > 0f)
            {
                // 最も近いノットを調べる
                for (int i = 0; i < _knotWorldPositions.Count; i++)
                {
                    float d = Vector3.Distance(worldPos, _knotWorldPositions[i]);
                    if (d < nearestDist)
                    {
                        nearestDist = d;
                        nearestIndex = i;
                    }
                }

                if (nearestDist <= _slowdownRadius)
                {
                    float x = Mathf.InverseLerp(0f, _slowdownRadius, nearestDist);
                    float curve = _slowdownCurve != null ? Mathf.Clamp01(_slowdownCurve.Evaluate(x)) : x;
                    slowFactor = Mathf.Lerp(_minSpeedFactor, 1f, curve);
                    
                    if (nearestIndex != -1 && nearestIndex != _lastDelayKnotIndex && nearestDist <= 0.2f)
                    {
                        _delayRemaining = _delayTime;
                        _lastDelayKnotIndex = nearestIndex;
                    }
                }
            }
            
            Progress += Runner.DeltaTime * _speed * slowFactor / _approxCount;

            // Stop処理
            if (Progress >= 1f)
            {
                if (_loop)
                    Progress -= 1f;
                else
                {
                    Progress = 1f;
                    IsMoving = false;
                }
            }

            // Transformに反映
            ApplyPose(Progress);
        }

        private void ApplyPose(float t)
        {
            Spline spline = _spline.Spline;
            spline.Evaluate(t, out var lp, out var lt, out var lu);

            Vector3 pos = _spline.transform.TransformPoint(lp);
            Vector3 tan = _spline.transform.TransformDirection(lt).normalized;
            Vector3 up = _spline.transform.TransformDirection(lu).normalized;

            if (tan.sqrMagnitude < 1e-6f)
                tan = _target.forward;
            if (up.sqrMagnitude < 1e-6f)
                up = Vector3.up;

            _target.SetPositionAndRotation(pos, Quaternion.LookRotation(tan, up));
        }

        // 点を取得してワールド座標に変換
        private void CacheKnotWorldPositions()
        {
            _knotWorldPositions.Clear();
            Spline spline = _spline.Spline;

            foreach (BezierKnot knot in spline)
            {
                Vector3 wp = _spline.transform.TransformPoint(knot.Position);
                _knotWorldPositions.Add(wp);
            }
        }

        // Splineの長さを取得する
        private static float ApproxLength(SplineContainer container, int samples)
        {
            if (container == null || container.Spline == null)
                return 0f;

            var spline = container.Spline;

            samples = Mathf.Max(2, samples);
            Vector3 prev = container.transform.TransformPoint(spline.EvaluatePosition(0f));
            float len = 0f;

            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector3 p = container.transform.TransformPoint(spline.EvaluatePosition(t));
                len += Vector3.Distance(prev, p);
                prev = p;
            }

            return len;
        }
    }
}