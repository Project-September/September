using System;
using UnityEngine;

namespace September.InGame.Tutorial
{
    [Serializable]
    public class TutorialActionMove : TutorialActionBase
    {
        [SerializeField] private Transform[] _moveTarget;
        [SerializeField] private GameObject _targetDisplayObj;
        [SerializeField] private float _targetRange = 3f;
        [SerializeField] private LayerMask _playerLayer = 1 << 6; // プレイヤーのレイヤー
        private int _index = 0;
        private const int CHECK_FRAME_INTERVAL = 5;
        private Action _onCompleted;
        public override void OnStart(Action action)
        {
            base.OnStart(action);
            _onCompleted = action;
            Debug.Log(_targetDisplayObj.transform.position);
            _targetDisplayObj.transform.position = _moveTarget[_index].position;
            Debug.Log(_targetDisplayObj.transform.position);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            // プレイヤーがターゲットの範囲内にいるかどうかをチェック
            if (IsInPlayerInTarget())
            {
                ToNextTarget();
            }
        }

        /// <summary>
        /// 次のターゲットに移動する処理
        /// </summary>
        private void ToNextTarget()
        {
            _index++;
            // ターゲットのインデックスが範囲外になった場合の処理
            if (_index >= _moveTarget.Length)
            {
                // すべてのターゲットをクリアした場合の処理
                Debug.Log("すべてのターゲットをクリアしました！");
                _onCompleted?.Invoke();
                return;
            }
            // 次のターゲットに移動
            _targetDisplayObj.transform.position = _moveTarget[_index].position;
        }

        /// <summary>
        /// 指定した範囲内にプレイヤーがいるかどうかをチェックする
        /// </summary>
        private bool IsInPlayerInTarget()
        {
            // 処理の負荷を減らすため、毎フレームチェックするのではなく、5フレームに1回チェックする
            if (Time.frameCount % CHECK_FRAME_INTERVAL != 0) return false;

            if (_index >= _moveTarget.Length)
            {
                Debug.LogWarning("ターゲットのインデックスが範囲外です。");
                return false;
            }

            // 指定した範囲内にプレイヤーがいるかどうかをチェック
            Collider[] hitColliders = Physics.OverlapSphere(
                _moveTarget[_index].position,
                _targetRange,
                _playerLayer);

            // 範囲内にプレイヤーがいる場合はtrueを返す
            if (hitColliders.Length > 0)
            {
                return true;
            }
            return false;
        }

        public override void OnEndAction()
        {
            base.OnEndAction();
            _onCompleted = null;
            _index = 0;
        }
    }
}
