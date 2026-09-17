using DG.Tweening;
using InGame.Player.Ult;
using September.Common;
using September.InGame.Common;
using UnityEngine;
using UnityEngine.UI;

namespace September.InGame.Ult
{
    public class UltUI : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private float _easeDuration = 0.2f;

        private InGameManager _inGameManager;
        private UltCondition _model;

        private void Start()
        {
            _image.fillAmount = 0f;

            _inGameManager = StaticServiceLocator.Instance.Get<InGameManager>();
            
            // プレイヤーがスポーンされた後に処理を行う
            _inGameManager.GameStarted += BindCurrentPlayer;
        }

        private void Update()
        {
            // 擬態では操作PlayerのPrefab自体が交換されるため、参照が変わったら再購読する。
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
            if (_model) SetGaugeProgress(_model.Progress);
        }

        private void SetGaugeProgress(float ratio)
        {
            _image.DOFillAmount(ratio, _easeDuration);
        }
    }
}
