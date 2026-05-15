using System;
using UnityEngine;

namespace September.InGame.Kraken
{
    public interface IHitboxShape
    {
        public object Hitbox { get; }
        public void CastHitbox(Vector3 basePosition, Quaternion baseRotation, Collider[] results, LayerMask layerMask,
            Action<Collider> onHit);
    }
    
    public interface IHitboxShape<out TShape> : IHitboxShape
    {
        public new TShape Hitbox { get; }
        object IHitboxShape.Hitbox => Hitbox;
    }
}