using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace September.InGame.Kraken
{
    public class HitboxCaster : NetworkBehaviour, IHitboxCaster
    {
        [SerializeField] private Hitbox _hitbox;
        [SerializeField] private HitboxTicks _hitboxTicks;
        [SerializeReference, SubclassSelector] private IHitboxPrediction _hitboxPrediction;

        private Vector3 _castPosition;
        private Quaternion _castRotation;
        private int _startExecuteTick = -9999;
        private bool _isActive;
        
        private Collider[] _hitColliders = new Collider[32];
        private List<Collider> _alreadyHitColliders = new();
        
        public event Action<Collider> OnHit;

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (!_isActive) return;
            
            if (_hitboxTicks.IsStartPredictionTick(Runner.Tick, _startExecuteTick))
            {
                _isActive = true;
                StartPrediction(_hitboxTicks.PredictionDurationTick);
                return;
            }

            if (_hitboxTicks.IsInHitboxExecuteTick(Runner.Tick, _startExecuteTick))
            {
                _isActive = true;
                CastHitbox();
                return;
            }

            _isActive = false;
            _alreadyHitColliders.Clear();
        }

        public void StartCast()
        {
            _startExecuteTick = Runner.Tick;
            _isActive = true;
            _castPosition = _hitbox.GetWorldCenter(transform);
            _castRotation = transform.rotation;
        }

        public void StartPrediction(int durationTick)
        {
            _hitboxPrediction.StartPrediction(_hitbox, _castPosition, _castRotation, durationTick, Runner);
        }

        public void CastHitbox()
        {
            int hitCount = Physics.OverlapBoxNonAlloc(_castPosition, _hitbox.HalfExtents, _hitColliders, _castRotation);

            for (int i = 0; i < hitCount; i++)
            {
                if (_alreadyHitColliders.Contains(_hitColliders[i])) return;
                OnHit?.Invoke(_hitColliders[i]);
                _alreadyHitColliders.Add(_hitColliders[i]);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying) return;
            
            Gizmos.color = new Color(1f, 0.92f, 0.02f, 0.31f);
            Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.Translate(_hitbox.Center) * Matrix4x4.Rotate(_hitbox.Rotation);
            Gizmos.DrawCube(Vector3.zero, _hitbox.HalfExtents);
            Gizmos.DrawWireCube(Vector3.zero, _hitbox.HalfExtents);
        }
    }

    public interface IHitboxShape
    {
        public void CastHitbox(Matrix4x4 transform, Collider[] results, Action<Collider> onHit);
    }

    [Serializable]
    public class BoxHitbox : IHitboxShape
    {
        [SerializeField] private Hitbox _hitbox;
        
        public void CastHitbox(Matrix4x4 transform, Collider[] results, Action<Collider> onHit)
        {
            var hitboxMatrix = transform * _hitbox.GetMatrix();
            var castPosition = hitboxMatrix.GetPosition();
            var castRotation = hitboxMatrix.rotation;
            
            int hitCount = Physics.OverlapBoxNonAlloc(castPosition, _hitbox.HalfExtents, results, castRotation);

            for (int i = 0; i < hitCount; i++)
            {
                onHit?.Invoke(results[i]);
            }
        }
    }

    public interface IHitboxPrediction
    {
        public void StartPrediction(Hitbox box, Vector3 offset, Quaternion baseRotation, int durationTick, NetworkRunner runner);
    }

    public class DebugHitboxPrediction : IHitboxPrediction
    {
        public void StartPrediction(Hitbox box, Vector3 offset, Quaternion baseRotation, int durationTick, NetworkRunner runner)
        {
            var center = box.Center + offset;
            var rotation = box.Rotation * baseRotation;
            DebugDrawUtility.DrawOrientedWireBox(center, box.HalfExtents, rotation, Color.red, durationTick / runner.DeltaTime);
        }
    }

    public interface IHitboxCaster
    {
        public void StartCast();
        public void StartPrediction(int durationTick);
        public void CastHitbox();
    }

    [Serializable]
    public struct Hitbox
    {
        [SerializeField] private Vector3 _center;
        [SerializeField] private Vector3 _halfExtents;
        [SerializeField] private Quaternion _rotation;
        
        public Vector3 Center => _center;
        public Vector3 HalfExtents => _halfExtents;
        public Quaternion Rotation => _rotation;

        public Vector3 GetWorldCenter(Transform root)
        {
            return root.TransformPoint(_center);
        }

        public Matrix4x4 GetMatrix()
        {
            return Matrix4x4.TRS(_center, _rotation, _halfExtents * 2);
        }
    }

    [Serializable]
    public struct HitboxTicks
    {
        [SerializeField] private int _startPredictionTick;
        [SerializeField] private int _predictionDurationTick;
        [SerializeField] private int _hitboxDurationTick;
        
        public int PredictionDurationTick => _predictionDurationTick;
        public int HitboxDurationTick => _hitboxDurationTick;
        
        public int GetStartPredictionTick(int startExecuteTick) => _startPredictionTick + startExecuteTick;
        public int GetStartHitboxTick(int startExecuteTick) => _predictionDurationTick + _startPredictionTick + startExecuteTick;
        public int GetEndTick(int startExecuteTick) => _hitboxDurationTick + _predictionDurationTick + _startPredictionTick + startExecuteTick;

        public bool IsStartPredictionTick(int currentTick, int startExecuteTick) => currentTick == _startPredictionTick + startExecuteTick;
        public bool IsInHitboxExecuteTick(int currentTick, int startExecuteTick) =>
            currentTick >= GetStartHitboxTick(startExecuteTick) &&
            currentTick <= GetEndTick(startExecuteTick);
    }
}