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
            var success = await effect.TryFadeOut();
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
                    };
                }).ToArray();

            var ogres = data.Where(x => x.isOgre).OrderByDescending(x => x.score);
            var nonOgres = data.Where(x => x.isOgre == false).OrderByDescending(x => x.score);
            var ranking = nonOgres.Concat(ogres).ToArray();
                
            for (var i = 0; i < ranking.Length; i++)
            {
                var playerName = ranking[i].name;
                var characterType = ranking[i].type;
                var score = ranking[i].score;
                var isOgre = ranking[i].isOgre;
                var isSelf = ranking[i].isSelf;
                    
                var player = new PlayerResultInfoBuilder();
                player.SetPlayerName(playerName);
                player.SetCharacterType(characterType);
                player.SetTotalScore(score);
                player.SetIsOgre(isOgre);
                player.SetIsSelf(isSelf);
                
                builder.AddPlayer(player.BuildInstance());
            }
                
            builder.SetStageName("Field");
            InGameResultContainer.Set(builder.BuildInstance());
            return true;
        }
    }
}