using Fusion;
using September.InGame.Common;
using September.InGame.UI;

namespace September.Common
{
    public class PlayingState : ImtStateMachine<InGameManager>.State
    {
        [Networked] private TickTimer TickTimer { get; set; }
        private bool _hasTriggeredTimeNotice;
        private float _timeRemaining;
        private float _gameTime;
        private int _lastScore = -1;
        protected internal override void OnEnter()
        {
            //  制限時間カウント開始
            TickTimer = TickTimer.CreateFromSeconds(Context.Runner, Context.TimerData.GameTime);
            Context.GameStarted?.Invoke();
            _hasTriggeredTimeNotice = false;
            _timeRemaining = Context.TimerData.TimeRemaining;
            _gameTime = Context.TimerData.GameTime;
            if(PlayerDatabase.Instance) PlayerDatabase.Instance.ChangedDataAction += OnChangedData;
        }

        protected internal override void OnNetworkFixedUpdate()
        {
            //  _timeRemainingは残りxx秒と表示するゲーム残り時間
            //  ゲーム時間が_timeRemaining以上かつ、タイマーが_timeRemaining未満かつ、_hasTriggeredTimeNoticeがfalseの際
            if (_gameTime >= _timeRemaining && TickTimer.RemainingTime(Context.Runner) < _timeRemaining && !_hasTriggeredTimeNotice)
            {
                _hasTriggeredTimeNotice = true;
                UIController.I.TimeOverlayMessage?.Invoke(TimeMessageType.TimeRemainingAlert);
            }
            
            // 時間が0になったら次のステートに遷移する
            if (HasStateAuthority && TickTimer.Expired(Context.Runner))
            {
                TickTimer = TickTimer.None;
                Context.Rpc_SendEvent((int)StateEventId.Finish);
            }
        }

        protected internal override void OnExit()
        {
            PlayerDatabase.Instance.ChangedDataAction -= OnChangedData;
        }

        private void OnChangedData(NetworkDictionary<PlayerRef, SessionPlayerData> dic)
        {
            if (!dic.TryGet(Context.Runner.LocalPlayer, out SessionPlayerData data)) return;
            if (_lastScore != data.Score) 
            {
                UIController.I?.OnChangeScore(data.Score);
                _lastScore = data.Score;
            }
        }
    }
}