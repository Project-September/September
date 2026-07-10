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
            var rankingPolicy = StaticServiceLocator.Instance.Get<InGameManager>().GameRule.RankingPolicy;
            var builder = new GameResultInfoBuilder(rankingPolicy);

            var db = PlayerDatabase.Instance;
            if (!db)
                return false;

            if (!db.PlayerDataDic.TryGet(db.Runner.LocalPlayer, out var localPlayerData))
            {
                Debug.LogError("No local player data found");
                return false;
            }

            foreach (var kvp in db.PlayerDataDic)
            {
                var d = kvp.Value;
                var player = new PlayerResultInfoBuilder();
                player.SetPlayerName(d.DisplayNickName);
                player.SetCharacterType(d.CharacterType);
                player.SetTotalScore(d.Score);
                player.SetIsOgre(d.IsOgre);
                player.SetIsSelf(d.DisplayNickName == localPlayerData.DisplayNickName);
                player.SetDamage(d.DamageDealt, d.DamageReceived);
                player.SetOgreCount(d.OgreCount);
                player.SetTotalInteractCount(d.TotalInteractCount);

                builder.AddPlayer(player.BuildInstance());
            }

            builder.SetStageName("Field");
            InGameResultContainer.Set(builder.BuildInstance());
            return true;
        }
    }
}
