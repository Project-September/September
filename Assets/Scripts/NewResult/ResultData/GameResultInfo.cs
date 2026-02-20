using System.Collections.Generic;
using Result;
using September.Common;
using UnityEngine;

namespace September.NewResult
{
    public struct PlayerResultEntry
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
        
        public int Rank;

        public PlayerResultEntry(string playerName, 
            CharacterType characterType,
            ResultExhibitScoreEntry[] exhibitScoreEntries,
            int totalScore,
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
            TotalScore = totalScore;
            Rank = 0;
            IsOgre = isOgre;
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
        public IReadOnlyList<PlayerResultEntry> Players { get; }

        public GameResultInfo(
            string stageSceneName, 
            IReadOnlyList<PlayerResultEntry> players)
        {
            StageSceneName = stageSceneName;
            Players = players;
        }
    }
}