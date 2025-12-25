using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace September.NewResult
{
    public class SelectOnStart : MonoBehaviour
    {
        [SerializeField] private Selectable _selectable;
        
        private void Start()
        {
            EventSystem.current.SetSelectedGameObject(_selectable.gameObject);
        }
    }
}