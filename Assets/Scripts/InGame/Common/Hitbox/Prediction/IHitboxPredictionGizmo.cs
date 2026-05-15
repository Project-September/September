using UnityEngine;

namespace September.InGame.Kraken
{
    public interface IHitboxPredictionGizmo
    {
        public void DrawGizmos(Matrix4x4 matrix, object shape);
    }
    
    public interface IHitboxPredictionGizmo<in TShape> : IHitboxPredictionGizmo
    {
        public void DrawGizmos(Matrix4x4 matrix, TShape shape);

        void IHitboxPredictionGizmo.DrawGizmos(Matrix4x4 matrix, object shape)
        {
            if (shape is TShape typedShape)
            {
                DrawGizmos(matrix, typedShape);
            }
        }
    }
}