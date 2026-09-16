using System.Collections.Generic;
using Result;

namespace September.NewResult
{
    public static class ResultExhibitScoreEntryUtility
    {
        public static ResultExhibitScoreEntry[] CalcScoreEntries(ExhibitScoreConfig config, IReadOnlyDictionary<ExhibitType, int> exhibitInteractCounts)
        {
            var configEntries = config.Entries;
            var entries = new ResultExhibitScoreEntry[configEntries.Count];

            // インタラクトできる種類とスコアを取得
            for (int i = 0; i < configEntries.Count; i++)
            {
                ExhibitScoreEntry entry = configEntries[i];
                ExhibitType type = entry.Type;
                int count = exhibitInteractCounts.GetValueOrDefault(type, 0);
                int score = count * entry.Points;

                entries[i] = new ResultExhibitScoreEntry(type, count, score);
            }
            return entries;
        }
    }
}
