using System;
using UnityEngine;

namespace September.InGame.Tutorial
{
    /// <summary>チュートリアルで使用する行動</summary>
    [Serializable]
    public class TutorialActionBase
    {
        public virtual void OnStart(Action action) { }
        public virtual void OnUpdate() { }
        public virtual void OnEndAction() { }
    }
}
