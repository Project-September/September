using System;
using InGame.Health;
using InGame.Jewelry.Common;
using NaughtyAttributes;
using September.Common;
using September.NewResult.RankingPolicy;
using UnityEngine;

namespace September.InGame.Jewelry.Drop.Strategies
{
    [Serializable]
    public class RankingDropStrategy : IJewelryDropStrategy
    {
        [Header("順位に応じたドロップ量変動処理 (高順位が1, 低順位が0)")]
        [SerializeField, CurveRange(0, 0, 1, 5)] private AnimationCurve _rankDropCurve;

        private Ranking _ranking;

        public int GetDropAmount(HitData hitData, JewelryType jewelryType, IJewelryContainer jewelryContainer)
        {
            _ranking ??= StaticServiceLocator.Instance.Get<Ranking>();

            int playersCount = _ranking.GetCount();
            int victimRank = _ranking.GetRank(hitData.TargetRef);

            // 0~1 の範囲に正規化。
            // 数値が大きい方が順位が高いようにする
            float rankRatio = playersCount > 0
                    ? 1f - (victimRank - 1f) / (playersCount - 1f)
                    : 1f;

            int dropAmount = Mathf.RoundToInt(_rankDropCurve.Evaluate(rankRatio));

            return dropAmount;
        }
    }
}
