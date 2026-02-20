using UnityEngine;
using UnityEngine.Events;

namespace September.NewResult
{
    public class NavigationBar : MonoBehaviour
    {
        [SerializeField] private bool _enableBack;
        [SerializeField] private bool _enableForward;
        
        [SerializeField] private RectTransform _backNavigationDisplay;
        [SerializeField] private RectTransform _forwardNavigationDisplay;

        public UnityEvent onBack;
        public UnityEvent onNext;

        private void Start()
        {
            SetVisibility(_enableBack, _enableForward);
        }

        private void Update()
        {
            if (_enableBack && GameInput.I.UI.Cancel.IsPressed())
            {
                onBack.Invoke();
            }

            if (_enableForward && GameInput.I.UI.Submit.IsPressed())
            {
                onNext.Invoke();
            }
        }

        private void SetVisibility(bool backNavigation, bool forwardNavigation)
        {
            _backNavigationDisplay.gameObject.SetActive(backNavigation);
            _forwardNavigationDisplay.gameObject.SetActive(forwardNavigation);
        }
    }
}