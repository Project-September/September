using System.Collections.Generic;
using UnityEngine;

namespace September.NewResult
{
    public class ResultUIInitializer : MonoBehaviour
    {
        [SerializeField] private ScorePagePresenter _scorePagePresenter;

        public void Initialize(IReadOnlyList<ResultExhibitScoreEntry> entries)
        {
            _scorePagePresenter.UpdatePage(entries);
        }
    }
}