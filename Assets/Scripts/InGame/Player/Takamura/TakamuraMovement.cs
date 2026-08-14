using Fusion;
using UnityEngine;

namespace InGame.Player
{
    public class TakamuraMovement : PlayerMovement
    {
        [SerializeField] MimickingParams _mimickingParams;

        [Networked, HideInInspector]
        public MimicryState CurrentMimicryState{ get; set; }

        [Networked, HideInInspector]
        public ScanAbilityPhase CurrentAbilityPhase {  get; set; }

        protected override float GetMoveMagnification()
        {
            // メソッドoverride前の計算結果を取得
            var result = base.GetMoveMagnification();

            // 現在の"擬態状態"に応じた移動速度倍率を取得し乗算
            result *= _mimickingParams.TryGetParams(CurrentMimicryState, out var param)
                && param != null                // パラメータクラスを正常に取得できたか
                ? param.SpeedMagnification      // パラメータクラスに定義されている移動速度倍率
                : 1;                            // 倍率なし

            return result;
        }
    }
}
