using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace September.NewResult
{
    public class ResultPerformanceManager : MonoBehaviour
    {
        [SerializeField] private ResultUIAnimator _resultUIAnimator;
        [SerializeField] private RankingView _rankingView;
        [SerializeField] private MenuActiveController _menuActiveController;
        [SerializeField] private SceneTransitionEffect _transitionEffect;
        
        public async UniTask StartResultPerformance(ResultCharacterDataContainer resultCharacterDataContainer, GameResultInfo gameResultInfo)
        {
            _menuActiveController.Deactivate();
            
            // ランキングの作成
            var rankingListNames = 
                gameResultInfo.Ranking.Where(x => x.Rank >= 2)
                    .OrderBy(x => x.Rank)
                    .Select(x => new RankingItemModel(x.Rank, x.PlayerName, x.CharacterType))
                    .ToArray();
            
            _rankingView.CreateRankingList(rankingListNames);
            _rankingView.SetWinnerPlayerNameText(gameResultInfo.Ranking.FirstOrDefault(x => x.Rank == 1).PlayerName);

            // アセットの取得
            var ranking = gameResultInfo.Ranking;
            Debug.Log("Get Asset Process");
            Debug.Log(gameResultInfo.StageSceneName);
            foreach (var entry in ranking)
            {
                Debug.Log($"{entry.Rank}, {entry.PlayerName}, {entry.CharacterType}");
            }

            // 演出開始
            var loadSceneTask = SceneManager.LoadSceneAsync(gameResultInfo.StageSceneName, LoadSceneMode.Additive).ToUniTask();
            await _transitionEffect.FadeIn(loadSceneTask);
            
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