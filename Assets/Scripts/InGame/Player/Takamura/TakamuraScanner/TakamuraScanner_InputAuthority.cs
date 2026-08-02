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
        [SerializeField, Tooltip("OverlapBoxの中心の位置")] Vector3 _centerOffset = new Vector3(0, 0, 5);
        [SerializeField, Tooltip("OverlapBoxの半分の長さ")] Vector3 _halfExtents = new Vector3(5, 2.5f, 2.5f);
        [SerializeField, Tooltip("スキャン対象のレイヤー")] LayerMask _exhibitLayer;
        [SerializeField, Tooltip("同時に判定をとれる最大数")] int _maxHitDetectionCount = 8;
        [SerializeField, Tooltip("擬態対象の候補にできる最大距離")] float _scannableMaxDistance = 10f;
        [SerializeField, Tooltip("演出用キャンバス")] ScannerCanvas _scannerCanvas;
        Collider[] _scanedColliders;
        Collider _currentScanedObject;
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
            _scanedColliders = new Collider[_maxHitDetectionCount];
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
            _currentScanedObject = null;
            _cameraController.ResetOffset(_cameraMoveDuration);
        }

        /// <summary>
        /// より近い展示物を計算して取得するメソッド
        /// </summary>
        void UpdateNearestExhibit()
        {
            //var count = Physics.OverlapBoxNonAlloc(_camera.transform.TransformPoint(_centerOffset)
            //    , _halfExtents
            //    , _scanedColliders
            //    , _camera.transform.rotation
            //    , _exhibitLayer);
            //var minDistance = float.MaxValue;
            //_currentScanedObject = null;
            //for (int i = 0; i < count; i++)
            //{
            //    if (_scanedColliders[i] == null) continue;
            //    var objectPoint = _scanedColliders[i].bounds.center;
            //    var viewportPoint = _camera.WorldToViewportPoint(objectPoint);
            //    // カメラに写っているものだけをスキャン対象にする
            //    if (0 <= viewportPoint.x && viewportPoint.x <= 1
            //        && 0 <= viewportPoint.y && viewportPoint.y <= 1
            //        && 0 <= viewportPoint.z)
            //    {
            //        Debug.Log(viewportPoint);
            //        // ターゲットとの距離を計算
            //        var distance = Vector3.SqrMagnitude(objectPoint - _camera.transform.position);
            //        // より近いオブジェクトをスキャン対象にする
            //        if (minDistance > distance)
            //        {
            //            _currentScanedObject = _scanedColliders[i];
            //            minDistance = distance;
            //        }
            //    }
            //}

            var minDistance = float.MaxValue;
            foreach (var interactable in _interactables)
            {
                if (interactable == null) continue;
                if (!interactable.gameObject.activeSelf) continue;
                if (!interactable.TryGetComponent<Collider>(out var col)) continue;

                // カメラに写っているかを確認
                var pos = col.bounds.center;
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
            if (_currentScanedInteractable != null
                && _currentScanedInteractable.TryGetComponent<Collider>(out var col))
            {
                // 展示物のワールド座標をスクリーン座標に変換
                var pos = _camera.WorldToScreenPoint(col.bounds.center);

                // 擬態対象であることを示すImageを展示物の位置へ移動
                _scannerCanvas.SetImagePosition(pos);
            }
        }

        /// <summary>
        /// スキャン領域を描画するメソッド
        /// </summary>
        void DrawScanArea()
        {
            //Gizmos.color = Color.green;
            //// OverlapBoxの中心位置を計算
            //var center = _virtualCamera.transform.TransformPoint(_centerOffset);

            //Gizmos.matrix = Matrix4x4.TRS(center, _virtualCamera.transform.rotation, Vector3.one);
            //Gizmos.DrawWireCube(Vector3.zero, _halfExtents * 2);

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
