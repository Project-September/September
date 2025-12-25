using System.Collections.Generic;

namespace September.NewResult
{
    public class GameResultInfoBuilder
    {
        private string _stageName;
        private readonly List<RankingEntry> _ranking = new();
        private readonly List<PlayerResultEntry> _players = new();
        
        public void SetStageName(string stageName)
        {
            _stageName = stageName;
        }

        public void SetRanking(RankingEntry[] rankingEntries)
        {
            _ranking.AddRange(rankingEntries);
        }

        public void AddRankingEntry(RankingEntry rankingEntry)
        {
            _ranking.Add(rankingEntry);
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
            return new GameResultInfo(_stageName, _ranking, _players);
        }
    }
}