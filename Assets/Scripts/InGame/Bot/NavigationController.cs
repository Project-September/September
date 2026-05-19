using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace InGame.Bot
{
    [System.Serializable]
    public class NavigationController
    {
        public float StopDistance;
        public bool CanVault;
        public bool IsComplete { get; private set; } = false;
        public bool IsVaultInput { get; private set; } = false;
        public Vector2 InputDirection { get; private set; }

        private Vector3 _currentGoalPosition;
        private bool _isFinding;
        private List<NodeData> _navigationData = new();
        private int _currentIndex;
        private NodeData _nextNode;

        private CancellationTokenSource _findRootToken = new();
        public void GetDestinationInput(Vector3 playerPosition, Vector3 goalPosition)
        {
            if (_currentGoalPosition != goalPosition)
            {
                IsComplete = false;
                if (GetNavigationRoute(playerPosition, goalPosition))
                {
                    _currentGoalPosition = goalPosition;
                }
                InputDirection = Vector2.zero;
                return;
            }


            //探索中・ゴール後は入力を入れない
            if (_isFinding || _navigationData.Count == 0 || _nextNode == null)
            {
                InputDirection = Vector2.zero;
                return;
            }

            float distance = 0;
            distance = (playerPosition - _nextNode.Position).sqrMagnitude;



            //NextNodeに近づいたら次のノードにする
            if (distance <= StopDistance * StopDistance)
            {
                _currentIndex++;
                //ゴール
                if (_currentIndex >= _navigationData.Count)
                {
                    IsComplete = true;
                    _nextNode = null;
                    InputDirection = Vector2.zero;
                    return;
                }
                else
                {
                    IsVaultInput = _nextNode.IsValut;
                    _nextNode = _navigationData[_currentIndex];
                }
            }

            //進む方向を求める
            Vector2 playerPos = new Vector2(playerPosition.x, playerPosition.z);
            Vector2 goalPos = new Vector2(_navigationData[_currentIndex].Position.x, _navigationData[_currentIndex].Position.z);

            IsComplete = false;
            InputDirection = (goalPos - playerPos).normalized;
            return;
        }

        private bool GetNavigationRoute(Vector3 startPos, Vector3 goalPos)
        {
            _isFinding = true;
            _currentIndex = 0;
            CancelFindRoute();
            List<NodeData> result = new();
            try
            {
                result = AStarSystem.FindRoute(startPos, goalPos);
            }
            catch (TimeoutException)
            {
                Debug.Log("経路探索タイムアウト");
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("外部キャンセル");
            }

            if (result == null || result.Count == 0)
            {
                Debug.Log("経路探索エラー");
                return false;
            }

            _navigationData = result;

            if (_navigationData != null && _navigationData.Count > 0)
            {
                _nextNode = _navigationData[0];
            }
            _isFinding = false;
            return true;
        }

        private void CancelFindRoute()
        {
            _findRootToken.Cancel();
            _findRootToken?.Dispose();

            _findRootToken = new();
        }

        public void ShowGizmo()
        {
            if (_navigationData.Count == 0 || _nextNode == null) return;

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_nextNode.Position, 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_navigationData[_navigationData.Count - 1].Position, 0.5f);
        }
    }
}
