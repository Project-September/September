using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace September.InGame.UI
{
    [DisallowMultipleComponent]
    public sealed class InGameUIRootRefs : MonoBehaviour
    {
        [Header("Panels / Texts")]
        [SerializeField] private GameObject _optionUI;
        [SerializeField] private GameObject _killLogPanel;
        [SerializeField] private TextMeshProUGUI _killLogText;
        [SerializeField] private GameObject _ogreUI;

        [Header("Bars")]
        [SerializeField] private Slider _hpBar;
        [SerializeField] private Slider _staminaBar;

        [Header("Interact")]
        [SerializeField] private InteractUi _interactUI;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI _timerText;
        
        public GameObject OptionUI => _optionUI;
        public GameObject KillLogPanel => _killLogPanel;
        public TextMeshProUGUI KillLogText => _killLogText;
        public GameObject OgreUI => _ogreUI;
        public Slider HpBar => _hpBar;
        public Slider StaminaBar => _staminaBar;
        public InteractUi InteractUI => _interactUI;
        public TextMeshProUGUI TimerText => _timerText;
    }
}