using System;
using Fusion;
using UnityEngine;

namespace InGame.Player.Ability
{
    /// <summary>
    /// ロケットの処理
    /// ・着弾地点へ移動し、着弾時の処理を実行する
    /// </summary>
    public class RocketBullet : NetworkBehaviour
    {
        private Vector3 _targetPosition; // 着弾地点
        private float _moveSpeed; // 移動速度
        private Action _hitAction; // 着弾時に実行する処理
        
        /// <summary>
        /// 弾の初期化
        /// </summary>
        /// <param name="pos">着弾地点</param>
        /// <param name="speed">移動速度</param>
        /// <param name="action">着弾時に実行する処理</param>
        public void Initialization(Vector3 pos, float speed, Action action)
        {
            _targetPosition = pos;
            _moveSpeed = speed;
            _hitAction = action;
        }

        public override void FixedUpdateNetwork()
        {
            // 着弾地点に移動
            transform.position = 
                Vector3.MoveTowards(transform.position, _targetPosition, _moveSpeed * Runner.DeltaTime);

            // 着弾地点にある程度到達すれば、着弾時の処理を実行する
            if (Vector3.Distance(transform.position, _targetPosition) <= 0.1f)
            {
                _hitAction?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
