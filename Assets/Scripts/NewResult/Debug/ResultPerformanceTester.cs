using UnityEngine;

namespace September.NewResult
{
    public class ResultPerformanceTester : MonoBehaviour
    {
        [SerializeField] private ResultPerformanceState _performanceState;
        
        private void Start()
        {
            if (!_performanceState) return;
            _performanceState.Play();
        }
    }
}