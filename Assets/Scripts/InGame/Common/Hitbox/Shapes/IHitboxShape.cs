using System;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Shapes
{
    public interface IHitboxShape
    {
        public object Hitbox { get; }
        public void CastHitbox(Matrix4x4 baseMatrix, Collider[] results, Action<Collider> onHit);
    }
    
    public interface IHitboxShape<out TShape> : IHitboxShape
    {
        public new TShape Hitbox { get; }
        object IHitboxShape.Hitbox => Hitbox;
    }
}
