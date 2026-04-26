namespace September.InGame.Common.Stats
{
    public class StatusPipeline
    {
        private readonly StatsModifierBase[] _statsModifiers;

        public StatusPipeline(params StatsModifierBase[] statsModifiers)
        {
            _statsModifiers = statsModifiers;
        }

        public StatsContainer CalcStats(StatsContainer baseStats)
        {
            var stats = baseStats;
            
            foreach (var mod in _statsModifiers)
            {
                stats = mod.Apply(stats);
            }
            
            return stats;
        }
    }
}