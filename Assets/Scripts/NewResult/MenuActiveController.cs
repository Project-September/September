using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NewResult
{
    public class MenuActiveController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _menuRootCanvasGroup;
        [SerializeField] private Selectable _defaultSelection;

        public void Activate()
        {
            _menuRootCanvasGroup.interactable = true;
        }

        public void Deactivate()
        {
            _menuRootCanvasGroup.interactable = false;
        }

        public void SetEventSystemSelected()
        {
            EventSystem.current.SetSelectedGameObject(_defaultSelection.gameObject);
        }
    }
}