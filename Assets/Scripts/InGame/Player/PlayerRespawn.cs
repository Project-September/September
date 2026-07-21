using System;
using Cysharp.Threading.Tasks;
using September.InGame.UI;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private float _coolTime;
        [SerializeField] private float _outFieldHeight;

        private bool _isOutField = false;

        public event Action OnOutFieldEvent;
        public event Action OnRevivalFieldEvent;

        private void Update()
        {
            if (_isOutField) return;
            if (this.transform.position.y <= _outFieldHeight)
            {
                OnOutField();
            }
        }

        private async void OnOutField()
        {
            OnOutFieldEvent?.Invoke();
            _isOutField = true;
            UIController.I.ShowOutFieldUI(true);

            await UniTask.WaitForSeconds(_coolTime);

            OnRevive();
        }

        private void OnRevive()
        {
            UIController.I.ShowOutFieldUI(false);
            OnRevivalFieldEvent?.Invoke();
            _isOutField = false;
        }
    }
}