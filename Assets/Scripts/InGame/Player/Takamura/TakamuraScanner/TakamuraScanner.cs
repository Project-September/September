using System;
using System.Collections.Generic;
using Fusion;
using InGame.Interact;
using September.Common;
using UnityEngine;

namespace InGame.Player
{
    /// <summary>タカムラキャラのスキャンスキルを管理するテストクラス</summary>
    public class TakamuraScanner : NetworkBehaviour, IAfterTick
    {
        PlayerManager _playerManager;
        TakamuraMovement _tkmrMovement;
        CameraController _cameraController;
        Camera _camera;
        NetworkButtons _preInput;

        [Networked, OnChangedRender(nameof(OnMimicTargetChanged))]
        NetworkId MimicTargetId { get; set; }

        [Header("カメラ制御")]
        [SerializeField, Tooltip("フォーカス時のカメラの位置")] Vector3 _focusPosition = new(0.5f, 1, -2);
        [SerializeField, Tooltip("カメラの移動時間")] float _cameraMoveDuration = 0.2f;
        [Header("カメラスキャンの有効領域についてのパラメータ")]
        [SerializeField, Tooltip("擬態対象の候補にできる最大距離")] float _scannableMaxDistance = 10f;
        [SerializeField, Tooltip("演出用キャンバス")] ScannerCanvas _scannerCanvas;
        [Header("ガワ")]
        [SerializeField] TakamuraVisual _visual;

        /// <summary>シーン上にある展示物の配列</summary>
        TakamuraScanTarget[] _interactables;
        /// <summary>NetworkIDごとの展示物</summary>
        readonly Dictionary<NetworkId, TakamuraScanTarget> _targetByNetworkId = new();
        /// <summary>入力権限側で選択中の展示物のIndex</summary>
        int _focusIndex = -1;

        /// <summary>次のTickで行う擬態状態の変更</summary>
        StateChangeType _pendingStateChange = StateChangeType.None;
        int _stateChangeTick = -1;

        enum StateChangeType
        {
            None,
            Mimic,
            Reveal
        }

        public TakamuraVisual Visual => _visual;

        public override void Spawned()
        {
            _playerManager = GetComponent<PlayerManager>();
            _tkmrMovement = GetComponent<TakamuraMovement>();
            _interactables = FindObjectsByType<TakamuraScanTarget>(FindObjectsSortMode.None);
            CreateTargetDictionary();
            if (_scannerCanvas != null) _scannerCanvas.gameObject.SetActive(false);
            if (HasInputAuthority) InitInputAuthority();
            if (HasStateAuthority) InitStateAuthority();

            ChangeVisual();
        }

        /// <summary>
        /// NetworkIDから展示物を取得するためのDictionaryを作るメソッド
        /// </summary>
        void CreateTargetDictionary()
        {
            _targetByNetworkId.Clear();

            foreach (var target in _interactables)
            {
                if (target == null) continue;

                var networkObject = target.GetComponentInParent<NetworkObject>();
                if (networkObject == null)
                {
                    Debug.LogError($"{target.name}にNetworkObjectがありません");
                    continue;
                }

                if (_targetByNetworkId.ContainsKey(networkObject.Id))
                {
                    Debug.LogError($"{networkObject.name}のNetworkIDが重複しています");
                    continue;
                }

                _targetByNetworkId.Add(networkObject.Id, target);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority) ApplyPendingStateChange();

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
            if (!_tkmrMovement.IsGround) return;

            if (_tkmrMovement.CurrentMimicryState == MimicryState.Default)
            {
                // フォーカスをあてる
                if (input.Buttons.WasPressed(_preInput, PlayerButtons.Ability2))
                {
                    if (HasInputAuthority) FocusStartEffective();
                    if (HasStateAuthority) FocusStartStateChange();
                }

                // フォーカス中
                if (input.Buttons.IsSet(PlayerButtons.Ability2))
                {
                    if (HasInputAuthority && _scannerCanvas.gameObject.activeSelf) FocusEffective(input);

                    // 擬態する
                    if (input.Buttons.WasPressed(_preInput, PlayerButtons.Attack)
                        && HasInputAuthority)
                    {
                        if (_focusIndex == -1) return;

                        var target = _interactables[_focusIndex];
                        if (target == null) return;
                        var networkObject = target.GetComponentInParent<NetworkObject>();
                        if (networkObject == null) return;

                        RPC_Mimic(networkObject.Id);
                        Mimic();
                    }
                }

                // フォーカス解除
                if (input.Buttons.WasReleased(_preInput, PlayerButtons.Ability2))
                {
                    if (HasInputAuthority)
                    {
                        FocusEndEffective();
                    }
                    if (HasStateAuthority) FocusEndStateChange();
                }
            }
            else if (_tkmrMovement.CurrentMimicryState == MimicryState.MimicExhibit)
            {
                // 擬態解除する
                if (input.Buttons.WasPressed(_preInput, PlayerButtons.Attack)
                    && HasInputAuthority)
                {
                    RPC_Reveal();
                }
            }
        }

        #region InputAuthority
        /// <summary>
        /// 入力権限がある場合の初期化メソッド
        /// </summary>
        void InitInputAuthority()
        {
            _cameraController = GetComponent<CameraController>();
            _camera = Camera.main;
            _scannerCanvas.gameObject.SetActive(true);
            _scannerCanvas.ChangeImageVisibility(false);
        }

        /// <summary>
        /// フォーカスを当て始めた時の演出メソッド
        /// </summary>
        void FocusStartEffective()
        {
            _scannerCanvas.gameObject.SetActive(true);
            _scannerCanvas.ChangeImageVisibility(false);
            _cameraController.ChangeOffset(_focusPosition, _cameraMoveDuration);
        }

        /// <summary>
        /// フォーカス中の演出メソッド
        /// </summary>
        /// <param name="input">このTickで同期されているプレイヤー入力</param>
        void FocusEffective(PlayerInput input)
        {
            // Cinemachineの描画結果をFixedUpdateNetworkで直接参照すると、
            // プレイヤー回転との追従が循環してカメラが揺れるため入力値を使用する
            _tkmrMovement.SetRotationDirection(input.DesiredLookDirection);
            UpdateNearestExhibit();
            FocusExhibit();
        }

        /// <summary>
        /// フォーカスを解除した時の演出メソッド
        /// </summary>
        void FocusEndEffective()
        {
            _cameraController.ResetOffset(_cameraMoveDuration);
            _scannerCanvas.ChangeImageVisibility(false);
            _scannerCanvas.gameObject.SetActive(false);
            _focusIndex = -1;
        }

        /// <summary>
        /// より近い展示物を計算して取得するメソッド
        /// </summary>
        void UpdateNearestExhibit()
        {
            var minDistance = float.MaxValue;
            _focusIndex = -1;
            foreach (var interactable in _interactables)
            {
                if (interactable == null) continue;
                if (!interactable.gameObject.activeSelf) continue;

                // 展示物の座標を取得
                var col = interactable.GetComponentInChildren<Collider>();
                Vector3 pos = col != null
                    ? col.bounds.center
                    : interactable.transform.position;

                // カメラに写っているかを確認
                var viewportPoint = _camera.WorldToViewportPoint(pos);
                if (0 <= viewportPoint.x && viewportPoint.x <= 1
                    && 0 <= viewportPoint.y && viewportPoint.y <= 1
                    && 0 <= viewportPoint.z)
                {
                    // カメラに写っていたら距離を計算
                    var distance = Vector3.SqrMagnitude(pos - transform.position);
                    if (distance <= _scannableMaxDistance * _scannableMaxDistance)
                    {
                        if (distance < minDistance)
                        {
                            // 判定距離内かつより近いオブジェクトであれば擬態対象にする
                            minDistance = distance;
                            _focusIndex = Array.IndexOf(_interactables, interactable);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 擬態対象の位置を計算して描画指示を出すメソッド
        /// </summary>
        void FocusExhibit()
        {
            var scanned = _focusIndex != -1;
            _scannerCanvas.ChangeImageVisibility(scanned);

            // 展示物かどうかの最終確認ができたら描画処理
            if (scanned)
            {
                // 展示物の座標をスクリーン座標に変換
                var target = _interactables[_focusIndex];
                if (target == null) return;
                var col = target.GetComponentInChildren<Collider>();
                var pos = _camera.WorldToScreenPoint(
                    col != null
                    ? col.bounds.center
                    : target.transform.position);

                // 擬態対象であることを示すImageを展示物の位置へ移動
                _scannerCanvas.SetImageOverExhibit(pos);
            }
        }

        /// <summary>
        /// 擬態するメソッド
        /// </summary>
        void Mimic()
        {
            FocusEndEffective();
        }
        #endregion

        #region StateAuthority
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

        /// <summary>
        /// 擬態解除するときに呼ばれるメソッド
        /// </summary>
        void RevealStateChange()
        {
            _tkmrMovement.CurrentMimicryState = MimicryState.Default;
        }
        #endregion

        #region Network
        /// <summary>
        /// 入力権限側で決定した擬態対象を状態変更権限側へ送るメソッド
        /// </summary>
        /// <param name="targetId">擬態対象のNetworkID</param>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        void RPC_Mimic(NetworkId targetId)
        {
            if (!_targetByNetworkId.TryGetValue(targetId, out var target)) return;
            var interactable = target.GetComponentInParent<InteractableBase>();
            if (interactable == null) return;
            if (_tkmrMovement.CurrentMimicryState != MimicryState.Default) return;

            // コライダーの不都合を考えて少し上に移動
            transform.position += Vector3.up;

            // 擬態対象の情報を状態変更権限側で確定する
            MimicTargetId = targetId;
            _tkmrMovement.CurrentExhibitType = interactable.ExhibitType;

            // このTickのAttack入力を攻撃条件が判定してから擬態状態を変更する
            ReserveStateChange(StateChangeType.Mimic);
        }

        /// <summary>
        /// 擬態解除を状態変更権限側へ要求するメソッド
        /// </summary>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        void RPC_Reveal()
        {
            if (_tkmrMovement.CurrentMimicryState != MimicryState.MimicExhibit) return;

            // コライダーの不都合を考えて少し上に移動
            transform.position += Vector3.up;

            // このTickのAttack入力を擬態解除攻撃として判定してから状態を戻す
            ReserveStateChange(StateChangeType.Reveal);
        }

        /// <summary>
        /// 擬態状態の変更を次のTickに予約するメソッド
        /// </summary>
        /// <param name="stateChange">予約する状態変更</param>
        void ReserveStateChange(StateChangeType stateChange)
        {
            _pendingStateChange = stateChange;
            _stateChangeTick = Runner.Tick + 1;
        }

        /// <summary>
        /// 予約されている擬態状態の変更を行うメソッド
        /// </summary>
        void ApplyPendingStateChange()
        {
            if (_pendingStateChange == StateChangeType.None) return;
            if (Runner.Tick < _stateChangeTick) return;

            switch (_pendingStateChange)
            {
                case StateChangeType.Mimic:
                    MimicStateChange();
                    break;
                case StateChangeType.Reveal:
                    MimicTargetId = default;
                    RevealStateChange();
                    break;
            }

            _pendingStateChange = StateChangeType.None;
            _stateChangeTick = -1;
        }

        /// <summary>
        /// 擬態対象が変更された時に見た目を変更するメソッド
        /// </summary>
        void OnMimicTargetChanged()
        {
            ChangeVisual();
        }

        /// <summary>
        /// 現在の擬態対象に合わせて見た目を変更するメソッド
        /// </summary>
        void ChangeVisual()
        {
            if (_visual == null) return;

            if (MimicTargetId == default)
            {
                _visual.Reveal();
                return;
            }

            if (!_targetByNetworkId.TryGetValue(MimicTargetId, out var target)) return;

            // ガワを変える
            _visual.Mimic(target);
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            DrawScanArea();
        }

        #region Gizmos
        /// <summary>
        /// スキャン領域を描画するメソッド
        /// </summary>
        void DrawScanArea()
        {
            if (_camera == null) return;

            Gizmos.color = Color.green;
            // カメラの描画範囲の四隅かつ最大スキャン距離
            Vector3 bl = _camera.ViewportToWorldPoint(new Vector3(0, 0, _scannableMaxDistance));
            Vector3 br = _camera.ViewportToWorldPoint(new Vector3(1, 0, _scannableMaxDistance));
            Vector3 tr = _camera.ViewportToWorldPoint(new Vector3(1, 1, _scannableMaxDistance));
            Vector3 tl = _camera.ViewportToWorldPoint(new Vector3(0, 1, _scannableMaxDistance));

            // カメラと同じような線を描く
            var cameraPos = _camera.transform.position;
            var blCameraPos = cameraPos + (bl - cameraPos).normalized * _scannableMaxDistance;
            var brCameraPos = cameraPos + (br - cameraPos).normalized * _scannableMaxDistance;
            var trCameraPos = cameraPos + (tr - cameraPos).normalized * _scannableMaxDistance;
            var tlCameraPos = cameraPos + (tl - cameraPos).normalized * _scannableMaxDistance;
            Gizmos.DrawLine(blCameraPos, brCameraPos);
            Gizmos.DrawLine(brCameraPos, trCameraPos);
            Gizmos.DrawLine(trCameraPos, tlCameraPos);
            Gizmos.DrawLine(tlCameraPos, blCameraPos);
            Gizmos.DrawLine(blCameraPos, cameraPos);
            Gizmos.DrawLine(brCameraPos, cameraPos);
            Gizmos.DrawLine(trCameraPos, cameraPos);
            Gizmos.DrawLine(tlCameraPos, cameraPos);

            // スキャン範囲の先端部分を描画
            var segments = 36;
            var rightUpLine = tr - bl;
            var leftUpLine = br - tl;
            for (int i = 0; i < segments; i++)
            {
                var rightUpLineElement1 = ((bl + rightUpLine * i / segments) - cameraPos).normalized * _scannableMaxDistance;
                var rightUpLineElement2 = ((bl + rightUpLine * (i + 1) / segments) - cameraPos).normalized * _scannableMaxDistance;
                var leftUpLineElement3 = ((tl + leftUpLine * i / segments) - cameraPos).normalized * _scannableMaxDistance;
                var leftUpLineElement4 = ((tl + leftUpLine * (i + 1) / segments) - cameraPos).normalized * _scannableMaxDistance;

                Gizmos.DrawLine(cameraPos + rightUpLineElement1, cameraPos + rightUpLineElement2);
                Gizmos.DrawLine(cameraPos + leftUpLineElement3, cameraPos + leftUpLineElement4);
            }
        }
        #endregion
    }
}
