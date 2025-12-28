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
            var gameResultInfo = MockData.Create(_config);
            
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

            builder.AddPlayer(new PlayerResultEntry("Test_FirstPlayer", CharacterType.HulkTheButcher, config.Entries, false));
            builder.AddPlayer(new PlayerResultEntry("Test_SecondPlayer", CharacterType.OkabeWright, config.Entries, true));
            builder.AddPlayer(new PlayerResultEntry("Test_ThirdPlayer", CharacterType.Tanihira, config.Entries, false));
            builder.AddPlayer(new PlayerResultEntry("Test_ForcePlayer", CharacterType.Sarutobi, config.Entries, false));

            var gameResultInfo = builder.BuildInstance();
            return gameResultInfo;
        }
    }
}