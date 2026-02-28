using TMPro;
using UnityEngine;

namespace September.NewResult
{
    public class WinnerView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _winnerNameText;

        public void SetWinnerName(string winnerName)
        {
            _winnerNameText.text = winnerName;
        }
    }
}