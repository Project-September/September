using System;
using UnityEngine;

namespace September
{
    [Serializable]
    public class TutorialActionMove : TutorialActionBase
    {
        [SerializeField] private Transform[] _moveTarget;
        [SerializeField] private GameObject _targetObj;
        [SerializeField] private float _targetRange = 3f;
        [SerializeField] private LayerMask _playerLayer = 1 << 6; // プレイヤーのレイヤー
        private int _index = 0;
        private const int CHECK_FRAME_INTERVAL = 5;
        public override void OnStart()
        {
            base.OnStart();
            Debug.Log(_targetObj.transform.position);
            _targetObj.transform.position = _moveTarget[_index].position;
            Debug.Log(_targetObj.transform.position);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (IsInPlayerInTarget())
            {
                Debug.Log("範囲内に入ったよ");
            }
        }

        /// <summary>
        /// 指定した範囲内にプレイヤーがいるかどうかをチェックする
        /// </summary>
        private bool IsInPlayerInTarget()
        {
            // 処理の負荷を減らすため、毎フレームチェックするのではなく、5フレームに1回チェックする
            if (Time.frameCount % CHECK_FRAME_INTERVAL != 0)  return false;
            // 指定した範囲内にプレイヤーがいるかどうかをチェック
            Collider[] hitColliders = Physics.OverlapSphere(_moveTarget[_index].position, _targetRange, _playerLayer);         
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
        }
    }
}
