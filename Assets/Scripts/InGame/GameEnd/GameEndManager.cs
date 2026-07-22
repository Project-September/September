using System.Linq;
using Cysharp.Threading.Tasks;
using September.Common;
using September.InGame.Common;
using September.InGame.UI;
using September.NewResult;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace September.InGame
{
    public class GameEndManager : MonoBehaviour
    {
        [SerializeField] private FinishAnimation _finishAnimation;
        [SerializeField] private SceneTransitionEffect _sceneTransitionEffect;

        private async void Start()
        {
            UIController.I.OnGameEnd.Subscribe(_ => GameEnd().Forget()).AddTo(this);
        }

        public async UniTask GameEnd()
        {
            await _finishAnimation.Play();
            await TransitionToResultScene(_sceneTransitionEffect);
        }

        private static async UniTask<bool> TransitionToResultScene(SceneTransitionEffect effect)
        {
            var success = await effect.TryTransitionOut();
            if (success)
            {
                SceneManager.LoadSceneAsync("NewResult");
                return BuildGameResultInfo();
            }

            return false;
        }

        private static bool BuildGameResultInfo()
        {
            var gameRule = StaticServiceLocator.Instance.Get<InGameManager>().GameRule;
            var builder = new GameResultFactory(gameRule.RankingPolicy, gameRule.GameResultScorePolicy);

            var db = PlayerDatabase.Instance;
            if (!db) return false;

            GameResultInfo gameResultInfo = builder.CreateResult(db.Runner, MapType.Museum);

            InGameResultContainer.Set(gameResultInfo);
            return true;
        }
    }
}
