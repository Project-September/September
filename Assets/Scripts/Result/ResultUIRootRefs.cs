using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Result
{
    public class ResultUIRootRefs :MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _finishText;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] public ResultRowRefs[] _rows;
        [SerializeField] private TextMeshProUGUI _yourRankText;
        [SerializeField] private Image _resultBg;
        [SerializeField] private RectTransform _rowsRoot;

        public TextMeshProUGUI FinishText => _finishText;
        public TextMeshProUGUI ResultText => _resultText;
        public Image ResultBg => _resultBg;
        public RectTransform RowsRoot => _rowsRoot;
        public ResultRowRefs[] Rows => _rows;
        public TextMeshProUGUI YourRankText => _yourRankText;
    }
}