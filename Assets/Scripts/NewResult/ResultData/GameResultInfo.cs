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

        public RankingEntry(int rank, string playerName)
        {
            Rank = rank;
            PlayerName = playerName;
        }
    }

    public readonly struct PlayerResultEntry
    {
        public readonly string PlayerName;
        public readonly CharacterType CharacterType;
        public readonly ResultExhibitScoreEntry[] ExhibitScoreEntries;
        public readonly int TotalScore;
        public readonly bool IsOgre;
        public readonly bool IsSelf;
        
        public readonly int DamageDealt;
        public readonly int DamageReceived;
        public readonly int ExhibitInteractCount;
        public readonly int OgreCount;

        public PlayerResultEntry(string playerName, 
            CharacterType characterType,
            int totalScore,
            ResultExhibitScoreEntry[] exhibitScoreEntries,
            bool isOgre,
            bool isSelf, 
            int damageDealt, 
            int damageReceived, 
            int exhibitInteractCount, 
            int ogreCount)
        {
            PlayerName = playerName;
            CharacterType = characterType;
            ExhibitScoreEntries = exhibitScoreEntries;
            IsOgre = isOgre;
            TotalScore = totalScore;
            IsSelf = isSelf;
            DamageDealt = damageDealt;
            DamageReceived = damageReceived;
            ExhibitInteractCount = exhibitInteractCount;
            OgreCount = ogreCount;
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