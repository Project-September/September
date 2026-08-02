using Unity.Cinemachine;
using InGame.Interact;
using UnityEngine;

namespace InGame.Player
{
    // 入力についての処理をまとめたファイル
    public partial class TakamuraScanner
    {
        [Header("カメラ制御")]
        [SerializeField, Tooltip("CinemaChineのカメラオブジェクト")] CinemachineVirtualCamera _virtualCamera;
        [SerializeField, Tooltip("フォーカス時のカメラの位置")] Vector3 _focusPosition = new(0.5f, 1, -2);
        [SerializeField, Tooltip("カメラの移動時間")] float _cameraMoveDuration = 0.2f;
        [Header("カメラスキャンの有効領域についてのパラメータ")]
        [SerializeField, Tooltip("擬態対象の候補にできる最大距離")] float _scannableMaxDistance = 10f;
        [SerializeField, Tooltip("演出用キャンバス")] ScannerCanvas _scannerCanvas;
        /// <summary>シーン上にある展示物の配列</summary>
        InteractableBase[] _interactables;
        /// <summary>現在擬態対象としているオブジェクト</summary>
        InteractableBase _currentScanedInteractable;

        /// <summary>
        /// 入力権限がある場合の初期化メソッド
        /// </summary>
        void InitInputAuthority()
        {
            _cameraController = GetComponent<CameraController>();
            _camera = Camera.main;
            _interactables = FindObjectsByType<InteractableBase>(FindObjectsSortMode.None);
            _scannerCanvas.gameObject.SetActive(true);
        }

        /// <summary>
        /// フォーカスを当て始めた時の演出メソッド
        /// </summary>
        void FocusStartEffective()
        {
            _cameraController.ChangeOffset(_focusPosition, _cameraMoveDuration);
        }

        /// <summary>
        /// フォーカス中の演出メソッド
        /// </summary>
        void FocusEffective()
        {
            _tkmrMovement.SetRotationDirection(_virtualCamera.transform.forward);

            UpdateNearestExhibit();
            FocusExhibit();
        }

        /// <summary>
        /// フォーカスを解除した時の演出メソッド
        /// </summary>
        void FocusEndEffective()
        {
            _currentScanedInteractable = null;
            _cameraController.ResetOffset(_cameraMoveDuration);
        }

        /// <summary>
        /// より近い展示物を計算して取得するメソッド
        /// </summary>
        void UpdateNearestExhibit()
        {
            var minDistance = float.MaxValue;
            _currentScanedInteractable = null;
            foreach (var interactable in _interactables)
            {
                if (interactable == null) continue;
                if (!interactable.gameObject.activeSelf) continue;

                // 展示物の座標を取得
                Vector3 pos = interactable.TryGetComponent<Collider>(out var col)
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
                            _currentScanedInteractable = interactable;
                            minDistance = distance;
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
            // 展示物かどうかの最終確認ができたら描画処理
            if (_currentScanedInteractable != null)
            {
                // 展示物の座標をスクリーン座標に変換
                var pos = _camera.WorldToScreenPoint(
                    _currentScanedInteractable.TryGetComponent<Collider>(out var col)
                    ? col.bounds.center
                    : _currentScanedInteractable.transform.position);

                // 擬態対象であることを示すImageを展示物の位置へ移動
                _scannerCanvas.SetImagePosition(pos);
            }
        }

        /// <summary>
        /// スキャン領域を描画するメソッド
        /// </summary>
        void DrawScanArea()
        {
            if (_camera == null) return;

            Gizmos.color = Color.green;
            // カメラの描画範囲の四隅かつ最大スキャン距離
            Vector3 bl = _camera.ViewportToWorldPoint(new Vector3(0, 0, _scannableMaxDistance));    // 左下
            Vector3 br = _camera.ViewportToWorldPoint(new Vector3(1, 0, _scannableMaxDistance));    // 右下
            Vector3 tr = _camera.ViewportToWorldPoint(new Vector3(1, 1, _scannableMaxDistance));    // 右上
            Vector3 tl = _camera.ViewportToWorldPoint(new Vector3(0, 1, _scannableMaxDistance));    // 左上

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
    }
}
