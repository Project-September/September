using System.Collections.Generic;

namespace September.NewResult
{
    public class GameResultInfoBuilder
    {
        private string _stageName;
        private readonly List<PlayerResultEntry> _players = new();
        
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
            var ranking = new RankingService().Create(_players);
            return new GameResultInfo(_stageName, ranking, _players);
        }
    }
}