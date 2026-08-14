using Unity.Cinemachine;
using InGame.Interact;
using UnityEngine;
using Result;
using September.Common;
using System;

namespace InGame.Player
{
    // 操作主の画面でだけ表示したい処理をまとめたファイル
    public partial class TakamuraScanner
    {
        [Header("カメラ制御")]
        [SerializeField, Tooltip("CinemaChineのカメラオブジェクト")] CinemachineVirtualCamera _virtualCamera;
        [SerializeField, Tooltip("フォーカス時のカメラの位置")] Vector3 _focusPosition = new(0.5f, 1, -2);
        [SerializeField, Tooltip("カメラの移動時間")] float _cameraMoveDuration = 0.2f;
        [Header("カメラスキャンの有効領域についてのパラメータ")]
        [SerializeField, Tooltip("擬態対象の候補にできる最大距離")] float _scannableMaxDistance = 10f;
        [SerializeField, Tooltip("演出用キャンバス")] ScannerCanvas _scannerCanvas;
        [Header("ガワ")]
        [SerializeField] TakamuraVisual _visual;
        /// <summary>シーン上にある展示物の配列</summary>
        TakamuraScanTarget[] _interactables;
        readonly UniqueID _irregularId = new UniqueID(ExhibitType.None, 255);

        public TakamuraVisual Visual => _visual;

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
            _cameraController.ResetOffset(_cameraMoveDuration);
            _scannerCanvas.ChangeImageVisibility(false);
            Index = -1;
        }

        /// <summary>
        /// より近い展示物を計算して取得するメソッド
        /// </summary>
        void UpdateNearestExhibit()
        {
            var minDistance = float.MaxValue;
            Index = -1;
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
                            Index = Array.IndexOf(_interactables, interactable);
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
            var scanned = Index != -1;
            _scannerCanvas.ChangeImageVisibility(scanned);

            // 展示物かどうかの最終確認ができたら描画処理
            if (scanned)
            {
                // 展示物の座標をスクリーン座標に変換
                var target = _interactables[Index];
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

        #region Gizmos
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
        #endregion
    }
}
