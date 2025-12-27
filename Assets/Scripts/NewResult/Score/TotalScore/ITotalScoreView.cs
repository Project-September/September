using UnityEngine;

namespace September.NewResult
{
    public interface ITotalScoreView
    {
        public void Setup((Sprite icon, string playerName, int score)[] entries);
    }
}