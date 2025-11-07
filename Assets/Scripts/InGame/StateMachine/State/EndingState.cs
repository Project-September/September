using System;
using CRISound;
using Cysharp.Threading.Tasks;
using September.InGame.Common;
using UniRx;
using UnityEngine;

namespace September.Common
{
    public class EndingState : ImtStateMachine<InGameManager>.State
    {
        // ToDo : UI工事中
        private readonly Subject<Unit> StartAnimation = new();
        protected internal override void OnEnter()
        {
            Context.GameEnded?.Invoke();
            GameEnded().Forget();
        }

        private async UniTaskVoid GameEnded()
        {
            CRIAudio.PlaySE("ALLCue", SoundCues.SE.UI_GameFinish.Name); // タイムアップ音
            await UniTask.Delay(TimeSpan.FromSeconds(Context.TimerData.EndGameDelay));
            Context.Cts.Cancel();
            
            ShowCursor();
            //if(!string.IsNullOrEmpty(Context.CurrentBGM)) CRIAudio.StopBGM("BGM", Context.CurrentBGM);
            // ここにエンド処理
            PlayerDatabase.Instance.Server_PushResultToClients();
            StartAnimation.OnNext(Unit.Default);
            //UIPresenter.I.ShowResultAnimation();
            GameInput.I.ToggleMoveInput(false);
            GameInput.I.ToggleLookInput(false);
            CRIAudio.StopSE();      // 鳴ってるSEを止める 2DPlayer用
            CRIAudio.Stop3DSEAll(); // 3DPlayer用
            BGMManager.ChangeBGM("Result"); // リザルトシーン用のBGMを再生
        }
        
        private void ShowCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}