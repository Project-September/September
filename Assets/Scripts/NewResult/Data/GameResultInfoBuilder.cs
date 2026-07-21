using System.Collections.Generic;
using System.Linq;
using September.NewResult.RankingPolicy;

namespace September.NewResult
{
    public class GameResultInfoBuilder
    {
        private string _stageName;
        private readonly List<PlayerResultEntry> _players = new();
        private readonly IRankingPolicy _rankingPolicy;

        public GameResultInfoBuilder(IRankingPolicy rankingPolicy)
        {
            _rankingPolicy = rankingPolicy;
        }
        
        public void SetStageName(string stageName)
        {
            _stageName = stageName;
        }

        public void SetPlayers(PlayerResultEntry[] players)
        {
            _players.AddRange(players);
        }

        public void AddPlayer(PlayerResultEntry player)
        {
            _players.Add(player);
        }

        public GameResultInfo BuildInstance()
        {
            var ranking = _rankingPolicy.Apply(_players).ToArray();
            return new GameResultInfo(_stageName, ranking);
        }
    }
}
