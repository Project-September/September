using NaughtyAttributes;
using Newtonsoft.Json;
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

        [Button("Log Result")]
        private void Log()
        {
            Debug.Log(JsonConvert.SerializeObject(InGameResultContainer.Info, Formatting.Indented));
        }
    }
}