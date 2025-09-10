using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Result
{
    public class ResultUIRootRefs :MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _finishText;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI[] _nameSlots;
        [SerializeField] private TextMeshProUGUI[] _scoreSlots;
        [SerializeField] private TextMeshProUGUI[] _rankSlots;
        [SerializeField] private TextMeshProUGUI _yourRankText;
        [SerializeField] private Image[] _iconSlots;
        [SerializeField] private Image _resultBg;
        [SerializeField] private RectTransform _rowsRoot;

        public TextMeshProUGUI FinishText => _finishText;
        public TextMeshProUGUI ResultText => _resultText;
        public Image ResultBg => _resultBg;
        public RectTransform RowsRoot => _rowsRoot;
        public TextMeshProUGUI[] ScoreSlots => _scoreSlots;
        public TextMeshProUGUI[] RankSlots => _rankSlots;
        public Image[] IconSlots => _iconSlots;
        public TextMeshProUGUI[] NameSlots => _nameSlots;
        public TextMeshProUGUI YourRankText => _yourRankText;
    }
}