using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace September.InGame.UI
{
    [DisallowMultipleComponent]
    public sealed class InGameUIRootRefs : MonoBehaviour
    {
        [Header("Panels / Texts")]
        public GameObject OptionUI;
        public GameObject KillLogPanel;
        public GameObject OgreUI;
        public TextMeshProUGUI OgreMessageText;

        [Header("Bars")]
        public Slider HpBar;
        public Slider StaminaBar;

        [Header("Interact")] 
        public InteractUi InteractUI;

        [Header("Timer")] 
        public TextMeshProUGUI TimerText;
    }
}