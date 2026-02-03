using UnityEngine;

namespace September.NewResult
{
    public class TotalScoreView : MonoBehaviour, ITotalScoreView
    {
        [SerializeField] private Transform _parent;
        [SerializeField] private TotalScoreEntryView _entryPrefab;
        
        public void Setup(TotalScoreViewEntry[] entries)
        {
            foreach (Transform child in _parent)
            {
                Destroy(child.gameObject);
            }
            
            foreach (var entry in entries)
            {
                var entryView = Instantiate(_entryPrefab, _parent);
                entryView.Setup(entry);
            }
        }
    }
}