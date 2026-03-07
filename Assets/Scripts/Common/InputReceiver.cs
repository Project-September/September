using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace September.NewTitle
{
    public class InputReceiver : MonoBehaviour
    {
        [SerializeField] private InputActionReference _inputAction;
        [SerializeField] private UnityEvent _onPerformed;

        private void OnEnable()
        {
            _inputAction.action.performed += OnPerformed;
        }

        private void OnDisable()
        {
            _inputAction.action.performed -= OnPerformed;
        }

        private void OnPerformed(InputAction.CallbackContext obj)
        {
            _onPerformed.Invoke();
        }
    }
}