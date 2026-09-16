using Fusion;
using InGame.Interact;
using UnityEngine;

namespace September.InGame.Tutorial
{
    public class TutorialActionInteract : TutorialActionBase
    {
        [SerializeField] private SharkInteractable _targetShark;
        [SerializeField] private TutorialMoveGuide _guide;
        private NetworkObject _playerObject;

        public override void OnStart(TutorialActionData actionData)
        {
            base.OnStart(actionData);
            _isCompleted = false;
            TutorialInteractEffect.Completed -= OnInteractionCompleted;
            TutorialInteractEffect.Completed += OnInteractionCompleted;
            _playerObject = actionData.Player.GetComponent<NetworkObject>();
            actionData.TutorialText.text = _explanationText;
            ConditionTextSet();
            // インタラクト練習中だけ、対象のサメの操作と案内を許可する。
            SetSharkInteractable(true);


            // 今回操作するサメだけを目的地にして、移動チュートリアルと同じ経路を表示する。
            if (_targetShark && _guide)
                _guide.Show(actionData.Player.transform, _targetShark.transform);
        }

        private void OnInteractionCompleted(InteractableBase target, int interactor)
        {
            if (!_isActionStarted || _isCompleted) return;
            if (!_targetShark || !_playerObject || !_playerObject.IsValid) return;
            // 他の展示物や別プレイヤーの操作では進めず、対象の長押し成功だけを受け付ける。
            if (target != _targetShark.GetComponent<InteractableBase>() ||
                interactor != _playerObject.InputAuthority.RawEncoded) return;

            _isCompleted = true;
            ConditionTextSet();
            _actionData.Action?.Invoke();
        }

        private void SetSharkInteractable(bool value)
        {
            if (!_targetShark) return;
            var target = _targetShark.GetComponent<InteractableBase>();
            if (target && target.Object && target.Object.IsValid && target.HasStateAuthority)
                target.ForceSetInteractable = value;
        }

        private void ConditionTextSet()
        {
            _actionData.ActionConditionText.text = $"サメにインタラクトしよう{(_isCompleted ? "1" : "0")}/1";
        }

        public override void OnEndAction()
        {
            // 完了・中断後はサメも再び操作不可にする。
            SetSharkInteractable(false);
            _isActionStarted = false;
            TutorialInteractEffect.Completed -= OnInteractionCompleted;
            if (_guide) _guide.Hide();
            base.OnEndAction();
        }
    }
}
