using UnityEngine;
using UnityEngine.UI;

namespace September.Lobby
{
    public class HowToPlayView : MonoBehaviour
    {
        [SerializeField] GameObject _howToPlayPanel;
        [SerializeField] Button _closeButton;


        private void Awake()
        {
            _closeButton.onClick.AddListener(CloseHowToPlayPanel);
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(CloseHowToPlayPanel);
        }

        public void OpenHowToPlayPanel()
        {
            _howToPlayPanel.SetActive(true);
        }
        public void CloseHowToPlayPanel()
        {
            _howToPlayPanel.SetActive(false);
        }
    }
}