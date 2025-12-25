using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace September.NewResult
{
    public class ScorePagePresenter : MonoBehaviour
    {
        [FormerlySerializedAs("_scorePage")] [SerializeField] private ScorePageView _scorePageView;
        [SerializeField] private ExhibitScoreConfig _exhibitScoreConfig;

        private void Start()
        {
            UpdatePage();
        }

        private void UpdatePage()
        {
            var entries = new List<ResultExhibitScoreEntry>();
            
            // インタラクトできる種類とスコアを取得
            foreach (var entry in _exhibitScoreConfig.Entries)
            {
                var type = entry.Type;
                var count = InGameResultContainer.ExhibitInteractCounts?.GetValueOrDefault(type, 0) ?? Random.Range(0, 10);
                var score = count * entry.Points;

                entries.Add(new ResultExhibitScoreEntry(type, count, score));
            }
            
            _scorePageView.SetScore(entries.ToArray());
        }
    }
}