using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEngine;

namespace September.NewResult
{
    public class ResultSceneDebugger : MonoBehaviour
    {
        [SerializeField] private ExhibitScoreConfig _exhibitScoreConfig;
        [SerializeField] private MockData.RankingEntryData[] _rankings;
        
        [ShowNonSerializedField] private bool _forceInject;
        
        private void Awake()
        {
            if (InGameResultContainer.Info == null || _forceInject)
            {
                InGameResultContainer.Set(new MockData(_rankings).Create(_exhibitScoreConfig));
            }
        }

        [Button("Log Result")]
        private void Log()
        {
            Debug.Log(JsonConvert.SerializeObject(InGameResultContainer.Info, Formatting.Indented));
        }

        [Button("Toggle Force Inject")]
        private void ToggleForceInject()
        {
            _forceInject = !_forceInject;
        }
    }
}