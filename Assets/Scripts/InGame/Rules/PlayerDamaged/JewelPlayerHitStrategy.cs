using System;
using InGame.Health;
using September.InGame.Jewelry.Drop;
using UnityEngine;

namespace September.InGame.Rules.PlayerDamaged
{
    [Serializable]
    public class JewelPlayerHitStrategy : IPlayerHitStrategy
    {
        [SerializeField] private JewelryDropHitProcessor _defaultDamageProcessor = new(DropType.Drop);
        [SerializeField] private JewelryDropHitProcessor _rangedDamageProcessor = new(DropType.Pickup);

        public void OnHitTaken(ref HitData hitData)
        {
            switch (hitData.HitActionType)
            {
                case HitActionType.Damage:
                    _defaultDamageProcessor.OnHitTaken(hitData);
                    break;
                case HitActionType.RangedDamage:
                    _rangedDamageProcessor.OnHitTaken(hitData);
                    break;
                case HitActionType.Custom:
                    hitData.CustomHitProcessor.OnHitTaken(hitData);
                    break;
            }
        }
    }
}
