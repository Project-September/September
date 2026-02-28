using TMPro;
using UnityEngine;

namespace September.NewResult
{
    public class ExhibitScoreEntryView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _entryNameText;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private TextMeshProUGUI _scoreText;

        public void Setup(string entryName, int count, int score)
        {
            _entryNameText.text = entryName;
            _countText.text = $"x{count.ToString()}";
            _scoreText.text = score.ToString();
        }
    }
}