using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace September
{
    public class ButtonSelector : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            SelectButtonAsync().Forget();
        }

        // ‘I‘ð‚³‚ê‚½‚Æ‚«
        public void OnSelect(BaseEventData eventData)
        {
            transform.localScale = transform.localScale * 1.5f;
        }

        private async UniTaskVoid SelectButtonAsync()
        {
            await UniTask.Yield();

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_button.gameObject);

            Debug.Log($"Selected: {EventSystem.current.currentSelectedGameObject}");
        }
    }
}