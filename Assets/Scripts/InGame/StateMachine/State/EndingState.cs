using System;
using CRISound;
using Cysharp.Threading.Tasks;
using September.InGame.Common;
using September.InGame.UI;
using UnityEngine;

namespace September.Common
{
    public class EndingState : ImtStateMachine<InGameManager>.State
    {
        protected internal override void OnEnter()
        {
            GameEnded().Forget();
        }

        private async UniTaskVoid GameEnded()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(Context.TimerData.EndGameDelay));
            Context.Cts.Cancel();
            
            ShowCursor();
            if(!string.IsNullOrEmpty(Context.CurrentBGM)) CRIAudio.StopBGM("BGM", Context.CurrentBGM);
            // ここにエンド処理
            PlayerDatabase.Instance.Server_PushResultToClients();
            UIController.I.ShowResultAnimation();
            GameInput.I.ToggleMoveInput(false);
            BGMManager.ChangeBGM("Result");
        }
        private void ShowCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}