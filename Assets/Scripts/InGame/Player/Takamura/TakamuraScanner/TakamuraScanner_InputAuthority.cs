using Cinemachine;
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
        [SerializeField, Tooltip("演出用キャンバス")] ScannerCanvas _scannerCanvas;
        Collider[] _scanedColliders;
        Collider _currentScanedObject;

        /// <summary>
        /// 入力権限がある場合の初期化メソッド
        /// </summary>
        void InitInputAuthority()
        {
            _cameraController = GetComponent<CameraController>();
            _camera = Camera.main;
            _scanedColliders = new Collider[8]; // 仮で8個まで取れるようにする
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
            var count = Physics.OverlapBoxNonAlloc(_camera.transform.TransformPoint(_centerOffset)
                , _halfExtents
                , _scanedColliders
                , _camera.transform.rotation
                , _exhibitLayer);
            var minDistance = float.MaxValue;
            _currentScanedObject = null;
            for (int i = 0; i < count; i++)
            {
                if (_scanedColliders[i] == null) continue;
                var objectPoint = _scanedColliders[i].bounds.center;
                var viewportPoint = _camera.WorldToViewportPoint(objectPoint);
                // カメラに写っているものだけをスキャン対象にする
                if (0 <= viewportPoint.x && viewportPoint.x <= 1
                    && 0 <= viewportPoint.y && viewportPoint.y <= 1
                    && 0 <= viewportPoint.z)
                {
                    Debug.Log(viewportPoint);
                    // ターゲットとの距離を計算
                    var distance = Vector3.SqrMagnitude(objectPoint - _camera.transform.position);
                    // より近いオブジェクトをスキャン対象にする
                    if (minDistance > distance)
                    {
                        _currentScanedObject = _scanedColliders[i];
                        minDistance = distance;
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
            if (_currentScanedObject != null
                && _currentScanedObject.TryGetComponent<InteractableBase>(out var exhibit))
            {
                // 展示物のワールド座標をスクリーン座標に変換
                var pos = _camera.WorldToScreenPoint(_currentScanedObject.bounds.center);

                // 擬態対象であることを示すImageを展示物の位置へ移動
                _scannerCanvas.SetImagePosition(pos);
            }
        }

        /// <summary>
        /// スキャン領域を描画するメソッド
        /// </summary>
        void DrawScanArea()
        {
            Gizmos.color = Color.green;
            // OverlapBoxの中心位置を計算
            var center = _virtualCamera.transform.TransformPoint(_centerOffset);

            Gizmos.matrix = Matrix4x4.TRS(center, _virtualCamera.transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, _halfExtents * 2);
        }
    }
}
