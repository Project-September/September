using System.Collections.Generic;
using Result;

namespace September.NewResult
{
    public static class InGameResultContainer
    {
        public static IReadOnlyDictionary<ExhibitType,int> ExhibitInteractCounts { get; private set; }

        public static void SetExhibitInteractCounts(IReadOnlyDictionary<ExhibitType,int> exhibitInteractCounts)
        {
            ExhibitInteractCounts = new Dictionary<ExhibitType, int>(exhibitInteractCounts);
        }
    }
}