using System;
using Fusion;
using September.InGame.Kraken;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Binder
{
    public interface IHitboxBinder
    {
        public bool Validate();
        public void StartPrediction(Vector3 basePosition, Quaternion baseRotation, int durationTick, NetworkRunner runner);
        public void CastHitbox(Vector3 basePosition, Quaternion baseRotation, Collider[] results, Action<Collider> onHit);
        public void DrawGizmos(Vector3 basePosition, Quaternion baseRotation);
    }
    
    public interface IHitboxBinder<TShapeStruct, out THitboxShape, out TPrediction> : IHitboxBinder
        where THitboxShape : IHitboxShape<TShapeStruct>
        where TPrediction : IHitboxPrediction<TShapeStruct>
    {
        protected THitboxShape[] Shapes { get; }
        protected TPrediction Prediction { get; }
        protected LayerMask BaseLayerMask { get; }
        
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

        void IHitboxBinder.StartPrediction(Vector3 basePosition, Quaternion baseRotation, int durationTick, NetworkRunner runner)
        {
            foreach (var shape in Shapes)
            {
                Prediction.StartPrediction(shape.Hitbox, basePosition, baseRotation, durationTick, runner);
            }
        }

        void IHitboxBinder.CastHitbox(Vector3 basePosition, Quaternion baseRotation, Collider[] results, Action<Collider> onHit)
        {
            foreach (var shape in Shapes)
            {
                shape.CastHitbox(basePosition, baseRotation, results, BaseLayerMask, onHit);
            }
        }

        void IHitboxBinder.DrawGizmos(Vector3 basePosition, Quaternion baseRotation)
        {
            if (Prediction is not IHitboxPredictionGizmo<TShapeStruct> gizmo) return;
            
            foreach (var shape in Shapes)
            {
                gizmo.DrawGizmos(shape.Hitbox, basePosition, baseRotation);
            }
        }
    }
}