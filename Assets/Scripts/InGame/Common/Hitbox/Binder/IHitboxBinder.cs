using System;
using Fusion;
using September.InGame.Common.Hitbox.Hitboxes;
using September.InGame.Common.Hitbox.Prediction;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Binder
{
    /// <summary>
    /// ヒットボックスモジュール同士に同一の形状を使用させます。
    /// </summary>
    public interface IHitboxBinder
    {
        /// <summary>
        /// バインドが正常であるか検証します。
        /// </summary>
        public bool Validate();

        public void StartPrediction(Vector3 basePosition, Quaternion baseRotation, int durationTick, NetworkRunner runner);
        public void CastHitbox(Vector3 basePosition, Quaternion baseRotation, Collider[] results, Action<Collider> onHit);
        public void DrawGizmos(Vector3 basePosition, Quaternion baseRotation);
    }

    /// <summary>
    /// 具体的な形状に基づくバインドを提供します。
    /// </summary>
    /// <remarks>
    /// 形状毎の実装を簡易化するためのインターフェースです。
    /// </remarks>
    /// <typeparam name="TShape"> 形状を表すデータ型 </typeparam>
    /// <typeparam name="THitbox"> 形状に基づくヒット処理を提供する型 </typeparam>
    /// <typeparam name="TPrediction"> 形状に基づく予測表示を提供する型 </typeparam>
    public interface IHitboxBinder<TShape, out THitbox, out TPrediction> : IHitboxBinder
        where THitbox : IHitbox<TShape>
        where TPrediction : IHitboxPrediction<TShape>
    {
        protected THitbox[] Shapes { get; }
        protected TPrediction Prediction { get; }
        protected LayerMask BaseLayerMask { get; }

        bool IHitboxBinder.Validate()
        {
            foreach (var shape in Shapes)
            {
                if (!Prediction.IsShapeAvailable(shape.Shape))
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
                Prediction.StartPrediction(shape.Shape, basePosition, baseRotation, durationTick, runner);
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
            if (Prediction is not IHitboxPredictionGizmo<TShape> gizmo) return;
            
            foreach (var shape in Shapes)
            {
                gizmo.DrawGizmos(shape.Shape, basePosition, baseRotation);
            }
        }
    }
}
