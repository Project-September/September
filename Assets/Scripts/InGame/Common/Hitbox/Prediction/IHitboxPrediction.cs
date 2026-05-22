using Fusion;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Prediction
{
    /// <summary>
    /// ヒットボックスの範囲を表示する機能を提供する
    /// </summary>
    public interface IHitboxPrediction
    {
        public void StartPrediction(object box, Vector3 basePosition, Quaternion baseRotation, int durationTick, NetworkRunner runner);
        public bool IsShapeAvailable(object shape);
    }

    /// <summary>
    /// ヒットボックスの範囲を表示する機能を提供する
    /// </summary>
    public interface IHitboxPrediction<in TShape> : IHitboxPrediction
    {
        public void StartPrediction(TShape shape, Vector3 basePosition, Quaternion baseRotation, int durationTick, NetworkRunner runner);

        void IHitboxPrediction.StartPrediction(object box, Vector3 basePosition, Quaternion baseRotation, int durationTick,
            NetworkRunner runner)
        {
            StartPrediction((TShape)box, basePosition, baseRotation, durationTick, runner);
        }

        bool IHitboxPrediction.IsShapeAvailable(object shape)
        {
            return shape is TShape;
        }
    }
}
