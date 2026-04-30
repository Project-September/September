namespace September.InGame.Common.Stats
{
    public class StatusPipeline
    {
        private readonly StatsModifierBase[] _statsModifiers;
        
        public StatusPipeline(params StatsModifierBase[] statsModifiers)
        {
            _statsModifiers = statsModifiers;
        }

        /// <summary>
        /// 全てのエフェクトを適用する
        /// </summary>
        /// <param name="baseStats">適用前の初期値となる値</param>
        public StatsContainer CalcStats(in StatsContainer baseStats)
        {
            var stats = baseStats;
            
            // 全てのステータス効果を適用
            foreach (var mod in _statsModifiers)
            {
                stats = mod.Apply(stats);
            }
            
            // 結果をステータスコンテナに書き込む
            return stats;
        }
    }
}