using System.Collections.Generic;

using InGame.Player.Ult;
using September.Common;
using September.InGame.Common;
using UnityEngine;

namespace September
{
    public class UltGlowView : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _glowObjects;
        private InGameManager _inGameManager;
        private UltCondition _model;

        private void Start()
        {
            for (int i = 0; i < _glowObjects.Count; i++)
            {
                _glowObjects[i].SetActive(false);
            }

            _inGameManager = StaticServiceLocator.Instance.Get<InGameManager>();

            // プレイヤーがスポーンされた後に処理を行う
            _inGameManager.GameStarted += BindCurrentPlayer;
        }

        private void Update()
        {
            BindCurrentPlayer();
        }

        private void BindCurrentPlayer()
        {
            var runner = _inGameManager?.Runner;
            if (runner == null || !runner.TryGetPlayerObject(runner.LocalPlayer, out var player)) return;
            if (!player.TryGetComponent(out UltCondition nextModel) || nextModel == _model) return;

            if (_model) _model.OnProgressChanged -= Refresh;
            _model = nextModel;
            _model.OnProgressChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_model) _model.OnProgressChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_model) SetGlowObject(_model.Progress >= 1f);
        }

        private void SetGlowObject(bool isActive)
        {
            for (int i = 0; i < _glowObjects.Count; i++)
            {
                _glowObjects[i].SetActive(isActive);
            }
        }
    }
}
