using System;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Hitboxes
{
    /// <summary>
    /// ヒット判定処理を提供します
    /// </summary>
    public interface IHitbox
    {
        /// <summary>
        /// ヒットボックスの形状を表すデータ
        /// </summary>
        public object Shape { get; }

        /// <summary>
        /// この形状にヒットするオブジェクトが存在するか調べます
        /// </summary>
        /// <param name="basePosition">ヒットボックスの基準位置</param>
        /// <param name="baseRotation">ヒットボックスの基準回転</param>
        /// <param name="results">ヒットしたコライダーを格納する配列</param>
        /// <param name="layerMask">検出対象となるレイヤー</param>
        /// <param name="onHit">ヒット時に呼び出されるコールバック</param>
        public void CastHitbox(Vector3 basePosition, Quaternion baseRotation, Collider[] results, LayerMask layerMask,
            Action<Collider> onHit);
    }

    /// <summary>
    /// 形状に合わせたヒット判定処理を提供します
    /// </summary>
    /// <typeparam name="TShape">ヒットボックスの形状を表すデータ型</typeparam>
    public interface IHitbox<out TShape> : IHitbox
    {
        /// <summary>
        /// ヒットボックスの形状を表すデータ
        /// </summary>
        public new TShape Shape { get; }
        object IHitbox.Shape => Shape;
    }
}
