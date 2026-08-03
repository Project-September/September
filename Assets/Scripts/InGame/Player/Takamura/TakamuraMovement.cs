using Fusion;
using UnityEngine;

namespace InGame.Player
{
    public class TakamuraMovement : PlayerMovement
    {
        [Header("擬態状態"), SerializeField] MimicryState _currentMimicryState = MimicryState.Default;
        [Header("アビリティ発動状態"), SerializeField] ScanAbilityPhase _phase = ScanAbilityPhase.Default;
        [SerializeField] MimickingParams _mimickingParams;

        [Networked]
        public MimicryState CurrentMimicryState
        {
            get => _currentMimicryState;
            set
            {
                if (_currentMimicryState != value)
                    _currentMimicryState = value;
            }
        }

        [Networked]
        public ScanAbilityPhase CurrentAbilityPhase
        {
            get => _phase;
            set
            {
                if (_phase != value)
                    _phase = value;
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
