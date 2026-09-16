using Fusion;
using InGame.Common;
using InGame.Player.Ability;
using Result;
using System.Linq;
using UnityEngine;

namespace InGame.Player
{
    public class TakamuraMovement : PlayerMovement
    {
        [SerializeField] MimickingParams _mimickingParams;
        [SerializeField] RevealAttackParams _attackParams;
        [Header("エクスカリバー装備時の待機モーション")]
        [SerializeField] AnimationClip _armoryWaitClip;

        AnimationClipPlayer _animationClipPlayer;
        PlayerEquipmentManager _equipmentManager;
        AnimationClip _defaultWaitClip;

#if UNITY_EDITOR
        [Header("Gizmo確認用")]
        [SerializeField] ExhibitType _previewExhibitType = ExhibitType.None;
#endif

        [Networked, HideInInspector]
        public MimicryState CurrentMimicryState { get; set; }

        [Networked, HideInInspector]
        public ScanAbilityPhase CurrentAbilityPhase { get; set; }

        [Networked, HideInInspector]
        public ExhibitType CurrentExhibitType { get; set; } = ExhibitType.None;

        public override void Spawned()
        {
            base.Spawned();

            _animationClipPlayer = GetComponent<AnimationClipPlayer>();
            _equipmentManager = GetComponent<PlayerEquipmentManager>();
            if (!_animationClipPlayer || !_equipmentManager) return;

            _defaultWaitClip = _animationClipPlayer.WaitClip;
            _equipmentManager.Equipped += OnEquipmentChanged;
            _equipmentManager.Unequipped += OnEquipmentChanged;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_equipmentManager)
            {
                _equipmentManager.Equipped -= OnEquipmentChanged;
                _equipmentManager.Unequipped -= OnEquipmentChanged;
            }

            if (_animationClipPlayer)
                _animationClipPlayer.SetWaitClip(_defaultWaitClip);
        }

        /// <summary>エクスカリバーの装備状態に合わせてタカムラの待機姿勢を切り替える。</summary>
        void OnEquipmentChanged(Equipment _)
        {
            if (!_animationClipPlayer || !_equipmentManager) return;

            bool hasArmory = _equipmentManager.CurrentEquipments.Values
                .Any(equipment => equipment.Type == EquipmentType.Armory);
            _animationClipPlayer.SetWaitClip(hasArmory ? _armoryWaitClip : _defaultWaitClip);
        }

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

        /// <summary>
        /// 擬態解除攻撃の攻撃範囲を取得するメソッド
        /// </summary>
        /// <returns>擬態解除攻撃の攻撃範囲</returns>
        public float GetRevealAttackRadius()
        {
            if (_attackParams == null) return 0;

#if UNITY_EDITOR
            // Editor停止中は確認用に選択した展示物の攻撃範囲を使用する
            if (!Application.isPlaying)
            {
                return _attackParams.GetRadius(_previewExhibitType);
            }
#endif

            // NetworkObjectの生成前はNetworkedプロパティを参照できないためデフォルト値を使用する
            var exhibitType = Object != null
                ? CurrentExhibitType
                : ExhibitType.None;

            return _attackParams.GetRadius(exhibitType);
        }

        private void OnDrawGizmosSelected()
        {
            DrawRevealAttackRadius();
        }

        /// <summary>
        /// 擬態解除攻撃の攻撃範囲を描画するメソッド
        /// </summary>
        void DrawRevealAttackRadius()
        {
            var radius = GetRevealAttackRadius();
            if (radius <= 0) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
