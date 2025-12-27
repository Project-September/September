using UnityEngine;

namespace September.NewResult
{
    public interface ITotalScoreView
    {
        public void Setup(TotalScoreViewEntry[] entries);
    }

    public readonly struct TotalScoreViewEntry
    {
        public readonly Sprite Icon;
        public readonly string PlayerName;
        public readonly int Score;
        public readonly bool IsOgre;

        public TotalScoreViewEntry(Sprite icon, string playerName, int score, bool isOgre)
        {
            Icon = icon;
            PlayerName = playerName;
            Score = score;
            IsOgre = isOgre;
        }
    }
}