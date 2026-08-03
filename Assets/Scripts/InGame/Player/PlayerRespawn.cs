using System;
using Cysharp.Threading.Tasks;
using Fusion;
using September.InGame.UI;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerRespawn : NetworkBehaviour
    {
        [SerializeField] private float _coolTime;
        [SerializeField] private float _outFieldHeight;

        [Networked, OnChangedRender(nameof(OnOutFieldStateChanged))]
        private bool IsOutField { get; set; }

        public event Action OnOutFieldEvent;
        public event Action OnRevivalFieldEvent;

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            if (IsOutField) return;

            if (this.transform.position.y <= _outFieldHeight)
            {
                IsOutField = true;
            }
        }

        private void OnOutFieldStateChanged()
        {
            if (HasInputAuthority)
            {
                UIController.I.ShowOutFieldUI(IsOutField);
            }

            if (IsOutField)
            {
                OnOutFieldEvent?.Invoke();
                RespawnAsync().Forget();
            }
        }

        private async UniTaskVoid RespawnAsync()
        {
            await UniTask.WaitForSeconds(_coolTime);
            OnRevivalFieldEvent?.Invoke();
            IsOutField = false;
        }
    }
}
