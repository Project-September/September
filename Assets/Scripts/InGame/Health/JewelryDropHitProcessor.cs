using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using InGame.Jewelry.Common;
using NaughtyAttributes;
using September.InGame.Jewelry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace September.InGame.Health
{
    [Serializable]
    public class JewelryDropSettings
    {
        [SerializeField] private JewelryType _jewelryType;
        public JewelryType JewelryType => _jewelryType;

        [Header("一定ダメージ毎にドロップ")]
        [SerializeField] private int _requiredDamage = 20;
        private readonly Dictionary<PlayerRef, float> _attackerDealDamages = new();

        [Header("ダメージに応じた確率ドロップ")]
        [SerializeField] private int _damageChancedDropRate = 20;
        [SerializeField, CurveRange(0f, 0f, 200f, 1f, EColor.Red)]
        private AnimationCurve _dropChanceDamageCurve = AnimationCurve.Linear(0, 0, 200, 1);
        [SerializeField, CurveRange(0f, 0f, 1f, 10f, EColor.Blue)]
        private AnimationCurve _dropChanceMinCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("所持数に応じた確率ドロップ")]
        [SerializeField, Label("最低保証個数"), AllowNesting] private int _containerMinDropCount = 1;
        [SerializeField, CurveRange(0f, 0f, 20f, 1f, EColor.Red)]
        private AnimationCurve _containerDropRate = AnimationCurve.Linear(0, 0, 11, 1);

        public int GetDropAmount(HitData hitData, IJewelryContainer jewelryContainer)
        {
            int damageDrop = GetDealDamageDropCount(hitData, jewelryContainer);
            int chanceDrop = GetChanceDropCount(hitData, jewelryContainer);
            int containerDrop = GetContainerDropCount(hitData, jewelryContainer);

            int dropAmount = damageDrop + chanceDrop + containerDrop;

            int jewelryCount = jewelryContainer.GetJewelryCount(_jewelryType);

            Debug.Log($"dropAmount:{dropAmount}, {damageDrop}, {chanceDrop}, {containerDrop}");

            return Mathf.Min(dropAmount, jewelryCount);
        }

        private int GetChanceDropCount(HitData hitData, IJewelryContainer jewelryContainer)
        {
            float t = _dropChanceDamageCurve.Evaluate(hitData.Amount);
            int minDrop = Mathf.FloorToInt(_dropChanceMinCurve.Evaluate(t));
            int dropAmount = Mathf.RoundToInt(Random.Range(minDrop, _damageChancedDropRate));
            return dropAmount;
        }

        private int GetContainerDropCount(HitData hitData, IJewelryContainer jewelryContainer)
        {
            int jewelryCount = jewelryContainer.CalculateJewelryScore();
            int dropAmount = Random.value <= _containerDropRate.Evaluate(jewelryCount) ? 1 : 0;
            return Mathf.Max(dropAmount, _containerMinDropCount);
        }

        private int GetDealDamageDropCount(HitData hitData, IJewelryContainer jewelryContainer)
        {
            _attackerDealDamages.TryAdd(hitData.ExecutorRef, 0);
            _attackerDealDamages[hitData.ExecutorRef] += hitData.Amount;

            if (_attackerDealDamages[hitData.ExecutorRef] < _requiredDamage) return 0;

            int dropAmount = Mathf.FloorToInt(_attackerDealDamages[hitData.ExecutorRef] / _requiredDamage);
            _attackerDealDamages[hitData.ExecutorRef] %= _requiredDamage;

            return dropAmount;
        }
    }

    [Serializable]
    public class JewelryDropHitProcessor : IHitProcessor
    {
        [SerializeField] private DropType _dropType;

        [Header("ドロップ数設定")]
        [SerializeField] private JewelryDropSettings[] _dropSettingsList;

        private readonly IJewelry[] _resultBuffer = new IJewelry[30];

        public DropType DropType { get => _dropType; set => _dropType = value; }

        public JewelryDropHitProcessor(DropType dropType)
        {
            DropType = dropType;
        }

        public void OnHitTaken(HitData hitData)
        {
            var runner = NetworkRunner.Instances[0];
            if (!runner) return;

            var targetObj = runner.GetPlayerObject(hitData.TargetRef);
            var jewelryContainer = targetObj.GetComponentInChildren<IJewelryContainer>();
            if (jewelryContainer == null) return;

            foreach (var dropSettings in _dropSettingsList)
            {
                DropJewelry(dropSettings, jewelryContainer, hitData);
            }
        }

        private void DropJewelry(JewelryDropSettings dropSettings, IJewelryContainer victimJewelryContainer, HitData hitData)
        {
            int dropAmount = dropSettings.GetDropAmount(hitData, victimJewelryContainer);

            int count = victimJewelryContainer.DropJewelry(dropSettings.JewelryType, dropAmount, _resultBuffer);

            // Dropの場合、デフォルトでその場にドロップするので何もしない
            if (DropType == DropType.Drop) return;

            // Pickup処理
            PickupJewelries(_resultBuffer.AsSpan(..count), hitData.ExecutorRef);
        }

        public static void PickupJewelries(ReadOnlySpan<IJewelry> jewelries, PlayerRef pickupPlayerRef)
        {
            foreach (IJewelry jewelry in jewelries)
            {
                if (jewelry is global::InGame.Jewelry.Jewelry jewel)
                {
                    jewel.PickupFrom(pickupPlayerRef).Forget();
                }
            }
        }
    }

    public enum DropType
    {
        Drop,
        Pickup
    }
}
