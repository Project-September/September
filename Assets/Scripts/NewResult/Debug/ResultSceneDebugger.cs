using UnityEngine;

namespace September.NewResult
{
    public class ResultSceneDebugger : MonoBehaviour
    {
        [SerializeField] private ExhibitScoreConfig _exhibitScoreConfig;
        [SerializeField] private MockData.RankingEntryData[] _rankings;
        
        private void Awake()
        {
            if (InGameResultContainer.Info == null)
            {
                InGameResultContainer.Set(new MockData(_rankings).Create(_exhibitScoreConfig));
            }
        }
    }
}