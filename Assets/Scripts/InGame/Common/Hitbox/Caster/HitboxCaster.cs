using System;
using System.Collections.Generic;
using Fusion;
using September.InGame.Common.Hitbox.Binder;
using September.InGame.Common.Hitbox.ShapeStructs;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Caster
{
    public class HitboxCaster : NetworkBehaviour, IHitboxCaster 
    {
        [SerializeReference, SubclassSelector] private IHitboxBinder[] _binds;
        
        [SerializeField] private HitboxTicks _hitboxTicks;

        private int _startExecuteTick = -9999;
        private bool _isActive;
        
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        
        private readonly Collider[] _hitColliders = new Collider[32];
        private readonly List<Collider> _alreadyHitColliders = new();
        
        public event Action<Collider> OnHit;

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (!_isActive) return;
            
            Debug.Log("Tick");
            
            if (_hitboxTicks.IsStartPredictionTick(Runner.Tick, _startExecuteTick))
            {
                Debug.Log("Start Prediction Tick");
                _isActive = true;
                StartPrediction(_hitboxTicks.PredictionDurationTick);
                return;
            }

            if (_hitboxTicks.IsInHitboxExecuteTick(Runner.Tick, _startExecuteTick))
            {
                Debug.Log("Cast Hitbox Tick");
                _isActive = true;
                CastHitbox();
                return;
            }

            if (_hitboxTicks.IsEnded(Runner.Tick, _startExecuteTick))
            {
                _isActive = false;
                _alreadyHitColliders.Clear();
            }
        }

        public void StartCast()
        {
            if (_isActive) return;
            
            _startExecuteTick = Runner.Tick;
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            _isActive = true;
        }

        public void StartPrediction(int durationTick)
        {
            foreach (var bind in _binds)
            {
                bind.StartPrediction(_startPosition, _startRotation, durationTick, Runner);
            }
        }

        public void CastHitbox()
        {
            foreach (var bind in _binds)
            {
                bind.CastHitbox(_startPosition, _startRotation, _hitColliders, hitCollider =>
                {
                    if (_alreadyHitColliders.Contains(hitCollider)) return;
                    OnHit?.Invoke(hitCollider);
                    _alreadyHitColliders.Add(hitCollider);
                });
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying) return;
            if (!enabled) return;
            if (_binds == null) return;

            Gizmos.color = new Color(1f, 0.92f, 0.02f, 0.31f);
            
            foreach (var bind in _binds)
            {
                bind?.DrawGizmos(transform.position, transform.rotation);
            }
        }
    }
}
