using UnityEngine;
using Fusion;
using September.Common;

namespace InGame.Player
{
    // TakamuraScanner.csのコアファイル
    /// <summary>タカムラキャラのスキャンスキルを管理するクラス</summary>
    public partial class TakamuraScanner : NetworkBehaviour, IAfterTick
    {
        [Header("カメラ制御")]
        [SerializeField, Tooltip("フォーカス時のカメラの位置")] Vector3 _focusPosition = new(0.5f, 1, -2);
        [SerializeField, Tooltip("カメラの移動時間")] float _cameraMoveDuration = 0.2f;

        PlayerManager _playerManager;
        TakamuraMovement _tkmrMovement;
        CameraController _cameraController;
        Camera _camera;
        NetworkButtons _preInput;

        public override void Spawned()
        {
            _playerManager = GetComponent<PlayerManager>();
            _tkmrMovement = GetComponent<TakamuraMovement>();
            if (HasInputAuthority) InitInputAuthority();
            if (HasStateAuthority) InitStateAuthority();
        }

        public override void FixedUpdateNetwork()
        {
            // inputにはこのオブジェクトに対する入力権限があるプレイヤーからの入力が入る
            if (!GetInput<PlayerInput>(out var input)) return;

            Ability2Flow(input);
        }

        public void AfterTick()
        {
            // Tickの終わりに前の入力を保存する
            if (GetInput<PlayerInput>(out var input))
            {
                _preInput = input.Buttons;
            }
        }

        /// <summary>
        /// Ability2の処理の流れを持つメソッド
        /// </summary>
        /// <param name="input">このオブジェクトに対する入力権限を持つプレイヤーからの入力</param>
        void Ability2Flow(PlayerInput input)
        {
            // フォーカスをあてる
            if (input.Buttons.WasPressed(_preInput, PlayerButtons.Ability2))
            {
                if (HasInputAuthority)
                {
                    FocusStartEffective();
                }
                if (HasStateAuthority)
                {
                    FocusStartStateChange();
                }
            }

            // フォーカス中
            if (input.Buttons.IsSet(PlayerButtons.Ability2))
            {
                if (HasInputAuthority)
                {
                    FocusEffective();
                }
            }

            // フォーカス解除
            if (input.Buttons.WasReleased(_preInput, PlayerButtons.Ability2))
            {
                if (HasInputAuthority)
                {
                    FocusEndEffective();
                }
                if (HasStateAuthority)
                {
                    FocusEndStateChange();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawScanArea();
        }
    }
}
