using System;
using Fusion;
using InGame.Interact;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace September.Lesson
{
    /// <summary>
    /// 授業用: インタラクトしたプレイヤーの移動速度を大きく上げる展示物の効果。
    /// </summary>
    [Serializable]
    public sealed class LessonSpeedInteractEffect : CharacterInteractEffectBase
    {
        // 速度アップが続く秒数。
        [SerializeField, Min(0.1f), InspectorName("効果時間（秒）"), Tooltip("インタラクトしてから速度アップが続く時間です。")]
        private float _duration = 10f;
        // 何倍の速さにするか。5なら通常の5倍。
        [SerializeField, Min(1f), InspectorName("移動速度の倍率"), Tooltip("通常の移動速度を何倍にするか指定します。5なら5倍です。")]
        private float _speedMultiplier = 5f;

        /// <summary>
        /// 展示物へのインタラクトが完了したとき、触れたプレイヤーに速度アップを付ける。
        /// </summary>
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            // context.Interactorはプレイヤーの識別番号。PlayerRefへ変換して実体を探す。
            var playerRef = PlayerRef.FromEncoded(context.Interactor);
            if (!PlayerDatabase.Instance.PlayerObjectDic.TryGet(playerRef, out var playerObject)) return;

            // 授業で書く部分：プレイヤーから移動コンポーネントを取得して、速度の倍率を変更する。
        }

        /// <summary>
        /// 展示物システムが効果を複製するときに呼ぶ。Inspectorで設定した値を新しい効果へ引き継ぐ。
        /// </summary>
        public override CharacterInteractEffectBase Clone()
        {
            return new LessonSpeedInteractEffect
            {
                _duration = _duration,
                _speedMultiplier = _speedMultiplier,
            };
        }
    }
}
