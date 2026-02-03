using System;
using System.Linq;
using UnityEngine;

namespace September.NewResult
{
    [CreateAssetMenu(fileName = "RankIconContainer", menuName = "ScriptableObjects/RankIconContainer", order = 0)]
    public class RankIconContainer : ScriptableObject
    {
        [SerializeField] private RankIcon[] _rankIcons;

        public Sprite GetRankIcon(int rank)
        {
            return _rankIcons.FirstOrDefault(x => x.Rank == rank).Icon;
        }

        [Serializable]
        private struct RankIcon
        {
            public int Rank;
            public Sprite Icon;
        }
    }
}