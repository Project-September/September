using UnityEngine;

namespace September.NewResult
{
    public interface ITotalScoreView
    {
        public void Setup(TotalScoreModel[] entries);
    }

    public readonly struct TotalScoreModel
    {
        public readonly Sprite Icon;
        public readonly string PlayerName;
        public readonly int Score;
        public readonly bool IsOgre;
        public readonly bool IsSelf;

        public TotalScoreModel(Sprite icon, string playerName, int score, bool isOgre, bool isSelf)
        {
            Icon = icon;
            PlayerName = playerName;
            Score = score;
            IsOgre = isOgre;
            IsSelf = isSelf;
        }
    }
}