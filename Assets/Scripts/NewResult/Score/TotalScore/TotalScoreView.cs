using UnityEngine;

namespace September.NewResult
{
    public class TotalScoreView : MonoBehaviour, ITotalScoreView
    {
        [SerializeField] private Transform _parent;
        [SerializeField] private TotalScoreEntryView _entryPrefab;
        
        public void Setup((Sprite icon, string playerName, int score)[] entries)
        {
            foreach (Transform child in _parent)
            {
                Destroy(child.gameObject);
            }
            
            foreach ((Sprite icon, string playerName, int score) in entries)
            {
                var entryView = Instantiate(_entryPrefab, _parent);
                entryView.Setup(icon, playerName, score);
            }
        }
    }
}