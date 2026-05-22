using Fusion;
using September.InGame.Common.Hitbox.Shapes;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Prediction
{
    public class DebugBoxHitboxPrediction : IBoxPrediction, IHitboxPredictionGizmo<Box>
    {
        public void StartPrediction(Box shape, Vector3 basePosition, Quaternion baseRotation, int durationTick,
            NetworkRunner runner)
        {
            var center = shape.GetWorldCenter(basePosition, baseRotation);
            var rotation = shape.GetWorldRotation(baseRotation);
            DebugDrawUtility.DrawOrientedWireBox(center, shape.HalfExtents, rotation, Color.yellow, durationTick * runner.DeltaTime);
        }

        public void DrawGizmos(Box shape, Vector3 basePosition, Quaternion baseRotation)
        {
            Gizmos.color = new Color(1f, 0.92f, 0.02f, 0.31f);

            var center = shape.GetWorldCenter(basePosition, baseRotation);
            var rotation = shape.GetWorldRotation(baseRotation);

            Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, shape.HalfExtents);
            Gizmos.DrawWireCube(Vector3.zero, shape.HalfExtents);
        }
    }
}
