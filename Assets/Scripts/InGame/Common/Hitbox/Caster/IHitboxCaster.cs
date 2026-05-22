using System;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Caster
{
    /// <summary>
    /// ヒットボックスを使用したヒット処理を提供します
    /// </summary>
    public interface IHitboxCaster
    {
        /// <summary>
        /// ヒット時に呼び出される処理
        /// </summary>
        public event Action<Collider> OnHit;

        /// <summary>
        /// ヒットボックスによるヒット検出処理を開始します
        /// </summary>
        public void StartCast();
    }
}
