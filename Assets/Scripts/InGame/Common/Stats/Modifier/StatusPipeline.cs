namespace September.InGame.Common.Stats
{
    public class StatusPipeline
    {
        private readonly StatsModifierBase[] _statsModifiers;
        
        /// <summary>
        /// 計算用ステータス
        /// </summary>
        private readonly EffectableStats _stats;

        public StatusPipeline(params StatsModifierBase[] statsModifiers)
        {
            _statsModifiers = statsModifiers;
        }

        public void ResetStats(StatsContainer stats)
        {
            _stats.SetStats(stats.Stats);
        }

        /// <summary>
        /// 全てのエフェクトを適用する
        /// </summary>
        /// <param name="result">結果となるステータスコンテナ</param>
        public void CalcStats(ref StatsContainer result)
        {
            foreach (var mod in _statsModifiers)
            {
                mod.Apply(_stats);
            }
            
            _stats.GetStats(ref result);
        }
    }
}