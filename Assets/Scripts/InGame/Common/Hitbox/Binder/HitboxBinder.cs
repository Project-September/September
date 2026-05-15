using System;
using Fusion;
using September.InGame.Kraken;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Binder
{
    [Serializable]
    public class HitboxBinder
    {
        [SerializeReference, SubclassSelector] private IHitboxShape[] _shapes;
        [SerializeReference, SubclassSelector] private IHitboxPrediction _prediction;

        public bool Validate()
        {
            foreach (var shape in _shapes)
            {
                if (!_prediction.IsShapeAvailable(shape.Hitbox))
                {
                    return false;
                }
            }
            
            return true;
        }

        public void StartPrediction(Matrix4x4 baseMatrix, int durationTick, NetworkRunner runner)
        {
            foreach (var shape in _shapes)
            {
                _prediction.StartPrediction(shape, baseMatrix, durationTick, runner);
            }
        }

        public void CastHitbox(Matrix4x4 baseMatrix, Collider[] results, Action<Collider> onHit)
        {
            foreach (var shape in _shapes)
            {
                shape.CastHitbox(baseMatrix, results, onHit);
            }
        }

        public void DrawGizmos(Matrix4x4 baseMatrix)
        {
            if (_prediction is not IHitboxPredictionGizmo gizmo) return;
            
            foreach (var shape in _shapes)
            {
                gizmo.DrawGizmos(baseMatrix, shape.Hitbox);
            }
        }
    }
}