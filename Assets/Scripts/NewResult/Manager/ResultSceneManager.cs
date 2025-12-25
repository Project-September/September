using September.Common;
using UnityEngine;

namespace September.NewResult
{
    public class ResultSceneManager : MonoBehaviour
    {
        [SerializeField] private ResultCharacterDataContainer _resultCharacterDataContainer;
        [SerializeField] private ResultPerformanceManager _resultPerformanceManager;
        
        private GameResultInfo _testResultInfo = new GameResultInfo("Test", new[]
        {
            new RankingEntry(2, "Second Octane", CharacterType.HulkTheButcher),
            new RankingEntry(1, "First the Winner with Long Name", CharacterType.OkabeWright),
            new RankingEntry(3, "Thirdlongestlongestlongestname", CharacterType.OkabeWright),
            new RankingEntry(5, "五位あいうえおかきくけこさしすせそ", CharacterType.Sarutobi),
            new RankingEntry(4, "Fourth long long long long Name", CharacterType.Tanihira),
        });

        private async void Start()
        {
            await _resultPerformanceManager.StartResultPerformance(_resultCharacterDataContainer, _testResultInfo);
        }
    }
}