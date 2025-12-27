using System.Collections.Generic;
using Result;
using TMPro;
using UnityEngine;

namespace September.NewResult
{
    public class ExhibitScoreView : MonoBehaviour, IExhibitScoreView
    {
        [SerializeField] private Transform _scorePanelRoot;
        [SerializeField] private ExhibitScoreEntryView _exhibitScoreEntryViewPrefab;
        [SerializeField] private TextMeshProUGUI _totalScoreText;
        
        private GameObject _prevSelected;

        public void Setup(IReadOnlyList<ResultExhibitScoreEntry> entries)
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