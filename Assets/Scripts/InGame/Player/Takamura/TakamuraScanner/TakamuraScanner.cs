using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Common;
using InGame.Interact;
using InGame.Player.Ability;
using September.Common;
using September.InGame.UI;
using UnityEngine;
using UnityEngine.Playables;

namespace InGame.Player
{
    /// <summary>スキャンに関する処理を持つクラス</summary>
    public class TakamuraScanner : NetworkBehaviour, IAfterTick
    {
        PlayerManager _playerManager;
        TakamuraMovement _movement;
        CameraController _cameraController;
        Camera _camera;
        NetworkButtons _preInput;
        AnimationClipPlayer _animationClipPlayer;
        bool _scanAnimationPlaying;
        AnimationClip _currentScanClip;
        bool _spawned;
        float _localScanStartTime = float.MinValue;
        float _interruptedScanStartTime = float.MinValue;
        PlayableGraph _scanAnimationGraph;
        LayerInfo.LayerType _scanAnimationLayer;
        CancellationTokenSource _scanReturnBlendCts;

        [Header("スキャンアニメーション")]
        [SerializeField] AnimationClip _scanAnimationClip;
        [SerializeField, Min(0f), Tooltip("スキャン終了時に通常モーションへ戻るブレンド時間（秒）")]
        float _scanReturnBlendDuration = 0.25f;

        [Networked, OnChangedRender(nameof(OnMimicTargetChanged))]
        NetworkId MimicTargetId { get; set; }

        // 一度きりのRPCではなく状態を保持し、途中参加・描画準備の遅れにも対応する。
        [Networked] bool ScanAnimationActive { get; set; }
        [Networked] float ScanAnimationStartTime { get; set; }

        [Header("カメラ制御")]
        [SerializeField, Tooltip("フォーカス時のカメラの位置")]
        Vector3 _focusPosition = new(0.5f, 1f, -2f);

        [SerializeField, Tooltip("カメラの移動時間")]
        float _cameraMoveDuration = 0.2f;

        [Header("カメラスキャンの有効領域")]
        [SerializeField, Tooltip("擬態対象の候補にできる最大距離")]
        float _scannableMaxDistance = 10f;

        [SerializeField, Tooltip("コライダーの対象レイヤー")]
        LayerMask _targetLayer;

        [SerializeField, Tooltip("演出用キャンバス")]
        ScannerCanvas _scannerCanvas;

        [Header("ガワ")]
        [SerializeField]
        TakamuraVisual _visual;

        [Header("宝石UI")]
        [SerializeField] CanvasGroup _playerJewelryView;
        [SerializeField] GameObject _nameText;
        [SerializeField] GameObject _nameBack;

        PlayerEquipmentManager _playerEquipmentManager;
        PlayerAbilityManager _playerAbilityManager;

        TakamuraScanTarget[] _scanTargets = Array.Empty<TakamuraScanTarget>();
        readonly Dictionary<NetworkId, TakamuraScanTarget> _targetByNetworkId = new();
        int _focusIndex = -1;

        StateChangeType _pendingStateChange = StateChangeType.None;
        int _stateChangeTick = -1;

        enum StateChangeType
        {
            None,
            Mimic,
            Reveal
        }

        public TakamuraVisual Visual => _visual;

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, Channel = RpcChannel.Reliable)]
        public void RPC_OnCharacterMimicRestored()
        {
            if (UIController.I)
                UIController.I.ShowOutFieldUI(false);
        }

        public override void Spawned()
        {
            _playerManager = GetComponent<PlayerManager>();
            _movement = GetComponent<TakamuraMovement>();
            _playerEquipmentManager = GetComponent<PlayerEquipmentManager>();
            _playerAbilityManager = GetComponent<PlayerAbilityManager>();
            _animationClipPlayer = GetComponentInChildren<AnimationClipPlayer>(true);
            _spawned = true;
            _localScanStartTime = float.MinValue;
            _interruptedScanStartTime = float.MinValue;
            if (_animationClipPlayer)
            {
                _animationClipPlayer.BeforeEvaluate -= UpdateScanAnimation;
                _animationClipPlayer.BeforeEvaluate += UpdateScanAnimation;
            }
            _scanTargets = FindObjectsByType<TakamuraScanTarget>(FindObjectsSortMode.None);
            CreateTargetDictionary();

            if (HasInputAuthority)
            {
                _cameraController = GetComponent<CameraController>();
                _camera = Camera.main;
            }

            _scannerCanvas.gameObject.SetActive(false);
            SetExcaliburAttackEnabled(_movement.CurrentMimicryState != MimicryState.MimicExhibit);
            ChangeVisual();
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority) ApplyPendingStateChange();

            if (HasStateAuthority && _movement.CurrentMimicryState != MimicryState.Default)
                ScanAnimationActive = false;

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
            if (_movement.CurrentMimicryState == MimicryState.Default)
            {
                // フォーカスをあてる（地上にいる時のみ開始できる）
                if (_movement.IsGround && input.Buttons.WasPressed(_preInput, PlayerButtons.Ability2))
                {
                    if (HasInputAuthority) FocusStartEffective();
                    if (HasStateAuthority) FocusStartStateChange();
                }

                // フォーカス中
                if (input.Buttons.IsSet(PlayerButtons.Ability2))
                {
                    if (HasInputAuthority && _scannerCanvas.gameObject.activeSelf) FocusEffective(input);

                    // 擬態する（地上にいる時のみ実行できる）
                    if (_movement.IsGround
                        && input.Buttons.WasPressed(_preInput, PlayerButtons.Attack)
                        && HasInputAuthority)
                    {
                        if (_focusIndex == -1) return;

                        var target = _scanTargets[_focusIndex];
                        if (target == null) return;
                        var networkObject = target.GetComponentInParent<NetworkObject>();
                        if (networkObject == null) return;

                        RPC_Mimic(networkObject.Id);
                        FocusEndEffective();
                    }
                }

                // フォーカス解除（着地するまで入力の押下/離した判定が持ち越されてしまうため、
                // 空中にいる間も判定できるようにする）
                if (input.Buttons.WasReleased(_preInput, PlayerButtons.Ability2))
                {
                    if (HasInputAuthority)
                    {
                        FocusEndEffective();
                    }
                    if (HasStateAuthority) FocusEndStateChange();
                }
            }
            else if (_movement.CurrentMimicryState == MimicryState.MimicExhibit)
            {
                // 展示物の操作に使うAttack入力では擬態を解除しない。
                if (CanReveal()
                    && input.Buttons.WasPressed(_preInput, PlayerButtons.Attack)
                    && HasInputAuthority)
                {
                    RPC_Reveal();
                }
            }
        }

        /// <summary>
        /// 全端末で擬態するためのメソッド
        /// </summary>
        /// <param name="targetId">擬態対象のNetworkId</param>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        void RPC_Mimic(NetworkId targetId)
        {
            if (!_targetByNetworkId.TryGetValue(targetId, out var target)) return;
            var interactable = target.GetComponentInParent<InteractableBase>();
            if (!interactable || _movement.CurrentMimicryState != MimicryState.Default) return;

            transform.position += Vector3.up;
            MimicTargetId = targetId;
            _movement.CurrentExhibitType = interactable.ExhibitType;
            ReserveStateChange(StateChangeType.Mimic);
        }

        /// <summary>
        /// 全端末で擬態解除するためのメソッド
        /// </summary>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        void RPC_Reveal()
        {
            ReserveReveal();
        }

        /// <summary>
        /// 擬態解除メソッド
        /// </summary>
        void ReserveReveal()
        {
            if (!CanReveal()) return;
            ReserveStateChange(StateChangeType.Reveal);
        }

        bool CanReveal()
        {
            return _playerManager && _movement
                && _playerManager.CurrentPlayerControlState == PlayerManager.PlayerControlState.Normal
                && _movement.CurrentMimicryState == MimicryState.MimicExhibit
                && _movement.IsGround;
        }

        /// <summary>
        /// 状態変更の予約メソッド
        /// </summary>
        /// <param name="stateChange"></param>
        void ReserveStateChange(StateChangeType stateChange)
        {
            _pendingStateChange = stateChange;
            _stateChangeTick = Runner.Tick + 1;
        }

        /// <summary>
        /// 状態変更を適用するメソッド
        /// </summary>
        void ApplyPendingStateChange()
        {
            if (_pendingStateChange == StateChangeType.None || Runner.Tick < _stateChangeTick) return;

            switch (_pendingStateChange)
            {
                case StateChangeType.Mimic:
                    _movement.CurrentMimicryState = MimicryState.MimicExhibit;
                    // 擬態解除用のAttack入力でエクスカリバー攻撃が同時発動しないようにする。
                    SetExcaliburAttackEnabled(false);
                    FocusEndStateChange();
                    break;
                case StateChangeType.Reveal:
                    // 予約後に搭乗・展示物操作が始まった場合も解除を破棄する。
                    if (!CanReveal()) break;
                    transform.position += Vector3.up;
                    MimicTargetId = default;
                    _movement.CurrentMimicryState = MimicryState.Default;
                    // 解除入力を処理した次のTick以降から、エクスカリバー攻撃を再び使用可能にする。
                    SetExcaliburAttackEnabled(true);
                    break;
            }

            _pendingStateChange = StateChangeType.None;
            _stateChangeTick = -1;
        }

        /// <summary>展示物への擬態中だけエクスカリバー攻撃を無効化する。</summary>
        void SetExcaliburAttackEnabled(bool enabled)
        {
            _playerAbilityManager?.SetAbilityEnabled(enabled, nameof(AbilityExcaliburAttack));
        }

        /// <summary>
        /// フォーカスを開始した時の演出メソッド
        /// </summary>
        void FocusStartEffective()
        {
            _playerEquipmentManager?.RPC_SetCurrentEquipmentVisible(false);
            _scannerCanvas.gameObject.SetActive(true);
            _scannerCanvas.ChangeImageVisibility(false);
            _cameraController.ChangeOffset(_focusPosition, _cameraMoveDuration);
        }

        /// <summary>
        /// フォーカス中の演出メソッド
        /// </summary>
        /// <param name="input">プレイヤーが向く方向</param>
        void FocusEffective(PlayerInput input)
        {
            _movement.SetRotationDirection(input.DesiredLookDirection);
            UpdateNearestExhibit();
            FocusExhibit();
        }

        /// <summary>
        /// フォーカス終了時の演出メソッド
        /// </summary>
        void FocusEndEffective()
        {
            _playerEquipmentManager?.RPC_SetCurrentEquipmentVisible(true);
            _cameraController.ResetOffset(_cameraMoveDuration);
            _scannerCanvas.ChangeImageVisibility(false);
            _scannerCanvas.gameObject.SetActive(false);
            _focusIndex = -1;
        }

        // 全端末で、同期状態とPlayableの準備状態を描画直前に確認する。
        void UpdateScanAnimation()
        {
            if (!_spawned || !Object || !Object.IsValid || !Runner) return;
            if (!ScanAnimationActive || _movement.CurrentMimicryState != MimicryState.Default)
            {
                StopScanAnimationLocal();
                return;
            }

            if (!_animationClipPlayer || !_animationClipPlayer.isActiveAndEnabled
                || !_animationClipPlayer.IsValid || !_scanAnimationClip) return;

            // モデルの再有効化などでグラフが作り直された場合は、現在の同期状態から再生を復元する。
            if (!_scanAnimationGraph.Equals(_animationClipPlayer.Graph))
            {
                StopScanAnimationLocal(immediate: true);
                _scanAnimationGraph = _animationClipPlayer.Graph;
                _interruptedScanStartTime = float.MinValue;
            }

            if (_scanAnimationPlaying && _localScanStartTime != ScanAnimationStartTime)
                StopScanAnimationLocal(immediate: true);

            if (_scanAnimationPlaying
                && !_animationClipPlayer.IsCurrentClipOnLayer(_scanAnimationLayer, _currentScanClip))
            {
                // 攻撃などの明示的な割り込みを、次の描画で上書きしない。
                _interruptedScanStartTime = ScanAnimationStartTime;
                StopScanAnimationLocal();
                return;
            }

            if (!_scanAnimationPlaying)
            {
                if (_interruptedScanStartTime == ScanAnimationStartTime) return;
                StartScanAnimationLocal();
            }
            if (!_scanAnimationPlaying
                || !_animationClipPlayer.TryGetPlayableInfo(_scanAnimationClip, out var info)) return;

            // RPC到着時刻や端末のフレームレートではなく、同期された開始時刻を基準にする。
            var renderTime = HasInputAuthority || HasStateAuthority
                ? Runner.LocalRenderTime
                : Runner.RemoteRenderTime;
            var elapsed = Mathf.Max(0f, (float)renderTime - ScanAnimationStartTime);
            var clipTime = Mathf.Min(elapsed * Mathf.Max(0f, info.montage.PlaySpeed),
                _scanAnimationClip.length);
            info.SetTime(clipTime, updateBlendWeight: false);
        }

        void StartScanAnimationLocal()
        {

            var montages = AnimationClipsContainer.Instance?.AnimationMontages;
            var index = montages == null ? -1 : Array.FindIndex(montages,
                montage => montage.AnimClip == _scanAnimationClip);
            if (index < 0 || montages[index].TargetLayer == LayerInfo.LayerType.Base)
            {
                // アセットの読み込みが完了するまで次の描画で再試行する。
                return;
            }

            CancelScanReturnBlend();
            _scanAnimationLayer = montages[index].TargetLayer;
            _animationClipPlayer.PlayOnLayer(_scanAnimationClip, _scanAnimationLayer, speed: 0f);
            if (!_animationClipPlayer.IsCurrentClipOnLayer(_scanAnimationLayer, _scanAnimationClip)) return;
            _currentScanClip = _scanAnimationClip;
            _scanAnimationPlaying = true;
            _localScanStartTime = ScanAnimationStartTime;
        }

        void StopScanAnimationLocal(bool immediate = false)
        {
            // ボタンを離す通知が重複しても、進行中の戻りブレンドは続ける。
            if (!_scanAnimationPlaying && !immediate) return;
            CancelScanReturnBlend();
            _scanAnimationPlaying = false;

            if (_animationClipPlayer
                && _animationClipPlayer.IsCurrentClipOnLayer(_scanAnimationLayer, _currentScanClip))
            {
                if (!immediate && _scanReturnBlendDuration > 0f)
                {
                    _scanReturnBlendCts = new CancellationTokenSource();
                    BlendBackToNormalAsync(_currentScanClip, _scanAnimationLayer,
                        _scanReturnBlendCts.Token).Forget();
                    return;
                }
                _animationClipPlayer.PlayOnLayer(null, _scanAnimationLayer);
            }
            _currentScanClip = null;
        }

        async UniTask BlendBackToNormalAsync(AnimationClip clip, LayerInfo.LayerType layer,
            CancellationToken token)
        {
            var blend = new LayerInfo.Blend
            {
                BlendTime = _scanReturnBlendDuration,
                BlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)
            };
            await _animationClipPlayer.BlendLayerWeight(layer, 0f, blend, token);

            // 再スキャンや別モーションへの切り替え後に古い終了処理を適用しない。
            if (token.IsCancellationRequested || !_animationClipPlayer) return;
            if (_animationClipPlayer.IsCurrentClipOnLayer(layer, clip))
                _animationClipPlayer.PlayOnLayer(null, layer);
            _currentScanClip = null;
            CancelScanReturnBlend();
        }

        void CancelScanReturnBlend()
        {
            var cancellation = _scanReturnBlendCts;
            _scanReturnBlendCts = null;
            cancellation?.Cancel();
            cancellation?.Dispose();
        }

        void OnDisable()
        {
            if (_animationClipPlayer)
                _animationClipPlayer.BeforeEvaluate -= UpdateScanAnimation;
            StopScanAnimationLocal(immediate: true);
        }

        void OnEnable()
        {
            if (_spawned && _animationClipPlayer)
            {
                _animationClipPlayer.BeforeEvaluate -= UpdateScanAnimation;
                _animationClipPlayer.BeforeEvaluate += UpdateScanAnimation;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _spawned = false;
            if (_animationClipPlayer)
                _animationClipPlayer.BeforeEvaluate -= UpdateScanAnimation;
            StopScanAnimationLocal(immediate: true);
        }

        /// <summary>
        /// フォーカス開始時の状態変更メソッド
        /// </summary>
        void FocusStartStateChange()
        {
            if (!ScanAnimationActive)
                ScanAnimationStartTime = Runner.SimulationTime;
            ScanAnimationActive = true;
            _playerManager.SetControlState(PlayerManager.PlayerControlState.InputLocked);
            _movement.CurrentAbilityPhase = ScanAbilityPhase.Scanning;
        }

        /// <summary>
        /// フォーカス終了時の状態変更メソッド
        /// </summary>
        void FocusEndStateChange()
        {
            ScanAnimationActive = false;
            _playerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
            _movement.CurrentAbilityPhase = ScanAbilityPhase.Default;
        }

        /// <summary>
        /// 条件に合った擬態対象を計算して取得するメソッド
        /// </summary>
        void UpdateNearestExhibit()
        {
            var moreCenter = float.MaxValue;
            _focusIndex = -1;

            foreach (var interactable in _scanTargets)
            {
                if (!interactable || !interactable.gameObject.activeSelf) continue;

                // 画面上の位置
                var scanPos = interactable.ScanPos;
                var position = scanPos ? scanPos.position : interactable.transform.position;
                var viewportPoint = _camera.WorldToViewportPoint(position);

                // 画面内にいなければスキップ
                if (viewportPoint.x < 0f || viewportPoint.x > 1f
                    || viewportPoint.y < 0f || viewportPoint.y > 1f
                    || viewportPoint.z < 0f)
                    continue;

                // 判定距離内かどうか
                var distance = Vector3.SqrMagnitude(position - transform.position);
                if (distance > _scannableMaxDistance * _scannableMaxDistance) continue;

                // より画面の中心にいるかどうか
                var center = (0.5f - viewportPoint.x) * (0.5f - viewportPoint.x)
                             + (0.5f - viewportPoint.y) * (0.5f - viewportPoint.y);
                if (center >= moreCenter) continue;

                // 壁越しかどうか判定
                var rayOrigin = _camera.transform.position;
                var rayDirection = position - rayOrigin;
                var rayDistance = rayDirection.magnitude;
                var hasHit = Physics.Raycast(
                    rayOrigin,
                    rayDirection.normalized,
                    out var hit,
                    rayDistance,
                    _targetLayer,
                    QueryTriggerInteraction.Collide);

                if (!hasHit)
                {
                    Debug.DrawLine(rayOrigin, position, Color.yellow);
                }
                else if (!IsHitScanTarget(hit.collider, interactable))
                {
                    Debug.DrawLine(rayOrigin, hit.point, Color.red);
                }
                else
                {
                    // 条件に合致した
                    Debug.DrawLine(rayOrigin, hit.point, Color.green);
                    moreCenter = center;
                    _focusIndex = Array.IndexOf(_scanTargets, interactable);
                }
            }
        }

        /// <summary>
        /// 壁越し判定メソッド
        /// </summary>
        /// <param name="hitCollider">Rayが当たったオブジェクト</param>
        /// <param name="target">想定している擬態対象</param>
        /// <returns>壁が間にないか</returns>
        bool IsHitScanTarget(Collider hitCollider, TakamuraScanTarget target)
        {
            if (!hitCollider || !target) return false;

            var hitNetworkObject = hitCollider.GetComponent<InteractableBase>();
            if (!hitNetworkObject)
                hitNetworkObject = hitCollider.GetComponentInParent<InteractableBase>();

            var targetNetworkObject = target.GetComponent<InteractableBase>();
            if (!targetNetworkObject)
                targetNetworkObject = target.GetComponentInParent<InteractableBase>();

            return hitNetworkObject && targetNetworkObject && hitNetworkObject == targetNetworkObject;
        }

        /// <summary>
        /// 擬態対象にフォーカスを合わせる演出メソッド
        /// </summary>
        void FocusExhibit()
        {
            var scanned = _focusIndex != -1;
            _scannerCanvas.ChangeImageVisibility(scanned);
            if (!scanned) return;

            var target = _scanTargets[_focusIndex];
            if (!target) return;

            var pivot = target.ScanPos;
            var position = _camera.WorldToScreenPoint(pivot ? pivot.position : target.transform.position);
            _scannerCanvas.SetImageOverExhibit(position);
        }

        /// <summary>
        /// 擬態対象のNetworkIdをキャッシュするメソッド
        /// </summary>
        void CreateTargetDictionary()
        {
            _targetByNetworkId.Clear();

            foreach (var target in _scanTargets)
            {
                if (!target) continue;

                var networkObject = target.GetComponentInParent<NetworkObject>();
                if (!networkObject || _targetByNetworkId.ContainsKey(networkObject.Id)) continue;
                _targetByNetworkId.Add(networkObject.Id, target);
            }
        }

        /// <summary>
        /// 擬態対象のIdが変わった時に呼ばれるメソッド
        /// </summary>
        void OnMimicTargetChanged()
        {
            ChangeVisual();
        }

        /// <summary>
        /// ガワを変更するメソッド
        /// </summary>
        void ChangeVisual()
        {
            if (MimicTargetId == default)
            {
                // 擬態解除後は、所持しているエクスカリバーなどの装備を再表示する。
                _playerEquipmentManager?.SetCurrentEquipmentVisible(true);
                _visual?.Reveal();
                // キャンバスを表示
                if (_playerJewelryView) _playerJewelryView.alpha = 1;
                _nameText?.SetActive(true);
                _nameBack?.SetActive(true);
                return;
            }

            // 対象の描画準備が遅れていても、同期上擬態中なら装備は先に隠しておく。
            _playerEquipmentManager?.SetCurrentEquipmentVisible(false);

            if (_targetByNetworkId.TryGetValue(MimicTargetId, out var target) && target)
            {
                _visual?.Mimic(target);
                // キャンバスを非表示
                if (_playerJewelryView) _playerJewelryView.alpha = 0;
                _nameText?.SetActive(false);
                _nameBack?.SetActive(false);
            }
        }
    }
}
