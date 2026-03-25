using InGame.Player.Ult;
using September.Common;
using September.InGame.Common;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace September.InGame.Ult
{
    public class UltUI : MonoBehaviour
    {
        [SerializeField] private Slider _gauge;

        private void Start()
        {
            var inGameManager = StaticServiceLocator.Instance.Get<InGameManager>();
            inGameManager.GameStarted += () =>
            {
                var runner = inGameManager.Runner;

                if (!inGameManager.PlayerDataDic.TryGetValue(runner.LocalPlayer, out var player))
                {
                    Debug.LogError("[UltUI] No local player data found");
                    return;
                }

                if (!player.gameObject.TryGetComponent<UltCondition>(out var model))
                {
                    Debug.LogError("[UltUI] No UltCondition found");
                    return;
                }

                this.UpdateAsObservable()
                    .Select(_ => model.Progress)
                    .DistinctUntilChanged()
                    .Subscribe(SetGaugeProgress);
            };
        }

        private void SetGaugeProgress(float ratio)
        {
            _gauge.value = ratio;
        }
    }
}