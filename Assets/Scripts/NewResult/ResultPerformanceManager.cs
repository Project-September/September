using System.Linq;
using Cysharp.Threading.Tasks;
using September.Common;
using UnityEngine;

namespace September.NewResult
{
    public class ResultPerformanceManager : MonoBehaviour
    {
        [SerializeField] private ResultUIAnimator _resultUIAnimator;
        [SerializeField] private RankingView _rankingView;
        [SerializeField] private MenuActiveController _menuActiveController;
        
        public async UniTask StartResultPerformance(ResultCharacterDataContainer resultCharacterDataContainer, GameResultInfo gameResultInfo)
        {
            _menuActiveController.Deactivate();
            
            // ランキングの作成
            var rankingListNames = 
                gameResultInfo.Ranking.Where(x => x.Rank >= 2)
                    .OrderBy(x => x.Rank)
                    .Select(x => x.PlayerName)
                    .ToArray();
            
            _rankingView.CreateRankingList(rankingListNames);
            _rankingView.SetWinnerPlayerNameText(gameResultInfo.Ranking.FirstOrDefault(x => x.Rank == 1).PlayerName);

            // アセットの取得
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

            // 演出開始
            await UniTask.Delay(3000);
            
            // 演出終了⇒UI表示
            await _resultUIAnimator.ShowResultUI();
            Debug.Log("Result Performance End");
            
            // メニューを選択可能に
            _menuActiveController.Activate();
            _menuActiveController.SetEventSystemSelected();
        }
    }
}