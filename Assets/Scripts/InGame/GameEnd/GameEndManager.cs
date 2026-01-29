using System.Linq;
using Cysharp.Threading.Tasks;
using Result;
using September.Common;
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
            var builder = new GameResultInfoBuilder();
                
            var db = PlayerDatabase.Instance;
            if (!db)
                return false;

            if (!db.PlayerDataDic.TryGet(db.Runner.LocalPlayer, out SessionPlayerData localPlayerData))
            {
                Debug.LogError("No local player data found");
                return false;
            }
            
            var data = db.PlayerDataDic
                .Select(kv =>
                {
                    SessionPlayerData d = kv.Value;
                    return new {
                        name = d.DisplayNickName,
                        score = d.Score,
                        isOgre = (bool)d.IsOgre,
                        type = d.CharacterType,
                        isSelf = d.DisplayNickName == localPlayerData.DisplayNickName,
                        damageDealt = d.DamageDealt,
                        damageReceived = d.DamageReceived,
                        ogreCount = d.OgreCount,
                        totalInteractCount = d.TotalInteractCount,
                    };
                }).ToArray();

            var ogres = data.Where(x => x.isOgre).OrderByDescending(x => x.score);
            var nonOgres = data.Where(x => x.isOgre == false).OrderByDescending(x => x.score);
            var ranking = nonOgres.Concat(ogres).ToArray();
                
            for (var i = 0; i < ranking.Length; i++)
            {
                var player = new PlayerResultInfoBuilder();
                player.SetPlayerName(ranking[i].name);
                player.SetCharacterType(ranking[i].type);
                player.SetTotalScore(ranking[i].score);
                player.SetIsOgre(ranking[i].isOgre);
                player.SetIsSelf(ranking[i].isSelf);
                player.SetDamage(ranking[i].damageDealt, ranking[i].damageReceived);
                player.SetOgreCount(ranking[i].ogreCount);
                player.SetTotalInteractCount(ranking[i].totalInteractCount);
                
                builder.AddPlayer(player.BuildInstance());
            }
                
            builder.SetStageName("Field");
            InGameResultContainer.Set(builder.BuildInstance());
            return true;
        }
    }
}