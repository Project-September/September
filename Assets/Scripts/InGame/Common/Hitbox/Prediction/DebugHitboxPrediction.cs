using Fusion;
using September.InGame.Common.Hitbox.ShapeStructs;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Prediction
{
    public class DebugHitboxPrediction : IBoxPrediction, IHitboxPredictionGizmo<BoxHitbox>
    {
        public void StartPrediction(BoxHitbox box, Matrix4x4 baseMatrix, int durationTick, NetworkRunner runner)
        {
            var center = box.Center + baseMatrix.GetPosition();
            var rotation = box.Rotation * baseMatrix.rotation;
            DebugDrawUtility.DrawOrientedWireBox(center, box.HalfExtents, rotation, Color.red, durationTick / runner.DeltaTime);
        }

        public void DrawGizmos(Matrix4x4 matrix, BoxHitbox shape)
        {
            Gizmos.color = new Color(1f, 0.92f, 0.02f, 0.31f);
            Gizmos.matrix = matrix * Matrix4x4.Translate(shape.Center) * Matrix4x4.Rotate(shape.Rotation);
            Gizmos.DrawCube(Vector3.zero, shape.HalfExtents);
            Gizmos.DrawWireCube(Vector3.zero, shape.HalfExtents);
        }
    }
}
