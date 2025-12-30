using System.Collections.Generic;
using Result;

namespace September.NewResult
{
    public static class InGameResultContainer
    {
        public static GameResultInfo Info { get; private set; }

        public static void Set(GameResultInfo info)
        {
            Info = info;
        }
    }
}