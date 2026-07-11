using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using InGame.Player;    

namespace September.InGame.Tutorial
{
    /// <summary>
    /// チュートリアルで使用する行動のデータ
    /// </summary>
    public struct TutorialActionData
    {
        public Action Action;
        public GameObject Player;
        public Image Image;
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
            actionData.Image.enabled = true;
            actionData.Image.sprite = _explanationPicture;
            _isActionStarted = true;
        }

        public virtual void OnUpdate()
        {
            if (!_isActionStarted) return;
        }
        public virtual void OnEndAction() { }
    }
}
