using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace September.InGame.UI
{
    [DisallowMultipleComponent]
    public sealed class InGameUIRootRefs : MonoBehaviour
    {
        [Header("Panels / Texts")]
        [field: SerializeField] public GameObject OptionUI { get; private set; }
        [field: SerializeField] public GameObject KillLogPanel { get; private set; }
        [field: SerializeField] public TextMeshProUGUI KillLogText { get; private set; }
        [field: SerializeField] public GameObject OgreUI { get; private set; }

        [Header("Bars")]
        [field: SerializeField] public Slider HpBar { get; private set; }
        [field: SerializeField] public Slider StaminaBar { get; private set; }

        [Header("Interact")]
        [field: SerializeField] public InteractUi InteractUI { get; private set; }

        [Header("Timer")]
        [field: SerializeField] public TextMeshProUGUI TimerText { get; private set; }
    }
}