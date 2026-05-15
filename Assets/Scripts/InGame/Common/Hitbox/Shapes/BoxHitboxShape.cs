using System;
using September.InGame.Common.Hitbox.ShapeStructs;
using UnityEngine;

namespace September.InGame.Kraken
{
    [Serializable]
    public class BoxHitboxShape : IBoxHitbox
    {
        [SerializeField] private BoxHitbox _hitboxShape;
        
        public BoxHitbox Hitbox => _hitboxShape;

        public void CastHitbox(Matrix4x4 baseMatrix, Collider[] results, Action<Collider> onHit)
        {
            var hitboxMatrix = baseMatrix * _hitboxShape.GetMatrix();
            var castPosition = hitboxMatrix.GetPosition();
            var castRotation = hitboxMatrix.rotation;
            
            int hitCount = Physics.OverlapBoxNonAlloc(castPosition, _hitboxShape.HalfExtents, results, castRotation);

            for (int i = 0; i < hitCount; i++)
            {
                onHit?.Invoke(results[i]);
            }
        }
    }
}