using NaughtyAttributes;
using Newtonsoft.Json;
using September.NewResult.RankingPolicy;
using UnityEngine;

namespace September.NewResult
{
    public class ResultSceneDebugger : MonoBehaviour
    {
        [SerializeField] private ExhibitScoreConfig _exhibitScoreConfig;
        [SerializeField] private MockData.RankingEntryData[] _rankings;

        [Header("順位付けルール")]
        [SerializeReference, SubclassSelector] private IRankingPolicy _rankingPolicy;
        
        [ShowNonSerializedField] private bool _forceInject;
        
        private void Awake()
        {
            if (InGameResultContainer.Info == null || _forceInject)
            {
                InGameResultContainer.Set(new MockData(_rankings).Create(_exhibitScoreConfig, _rankingPolicy));
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
