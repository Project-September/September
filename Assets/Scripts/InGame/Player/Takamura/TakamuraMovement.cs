using UnityEngine;

namespace InGame.Player
{
    public class TakamuraMovement : PlayerMovement
    {
        [Header("初期状態"), SerializeField] MimicryState _currentMimicryState = MimicryState.Default;
        [SerializeField] MimickingParams _mimickingParams;

        public MimicryState CurrentMimicryState
        {
            get => _currentMimicryState;
            set
            {
                if (_currentMimicryState != value)
                    _currentMimicryState = value;
            }
        }

        protected override float GetMoveMagnification()
        {
            // メソッドoverride前の計算結果を取得
            var result = base.GetMoveMagnification();

            // 現在の"擬態状態"に応じた移動速度倍率を取得し乗算
            result *= _mimickingParams.TryGetParams(_currentMimicryState, out var param)
                && param != null                // パラメータクラスを正常に取得できたか
                ? param.SpeedMagnification      // パラメータクラスに定義されている移動速度倍率
                : 1;                            // 倍率なし

            return result;
        }
    }
}
