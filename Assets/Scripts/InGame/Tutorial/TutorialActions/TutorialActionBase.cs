using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using InGame.Player;
using TMPro;

namespace September.InGame.Tutorial
{
    /// <summary>
    /// チュートリアルで使用する行動のデータ
    /// </summary>
    public struct TutorialActionData
    {
        public Action Action;
        public GameObject Player;
        public GameObject TutorialUI;
        public TextMeshProUGUI TutorialText;
        public TextMeshProUGUI ActionConditionText;
        public PlayerInputManager PlayerInputManager;
    }
    /// <summary>チュートリアルで使用する行動</summary>
    [Serializable]
    public class TutorialActionBase
    {
        [SerializeField] protected Sprite _explanationPicture;
        protected bool _isCompleted = false;
        protected TutorialActionData _actionData;
        protected bool _isActionStarted = false;
        public virtual void OnStart(TutorialActionData actionData)
        {
            _actionData = actionData;
            actionData.TutorialUI.SetActive(true);
            actionData.TutorialUI.GetComponent<Image>().sprite = _explanationPicture;
            _isActionStarted = true;
            Cursor.visible = true;
        }

        public virtual void OnUpdate()
        {
            if (!_isActionStarted) return;
        }
        public virtual void OnEndAction() { }
    }
}
