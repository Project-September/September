using Cysharp.Threading.Tasks;
using September.Common;
using UnityEngine;

namespace NewResult
{
    public class ResultPerformanceManager : MonoBehaviour
    {
        [SerializeField] private ResultCharacterDataContainer _resultCharacterDataContainer;
        [SerializeField] private ResultUIAnimator _resultUIAnimator;

        private void Start()
        {
            var resultInfo = new GameResultInfo("Test", new[]
            {
                new RankingEntry(2, "Second Octane", CharacterType.HulkTheButcher),
                new RankingEntry(1, "First the Winner with Long Name", CharacterType.OkabeWright),
                new RankingEntry(3, "Thirdlongestlongestlongestname", CharacterType.OkabeWright),
                new RankingEntry(5, "五位あいうえおかきくけこさしすせそ", CharacterType.Sarutobi),
                new RankingEntry(4, "Fourth long long long long Name", CharacterType.Tanihira),
            });
            
            StartResultPerformance(_resultCharacterDataContainer, resultInfo);
        }

        private async UniTask StartResultPerformance(ResultCharacterDataContainer resultCharacterDataContainer, GameResultInfo gameResultInfo)
        {
            var ranking = gameResultInfo.Ranking;
            foreach (var entry in ranking)
            {
                Debug.Log($"{entry.Rank}, {entry.PlayerName}, {entry.CharacterType}");
            }

            foreach (var entry in ranking)
            {
                var assets = resultCharacterDataContainer.GetAssets(entry.CharacterType);
                Debug.Log(assets.TestString);
            }

            // 演出待機
            await UniTask.Delay(3000);
            
            // 演出終了⇒UI表示
            await _resultUIAnimator.ShowResultUI();
        }
    }
}