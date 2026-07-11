using September.Common;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace September.InGame.Tutorial
{
    public class TutorialActionCamera : TutorialActionBase
    {
        [Header("カメラをリセットする回数"),SerializeField] private int _resetCount;
        [SerializeField] private string _explanationText;
        private int _currentResetCount;
        public override void OnStart(TutorialActionData actionData) 
        {
            base.OnStart(actionData);
            _currentResetCount = _resetCount;
            Debug.Log($"カメラを{_resetCount}回リセットしよう");
        }
        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!_isActionStarted) return;

            // プレイヤーがカメラをリセットしたかチェック
            if (GameInput.I.Player.Aim.triggered)
            {
                _currentResetCount--;

                if (_currentResetCount <= 0)
                {
                    _actionData.Action?.Invoke();
                }
            }
        }
        public override void OnEndAction() 
        {
            Debug.Log("カメラリセットアクション完了");
        }
    }
}
