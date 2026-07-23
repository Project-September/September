using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using September.Common;
using September.InGame.Rules.ScorePolicy;
using September.NewResult.RankingPolicy;

namespace September.NewResult
{
    /// <summary>
    /// セッションデータからリザルト情報を生成するファクトリ
    /// </summary>
    public class GameResultFactory
    {
        private readonly IRankingPolicy _rankingPolicy;
        private readonly IGameResultScorePolicy _scorePolicy;

        public GameResultFactory(IRankingPolicy rankingPolicy, IGameResultScorePolicy scorePolicy)
        {
            _rankingPolicy = rankingPolicy;
            _scorePolicy = scorePolicy;
        }
        
        public GameResultInfo CreateResult(NetworkRunner runner, MapType mapType)
        {
            string stageName = mapType.ToString();
            var playerResults = new List<PlayerResultEntry>();

            foreach ((PlayerRef player, SessionPlayerData sessionPlayerData) in PlayerDatabase.Instance.PlayerDataDic)
            {
                playerResults.Add(
                    new PlayerResultEntry(
                        sessionPlayerData.DisplayNickName,
                        sessionPlayerData.CharacterType,
                        Array.Empty<ResultExhibitScoreEntry>(),
                        _scorePolicy.GetScore(player),
                        sessionPlayerData.IsOgre,
                        player == runner.LocalPlayer,
                        sessionPlayerData.DamageDealt,
                        sessionPlayerData.DamageReceived,
                        sessionPlayerData.TotalInteractCount,
                        sessionPlayerData.OgreCount
                        )
                    );
            }

            PlayerResultEntry[] rankedPlayers = _rankingPolicy.Apply(playerResults).ToArray();

            return new GameResultInfo(stageName, rankedPlayers);
        }
    }
}
