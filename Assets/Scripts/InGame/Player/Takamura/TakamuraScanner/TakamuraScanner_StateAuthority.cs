namespace InGame.Player
{
    // 状態変更についての処理をまとめたファイル
    public partial class TakamuraScanner
    {
        /// <summary>
        /// 状態変更権限がある場合の初期化メソッド
        /// </summary>
        void InitStateAuthority()
        {
            // いらないかも
        }

        /// <summary>
        /// フォーカスを当て始めた時に呼ばれるメソッド
        /// </summary>
        void FocusStartStateChange()
        {
            _playerManager.SetControlState(PlayerManager.PlayerControlState.InputLocked);
            _tkmrMovement.CurrentAbilityPhase = ScanAbilityPhase.Scanning;
        }

        /// <summary>
        /// フォーカスを解除した時に呼ばれるメソッド
        /// </summary>
        void FocusEndStateChange()
        {
            _playerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
            _tkmrMovement.CurrentAbilityPhase = ScanAbilityPhase.Default;
        }

        /// <summary>
        /// 擬態するときに呼ばれるメソッド
        /// </summary>
        void MimicStateChange()
        {
            _tkmrMovement.CurrentMimicryState = MimicryState.MimicExhibit;
            FocusEndStateChange();
        }
    }
}
