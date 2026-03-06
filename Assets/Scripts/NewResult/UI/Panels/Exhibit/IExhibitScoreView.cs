using System.Collections.Generic;

namespace September.NewResult
{
    public interface IExhibitScoreView
    {
        public void Setup(IReadOnlyList<ResultExhibitScoreEntry> entries);
    }
}