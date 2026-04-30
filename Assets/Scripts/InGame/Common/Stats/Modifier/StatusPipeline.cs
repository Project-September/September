namespace September.InGame.Common.Stats
{
    /// <summary>
    /// <see cref="StatsEffectorBase"/>を登録し、ステータスに適用するクラス
    /// </summary>
    public class StatusPipeline
    {
        private readonly StatsEffectorBase[] _statsModifiers;
        
        public StatusPipeline(params StatsEffectorBase[] statsModifiers)
        {
            _statsModifiers = statsModifiers;
        }

        /// <summary>
        /// 登録されたモディファイアを適用した結果を返します
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