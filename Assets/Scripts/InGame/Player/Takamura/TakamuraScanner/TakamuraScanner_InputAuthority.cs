using Cinemachine;
using InGame.Interact;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField, Tooltip("カメラから見た領域の中心の位置")] Vector3 _centerOffset = new Vector3(0, 0, 5);
        [SerializeField, Tooltip("スキャン範囲の半分の長さ")] Vector3 _halfExtents = new Vector3(5, 2.5f, 2.5f);
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
            _cameraController.ResetOffset(_cameraMoveDuration);
        }

        /// <summary>
        /// より近い展示物を計算して取得するメソッド
        /// </summary>
        void UpdateNearestExhibit()
        {
            var count = Physics.OverlapBoxNonAlloc(_virtualCamera.transform.TransformPoint(_centerOffset), _halfExtents, _scanedColliders, _virtualCamera.transform.rotation, _exhibitLayer);
            var minDistance = float.MaxValue;
            _currentScanedObject = null;
            for (int i = 0; i < count; i++)
            {
                if (_scanedColliders[i] == null) continue;

                // より近いオブジェクトをスキャン対象にする
                // TODO : 一定の視野角内にいるものだけをスキャン対象にする
                var distance = Vector3.SqrMagnitude(_scanedColliders[i].transform.position - transform.position);
                if (_currentScanedObject == null || minDistance > distance)
                {
                    _currentScanedObject = _scanedColliders[i];
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
                var pos = _camera.WorldToScreenPoint(exhibit.transform.position);

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
            var pos = _virtualCamera == null ? _focusPosition + _centerOffset : _virtualCamera.transform.TransformPoint(_centerOffset);
            var rot = _virtualCamera == null ? Quaternion.identity : _virtualCamera.transform.rotation;

            Gizmos.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, _halfExtents * 2);
        }
    }
}
