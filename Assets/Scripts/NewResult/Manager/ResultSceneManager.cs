using System.Linq;
using September.Common;
using UnityEngine;

namespace September.NewResult
{
    public class ResultSceneManager : MonoBehaviour
    {
        [SerializeField] private ResultCharacterDataContainer _resultCharacterDataContainer;
        [SerializeField] private ResultPerformanceManager _resultPerformanceManager;
        [SerializeField] private ResultUIInitializer _resultUIInitializer;
        [SerializeField] private ExhibitScoreConfig _config;

        private async void Start()
        {
            var gameResultInfo = InGameResultContainer.Info ?? MockData.Create(_config);
            
            _resultUIInitializer.Initialize(gameResultInfo);
            
            await _resultPerformanceManager.StartResultPerformance(_resultCharacterDataContainer, gameResultInfo);
        }
    }

    public static class MockData
    {
        public static GameResultInfo Create(ExhibitScoreConfig config)
        {
            var builder = new GameResultInfoBuilder();
            builder.SetStageName("Field");
            builder.SetRanking(new RankingEntry[]
            {
                new(2, "Second Octane", CharacterType.HulkTheButcher),
                new(1, "First the Winner with Long Name", CharacterType.OkabeWright),
                new(3, "Thirdlongestlongestlongestname", CharacterType.OkabeWright),
                new(5, "五位あいうえおかきくけこさしすせそ", CharacterType.Sarutobi),
                new(4, "Fourth long long long long Name", CharacterType.Tanihira),
            });

            var playerCount = Random.Range(0, 6);
            var ogrePlayerIndex = Random.Range(0, playerCount - 1);
            for (var i = 0; i < playerCount; i++)
            {
                var exhibitInteractCounts = 
                    config.Entries
                        .Select(x => new { key = x.Type, value = Random.Range(0, 10)})
                        .ToDictionary(x => x.key, x => x.value);

                var score = exhibitInteractCounts.Select((x, i) => x.Value * config.Entries[i].Points).Sum();
                
                var player = new PlayerResultInfoBuilder();
                player.SetPlayerName($"Test_Player{i + 1}");
                player.SetCharacterType((CharacterType)(i % 4 + 1));
                player.SetTotalScore(score);
                player.SetScoreConfig(config.Entries);
                player.SetIsOgre(i == ogrePlayerIndex);
                player.SetExhibitInteractCounts(exhibitInteractCounts);
                builder.AddPlayer(player.BuildInstance());
            }

            var gameResultInfo = builder.BuildInstance();
            return gameResultInfo;
        }
    }
}