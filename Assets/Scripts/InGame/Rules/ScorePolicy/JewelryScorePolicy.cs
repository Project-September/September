using System.Linq;
using Fusion;
using InGame.Jewelry;
using InGame.Jewelry.Common;
using September.Common;
using UnityEngine;

namespace September.InGame.Rules.ScorePolicy
{
    public class JewelryScorePolicy : IGameResultScorePolicy
    {
        [SerializeField] private JewelryInfo[] _jewelryInfos;

        public int GetScore(PlayerRef player)
        {
            var playerObj = PlayerDatabase.Instance.PlayerObjectDic.Get(player);
            var jewelryRuntime = playerObj.GetComponentInChildren<PlayerJewelryRuntime>();

            return _jewelryInfos.Sum(jewelryInfo => jewelryRuntime.GetJewelryQuantity(jewelryInfo.JewelryType) * jewelryInfo.Score);
        }
    }
}
