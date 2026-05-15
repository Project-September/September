using UnityEngine;

namespace September.InGame.Kraken
{
    public interface IHitboxPredictionGizmo
    {
        public void DrawGizmos(Vector3 basePosition, Quaternion baseRotation, object shape);
    }
    
    public interface IHitboxPredictionGizmo<in TShape> : IHitboxPredictionGizmo
    {
        public void DrawGizmos(TShape shape, Vector3 basePosition, Quaternion baseRotation);

        void IHitboxPredictionGizmo.DrawGizmos(Vector3 basePosition, Quaternion baseRotation, object shape)
        {
            if (shape is TShape typedShape)
            {
                DrawGizmos(typedShape, basePosition, baseRotation);
            }
        }
    }
}