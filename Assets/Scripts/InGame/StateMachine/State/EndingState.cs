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
        [SerializeField] private InGameStatusView _statusView;
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
            UIController.I.ShowResultAnimation();
            Destroy(_statusView.UIRoot.gameObject);
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