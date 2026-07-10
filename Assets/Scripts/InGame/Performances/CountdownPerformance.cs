using System;
using Cysharp.Threading.Tasks;
using September.InGame.UI;
using UnityEngine;

namespace September.InGame.Performances
{
    [Serializable]
    public class CountdownPerformance : IGameStartPerformance
    {
        [SerializeField] private bool _enabled = true;

        public bool Enabled => _enabled;

        public async UniTask RunPerformance(IGameStartPerformance.Context ctx)
        {
            // カメラが変わったタイミングで視点入力だけ有効化
            if (ctx.Runner.IsServer) ctx.ToggleInputs(false, false, true);
            //  カメラが元の位置に戻るまで待つ
            await UniTask.WaitForSeconds(1.5f);
            //  カウントダウン開始
            if (UIController.I.TimeOverlayMessage != null)
            {
                await UIController.I.TimeOverlayMessage.Invoke(TimeMessageType.Countdown);
            }
        }
    }
}