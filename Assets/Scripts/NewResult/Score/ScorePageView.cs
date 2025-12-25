using Result;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        [Space(16)]
        [SerializeField] private Selectable _defaultSelect;
        [SerializeField] private Button _showButton;
        
        private GameObject _prevSelected;

        private void Start()
        {
            Hide();
            _pageNameText.text = _pageName;
            
            _showButton.onClick.AddListener(Show);
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
        
        public void Show()
        {
            _prevSelected = EventSystem.current.currentSelectedGameObject;
            _root.gameObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(_defaultSelect.gameObject);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
            EventSystem.current.SetSelectedGameObject(_prevSelected);
        }
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