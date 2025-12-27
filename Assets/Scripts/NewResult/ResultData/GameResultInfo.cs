using System.Collections.Generic;
using Result;
using September.Common;
using UnityEngine;

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

    public readonly struct PlayerResultEntry
    {
        public readonly string PlayerName;
        public readonly CharacterType CharacterType;
        public readonly ResultExhibitScoreEntry[] ExhibitScoreEntries;
        public readonly int TotalScore;

        public PlayerResultEntry(string playerName, CharacterType characterType, ResultExhibitScoreEntry[] exhibitScoreEntries)
        {
            PlayerName = playerName;
            CharacterType = characterType;
            ExhibitScoreEntries = exhibitScoreEntries;

            TotalScore = 0;
            foreach (var entry in ExhibitScoreEntries)
            {
                TotalScore += entry.Score;
            }
        }
        
        public PlayerResultEntry(string playerName, CharacterType characterType, IReadOnlyList<ExhibitScoreEntry> config)
        {
            var entries = new ResultExhibitScoreEntry[config.Count];
            
            var totalScore = 0;
            // インタラクトできる種類とスコアを取得
            for (var i = 0; i < config.Count; i++)
            {
                var entry = config[i];
                var type = entry.Type;
                var count = InGameResultContainer.ExhibitInteractCounts?.GetValueOrDefault(type, 0) ??
                            Random.Range(0, 10);
                var score = count * entry.Points;

                entries[i] = new ResultExhibitScoreEntry(type, count, score);
                totalScore += score;
            }
            TotalScore = totalScore;

            PlayerName = playerName;
            CharacterType = characterType;
            ExhibitScoreEntries = entries;
        }
    }
    
    /// <summary>
    /// インゲームからリザルトに渡すデータ<br></br>
    /// 戦績として保存するリザルトデータとは異なる
    /// </summary>
    public class GameResultInfo
    {
        public string StageName { get; }
        public IReadOnlyList<RankingEntry> Ranking { get; }
        public IReadOnlyList<PlayerResultEntry> Players { get; }

        public GameResultInfo(
            string stageName, 
            IReadOnlyList<RankingEntry> rankingEntries, 
            IReadOnlyList<PlayerResultEntry> players)
        {
            StageName = stageName;
            Ranking = rankingEntries;
            Players = players;
        }
    }
}