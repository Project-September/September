using System;
using September.InGame.Common.Hitbox.Shapes;
using UnityEngine;

namespace September.InGame.Common.Hitbox.Hitboxes
{
    [Serializable]
    public class BoxHitbox : IBoxHitbox
    {
        [SerializeField] private Box _shape;
        
        public Box Shape => _shape;

        public void CastHitbox(Vector3 basePosition, Quaternion baseRotation, Collider[] results, LayerMask layerMask, Action<Collider> onHit)
        {
            var castPosition = _shape.GetWorldCenter(basePosition, baseRotation);
            var castRotation = _shape.GetWorldRotation(baseRotation);
            
            HitboxDebugUtility.DrawBoxOneFrame(castPosition, _shape.HalfExtents, castRotation, Color.red);
            
            int hitCount = Physics.OverlapBoxNonAlloc(castPosition, _shape.HalfExtents, results, castRotation, layerMask);

            if (hitCount >= results.Length)
            {
                Debug.LogWarning($"hitCountがバッファの上限数({results.Length})に到達しました: {hitCount}。一部のコライダーが検出されていない可能性があります。検出対象となるレイヤーを減らすか、バッファサイズを増やしてください");
            }

            for (int i = 0; i < hitCount; i++)
            {
                HitboxDebugUtility.DrawCollider(results[i], Color.magenta);
                onHit?.Invoke(results[i]);
            }
        }
    }
}
