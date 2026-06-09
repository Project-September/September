using System;
using UnityEngine;

namespace September
{
    /// <summary>チュートリアルで使用する行動</summary>
    [Serializable]
    public class TutorialActionBase
    {
        public virtual void OnStart() { }
        public virtual void OnUpdate() { }
        public virtual void OnEndAction() { }
    }
}
