using Fusion;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Prediction
{
    public interface IHitboxPrediction
    {
        public void StartPrediction(object box, Matrix4x4 baseMatrix, int durationTick, NetworkRunner runner);
        public bool IsShapeAvailable(object shape);
    }
    
    public interface IHitboxPrediction<in TShape> : IHitboxPrediction
    {
        public void StartPrediction(TShape box, Matrix4x4 baseMatrix, int durationTick, NetworkRunner runner);

        void IHitboxPrediction.StartPrediction(object box, Matrix4x4 baseMatrix, int durationTick, NetworkRunner runner)
        {
            StartPrediction((TShape)box, baseMatrix, durationTick, runner);
        }

        bool IHitboxPrediction.IsShapeAvailable(object shape)
        {
            return shape is TShape;
        }
    }
}
