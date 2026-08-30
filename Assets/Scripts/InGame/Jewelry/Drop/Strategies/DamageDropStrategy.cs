using System;
using System.Collections.Generic;
using Fusion;
using InGame.Health;
using InGame.Jewelry.Common;
using UnityEngine;

namespace September.InGame.Jewelry.Drop.Strategies
{
    [Serializable]
    public class DamageDropStrategy : IJewelryDropStrategy
    {
        [Header("一定ダメージ毎にドロップ")]
        [SerializeField] private int _requiredDamage = 20;
        private readonly Dictionary<PlayerRef, float> _attackerDealDamages = new();

        public int GetDropAmount(HitData hitData, JewelryType jewelryType, IJewelryContainer jewelryContainer)
        {
            _attackerDealDamages.TryAdd(hitData.ExecutorRef, 0);
            _attackerDealDamages[hitData.ExecutorRef] += hitData.Amount;

            if (_attackerDealDamages[hitData.ExecutorRef] < _requiredDamage) return 0;

            int dropAmount = Mathf.FloorToInt(_attackerDealDamages[hitData.ExecutorRef] / _requiredDamage);
            _attackerDealDamages[hitData.ExecutorRef] %= _requiredDamage;

            return dropAmount;
        }
    }
}