using NaughtyAttributes;
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
        public GameObject LogPanel;
        public GameObject OgreUI;
        public ChangeTagOverlayMessage ChangeTagOverlayMessage;
        public TimeOverlayMessage TimeOverlayMessage;
        public CanvasGroup StatusUpGroup;
        public VerticalLayoutGroup StatusUpUIRoot;
        public TextMeshProUGUI ScoreText;
        public Image IconImage;

        [Header("Bars")]
        public Slider HpBar;
        public Slider StaminaBar;

        [Header("Interact")] 
        public InteractUi InteractUI;

        [Header("Timer")] 
        public TextMeshProUGUI TimerText;
        [Header("Description"),Label("1番目にPlayerの操作UI,2番目に展示物の操作UI")]
        public GameObject[] DescriptionIcon;
    }
}