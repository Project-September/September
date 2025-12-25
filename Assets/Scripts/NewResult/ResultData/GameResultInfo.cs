using System.Collections.Generic;
using Result;
using September.Common;

namespace September.NewResult
{
    public readonly struct RankingEntry
    {
        public readonly int Rank;
        public readonly string PlayerName;
        public readonly CharacterType CharacterType;

        public RankingEntry(int rank, string playerName, CharacterType characterType)
        {
            Rank = rank;
            PlayerName = playerName;
            CharacterType = characterType;
        }
    }

    public struct PlayerResultInfo
    {
        public string PlayerName;
        public ExhibitScoreEntry[] Score;
    }
    
    /// <summary>
    /// インゲームからリザルトに渡すデータ<br></br>
    /// 戦績として保存するリザルトデータとは異なる
    /// </summary>
    public class GameResultInfo
    {
        public string StageName { get; }
        public IReadOnlyList<RankingEntry> Ranking { get; }

        public GameResultInfo(string stageName, RankingEntry[] rankingEntries)
        {
            StageName = stageName;
            Ranking = rankingEntries;
        }
    }
}