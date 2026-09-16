using Fusion;
using UnityEngine;

namespace InGame.TreasureChest
{
    [System.Serializable]
    public class ItemScatterEvent : TreasureChestEventBase
    {
        [SerializeField] private NetworkObject _itemPrefab;
        [SerializeField] private float _range;
        [SerializeField] private int _count;

        public override void Trigger(TriggerContext context)
        {
            for (int i = 0; i < _count; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * _range;

                Vector3 position = context.CenterPosition;
                position.x += randomOffset.x;
                position.z += randomOffset.y;

                context.Runner.Spawn(_itemPrefab, position);
            }
        }
    }
}
