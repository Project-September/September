using UnityEngine;

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
        }

        /// <summary>
        /// フォーカスを解除した時に呼ばれるメソッド
        /// </summary>
        void FocusEndStateChange()
        {
            _playerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
        }
    }
}
