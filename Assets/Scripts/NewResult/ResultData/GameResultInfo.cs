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
        public readonly bool IsOgre;

        public PlayerResultEntry(string playerName, 
            CharacterType characterType,
            int totalScore,
            ResultExhibitScoreEntry[] exhibitScoreEntries,
            bool isOgre)
        {
            PlayerName = playerName;
            CharacterType = characterType;
            ExhibitScoreEntries = exhibitScoreEntries;
            IsOgre = isOgre;
            TotalScore = totalScore;
        }
        
        public PlayerResultEntry(string playerName, 
            CharacterType characterType,
            int totalScore,
            IReadOnlyList<ExhibitScoreEntry> config,
            IReadOnlyDictionary<ExhibitType,int> exhibitInteractCounts,
            bool isOgre)
        {
            PlayerName = playerName;
            CharacterType = characterType;
            TotalScore = totalScore;
            IsOgre = isOgre;
            
            var result = new ResultExhibitScoreEntry[config.Count];
            
            // インタラクトできる種類とスコアを取得
            for (var i = 0; i < config.Count; i++)
            {
                var entry = config[i];
                // 展示物の種類　例　プテラ
                ExhibitType type = entry.Type;
                // 登録した種類
                int point = entry.Points;
                // 何回インタラクトしたか
                int count = exhibitInteractCounts.GetValueOrDefault(type, 0);
                // スコアとインタラクト回数を乗算
                int score = count * point;

                result[i] = new ResultExhibitScoreEntry(type, count, score);
            }
            ExhibitScoreEntries = result;
        }
    }
    
    /// <summary>
    /// インゲームからリザルトに渡すデータ<br></br>
    /// 戦績として保存するリザルトデータとは異なる
    /// </summary>
    public class GameResultInfo
    {
        public string StageSceneName { get; }
        public IReadOnlyList<RankingEntry> Ranking { get; }
        public IReadOnlyList<PlayerResultEntry> Players { get; }

        public GameResultInfo(
            string stageSceneName, 
            IReadOnlyList<RankingEntry> rankingEntries, 
            IReadOnlyList<PlayerResultEntry> players)
        {
            StageSceneName = stageSceneName;
            Ranking = rankingEntries;
            Players = players;
        }
    }
}