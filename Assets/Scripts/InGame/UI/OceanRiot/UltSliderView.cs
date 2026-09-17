using DG.Tweening;
using InGame.Player.Ult;
using September.Common;
using September.InGame.Common;
using UnityEngine;

namespace September
{
    public class UltSliderView : MonoBehaviour
    {
        [SerializeField] private float _easeDuration = 0.2f;
        private InGameManager _inGameManager;
        private UltCondition _model;

        private void Start()
        {
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
            if (_model) SetGaugeRotation(_model.Progress);
        }

        private void SetGaugeRotation(float ratio)
        {
            var angle = -(Mathf.Clamp01(ratio) * 360f);
            transform.DOLocalRotate(new Vector3(0f, 0f, angle), _easeDuration);
        }
    }
}
