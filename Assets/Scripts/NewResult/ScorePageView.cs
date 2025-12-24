using Result;
using TMPro;
using UnityEngine;

namespace September.NewResult
{
    public class ScorePageView : MonoBehaviour
    {
        [SerializeField] private Transform _root;
        [SerializeField] private Transform _scorePanelRoot;
        [SerializeField] private ExhibitScoreEntryView _exhibitScoreEntryViewPrefab;
        [Space(16)]
        [SerializeField] private string _pageName;
        [SerializeField] private TextMeshProUGUI _pageNameText;
        [SerializeField] private TextMeshProUGUI _totalScoreText;

        private void Start()
        {
            Hide();
            _pageNameText.text = _pageName;
        }

        public void SetScore(ResultExhibitScoreEntry[] entries)
        {
            foreach (Transform child in _scorePanelRoot)
            {
                Destroy(child.gameObject);
            }
            
            var totalScore = 0;
            foreach (var entry in entries)
            {
                var item = Instantiate(_exhibitScoreEntryViewPrefab, _scorePanelRoot);
                var (type, count, score) = entry;
                item.Setup(type.ToDisplayName(), count, score);
                totalScore += score;
            }
            
            _totalScoreText.text = totalScore.ToString();
        }

        public void ToggleVisible()
        {
            _root.gameObject.SetActive(!_root.gameObject.activeSelf);
        }
        
        public void Show() => _root.gameObject.SetActive(true);

        public void Hide() => _root.gameObject.SetActive(false);
    }

    public readonly struct ResultExhibitScoreEntry
    {
        public readonly ExhibitType ExhibitType;
        public readonly int Count;
        public readonly int Score;

        public ResultExhibitScoreEntry(ExhibitType exhibitType, int count, int score)
        {
            ExhibitType = exhibitType;
            Count = count;
            Score = score;
        }

        public void Deconstruct(out ExhibitType exhibitType, out int count, out int score)
        {
            exhibitType = ExhibitType;
            count = Count;
            score = Score;
        }
    }
}