using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace September.NewResult
{
    public class ScorePagePresenter : MonoBehaviour
    {
        [SerializeField] private ScorePageView _scorePageView;
        [SerializeField] private ExhibitScoreConfig _exhibitScoreConfig;

        public void UpdatePage(IReadOnlyList<ResultExhibitScoreEntry> entries)
        {
            _scorePageView.SetScore(entries);
        }
    }
}