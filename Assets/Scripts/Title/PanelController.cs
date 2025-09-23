using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace September.Title
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PanelController : MonoBehaviour
    {
        [SerializeField] bool _hideOnStart;
        [SerializeField] Selectable _selectWhenShow;
        [SerializeField] Selectable _selectWhenHide;
        private CanvasGroup _panel;
        private bool _isInitialized;

        private void Awake()
        {
            _panel = GetComponent<CanvasGroup>();
            if (_hideOnStart) HidePanel();
            _isInitialized = true;
        }

        public void ShowPanel()
        {
            _panel.alpha = 1;
            _panel.interactable = true;
            _panel.blocksRaycasts = true;

            if (!_isInitialized) return;
            if (_selectWhenShow == null) return;
            EventSystem.current.SetSelectedGameObject(_selectWhenShow.gameObject);
        }

        public void HidePanel()
        {
            _panel.alpha = 0;
            _panel.interactable = false;
            _panel.blocksRaycasts = false;
            if (!_isInitialized) return;
            if (_selectWhenHide == null) return;
            EventSystem.current.SetSelectedGameObject(_selectWhenHide.gameObject);
        }
    }
}