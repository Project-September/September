using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    public class ResultPerformanceTester : MonoBehaviour
    {
        [SerializeField] private ResultPerformanceState _performanceState;
        [SerializeField] private ResultPerformanceHandler _handler;
        
        private void Start()
        {
            if (!_performanceState || !_performanceState.gameObject.activeInHierarchy)
            {
                _performanceState = FindFirstObjectByType<ResultPerformanceState>(FindObjectsInactive.Exclude);
            }
            
            _handler.Play(_performanceState, destroyCancellationToken).Forget();
        }
    }
}