using System;
using Fusion;
using September.InGame.Common.Hitbox.Prediction;
using September.InGame.Common.Hitbox.Shapes;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Binder
{
    public interface IHitboxBinder
    {
        public bool Validate();
        public void StartPrediction(Matrix4x4 baseMatrix, int durationTick, NetworkRunner runner);
        public void CastHitbox(Matrix4x4 baseMatrix, Collider[] results, Action<Collider> onHit);
        public void DrawGizmos(Matrix4x4 baseMatrix);
    }
    
    public interface IHitboxBinder<TShapeStruct, THitboxShape, out TPrediction> : IHitboxBinder
        where THitboxShape : IHitboxShape<TShapeStruct>
        where TPrediction : IHitboxPrediction<TShapeStruct>
    {
        protected THitboxShape[] Shapes { get; }
        protected TPrediction Prediction { get; }
        
        bool IHitboxBinder.Validate()
        {
            foreach (var shape in Shapes)
            {
                if (!Prediction.IsShapeAvailable(shape.Hitbox))
                {
                    return false;
                }
            }
            
            return true;
        }

        void IHitboxBinder.StartPrediction(Matrix4x4 baseMatrix, int durationTick, NetworkRunner runner)
        {
            foreach (var shape in Shapes)
            {
                Prediction.StartPrediction(shape, baseMatrix, durationTick, runner);
            }
        }

        void IHitboxBinder.CastHitbox(Matrix4x4 baseMatrix, Collider[] results, Action<Collider> onHit)
        {
            foreach (var shape in Shapes)
            {
                shape.CastHitbox(baseMatrix, results, onHit);
            }
        }

        void IHitboxBinder.DrawGizmos(Matrix4x4 baseMatrix)
        {
            if (Prediction is not IHitboxPredictionGizmo gizmo) return;
            
            foreach (var shape in Shapes)
            {
                gizmo.DrawGizmos(baseMatrix, shape.Hitbox);
            }
        }
    }
}
