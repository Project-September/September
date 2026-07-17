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
        public void Update()
        {
            if (_isOutField) return;
            if(this.transform.position.y <= _outFieldHeight)
            {
                OnOutField();
            }
        }

        public async void OnOutField()
        {
            OnOutFieldEvent.Invoke();
            _isOutField = true;
            UIController.I.ShowOutFieldUI(true);

            await UniTask.WaitForSeconds(_coolTime);

            OnRevive();
        }

        public void OnRevive()
        {
            UIController.I.ShowOutFieldUI(false);
            OnRevivalFieldEvent.Invoke();
            _isOutField = false;
        }
    }
}
