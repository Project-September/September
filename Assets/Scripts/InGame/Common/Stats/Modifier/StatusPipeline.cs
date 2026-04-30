using System;

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
        /// <param name="result">結果を書き込むコンテナ</param>
        public void CalcStats(StatsContainer baseStats, ref StatsContainer result)
        {
            // 計算時に使用するコレクションを作成（元の配列に影響を与えないように）
            Span<Stat> statsSpan = stackalloc Stat[baseStats.Stats.Count];

            // 値のみ書き写す（コピーした値をSpanに格納する）
            int i = 0;
            foreach (var (_, stat) in baseStats.Stats)
            {
                statsSpan[i++] = stat;
            }
            
            // 計算用の高速なステータス
            var calcStats = new CalcStats(statsSpan);
            
            // 全てのステータス効果を適用
            foreach (var mod in _statsModifiers)
            {
                mod.Apply(calcStats);
            }
            
            // 結果をステータスコンテナに書き込む
            calcStats.WriteStats(ref result);
        }
    }
}