using Fusion;
using InGame.Interact;
using September.Common;
using UnityEngine;

namespace InGame.Player
{
    // TakamuraScanner.csのコアファイル
    /// <summary>タカムラキャラのスキャンスキルを管理するクラス</summary>
    public partial class TakamuraScanner : NetworkBehaviour, IAfterTick
    {
        PlayerManager _playerManager;
        TakamuraMovement _tkmrMovement;
        CameraController _cameraController;
        Camera _camera;
        NetworkButtons _preInput;
        [Networked] int Index { get; set; } = -1;

        public override void Spawned()
        {
            _playerManager = GetComponent<PlayerManager>();
            _tkmrMovement = GetComponent<TakamuraMovement>();
            _scannerCanvas.gameObject.SetActive(false);
            _interactables = FindObjectsByType<TakamuraScanTarget>(FindObjectsSortMode.None);
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
            if (_tkmrMovement.CurrentMimicryState == MimicryState.Default)
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

                    // 擬態する
                    if (input.Buttons.WasPressed(_preInput, PlayerButtons.Attack))
                    {
                        if (Index == -1) return;

                        // コライダーの不都合を考えて少し上に移動
                        transform.position += Vector3.up;
                        // ガワを変える
                        var scanned = _interactables[Index];
                        _visual.Mimic(scanned);

                        if (HasInputAuthority)
                        {
                            Mimic();
                        }
                        if (HasStateAuthority)
                        {
                            MimicStateChange();
                        }
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
            else if (_tkmrMovement.CurrentMimicryState == MimicryState.MimicExhibit)
            {
                // 擬態する
                if (input.Buttons.WasPressed(_preInput, PlayerButtons.Attack))
                {
                    // コライダーの不都合を考えて少し上に移動
                    transform.position += Vector3.up;
                    // ガワを変える
                    _visual.Reveal();

                    if (HasStateAuthority)
                    {
                        RevealStateChange();
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawScanArea();
        }
    }
}
