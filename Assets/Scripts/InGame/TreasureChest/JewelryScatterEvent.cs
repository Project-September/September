using UnityEngine;
using InGame.Jewelry;

namespace InGame.TreasureChest
{
    [System.Serializable ]
    public class JewelryScatterEvent : TreasureChestEventBase
    {
        [SerializeField] JewelrySpawnSetting _jewelrySpawnSetting;
        [SerializeField] private JewelrySpawner _jewelrySpawner;

        public override void Trigger(TriggerContext context)
        {
            _jewelrySpawner.SpawnJewelryGroup(context.CenterPosition, _jewelrySpawnSetting);
        }
    }
}
