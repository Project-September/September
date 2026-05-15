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

        public void CastHitbox(Vector3 basePosition, Quaternion baseRotation, Collider[] results, Action<Collider> onHit)
        {
            var castPosition = _hitboxShape.GetWorldCenter(basePosition, baseRotation);
            var castRotation = _hitboxShape.GetWorldRotation(baseRotation);
            
            int hitCount = Physics.OverlapBoxNonAlloc(castPosition, _hitboxShape.HalfExtents, results, castRotation);

            for (int i = 0; i < hitCount; i++)
            {
                onHit?.Invoke(results[i]);
            }
        }
    }
}