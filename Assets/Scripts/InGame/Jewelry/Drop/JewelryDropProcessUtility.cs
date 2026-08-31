using InGame.Health;
using September.Common;
using September.NewResult.RankingPolicy;
using UnityEngine;

namespace September.InGame.Jewelry.Drop.Strategies
{
    public static class JewelryDropProcessUtility
    {
        private static Ranking _ranking;

        public static void RankDamagePenalty(in HitData hitData, RoundingMethod higherRoundingMethod, RoundingMethod lowerRoundingMethod, float thresholdRankRate, ref DropInfo info)
        {
            _ranking ??= StaticServiceLocator.Instance.Get<Ranking>();

            int playersCount = _ranking.GetCount();
            int victimRank = _ranking.GetRank(hitData.TargetRef);

            // 0~1 の範囲に正規化。
            // 数値が大きい方が順位が高いようにする
            float rankRatio = playersCount > 0
                ? 1f - (victimRank - 1f) / (playersCount - 1f)
                : 1f;

            if (rankRatio >= thresholdRankRate)
            {
                info.Amount = RoundUtility.Apply(info.Amount, higherRoundingMethod);
            }
            else
            {
                info.Amount = RoundUtility.Apply(info.Amount, lowerRoundingMethod);
            }
        }
    }
}
