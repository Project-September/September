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
        [SerializeField, Range(0, 90), Tooltip("水平方向の視野角（片側）")] float _horizontalAngle = 60;
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
            var count = Physics.OverlapBoxNonAlloc(_virtualCamera.transform.TransformPoint(_centerOffset)
                , _halfExtents
                , _scanedColliders
                , _virtualCamera.transform.rotation
                , _exhibitLayer);
            var minDistance = float.MaxValue;
            _currentScanedObject = null;
            for (int i = 0; i < count; i++)
            {
                if (_scanedColliders[i] == null) continue;

                // ターゲットとの距離を計算
                var targetDir = _scanedColliders[i].transform.position - _virtualCamera.transform.position;
                var distance = Vector3.SqrMagnitude(targetDir);

                // 視線とターゲットまでの距離が水平方向になす角を計算
                var sight = _virtualCamera.transform.forward;
                sight.y = 0;
                sight = sight.normalized;
                targetDir.y = 0;
                targetDir = targetDir.normalized;
                var angle = Vector3.Dot(sight, targetDir);

                // より近いオブジェクトをスキャン対象にする
                // 一定の視野角内にいるものだけをスキャン対象にする
                if (Mathf.Cos(_horizontalAngle * Mathf.Deg2Rad) <= angle && angle <= 1
                    && minDistance > distance)
                {
                    _currentScanedObject = _scanedColliders[i];
                    minDistance = distance;
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

            var cameraPos = _virtualCamera.transform.position;
            var halfZ = _centerOffset.z + _halfExtents.z;
            var halfY = _halfExtents.y;
            // 視野角に応じた横方向の長さ
            var halfX = Mathf.Tan(_horizontalAngle * Mathf.Deg2Rad) * halfZ;
            // 上下方向の領域開始点
            var cameraOffsetUp = _virtualCamera.transform.TransformPoint(Vector3.up * halfY);
            var cameraOffsetDown = _virtualCamera.transform.TransformPoint(-Vector3.up * halfY);
            // 最大奥行きの四隅の点
            var point1 = _virtualCamera.transform.TransformPoint(new Vector3(halfX, halfY, halfZ));
            var point2 = _virtualCamera.transform.TransformPoint(new Vector3(halfX, -halfY, halfZ));
            var point3 = _virtualCamera.transform.TransformPoint(new Vector3(-halfX, halfY, halfZ));
            var point4 = _virtualCamera.transform.TransformPoint(new Vector3(-halfX, -halfY, halfZ));
            if (halfX <= _halfExtents.x)
            {
                // 最大奥行きの四隅の点を判定領域に含まないまたはぴったりの場合の描画
                // 領域の形としては三角柱
                Gizmos.DrawLine(cameraOffsetUp, point1);
                Gizmos.DrawLine(cameraOffsetDown, point2);
                Gizmos.DrawLine(cameraOffsetUp, point3);
                Gizmos.DrawLine(cameraOffsetDown, point4);
            }
            else
            {
                // 視野角が最大奥行きの四隅の点より大きくなる場合
                // 領域の形としては五角柱
                var nearZ = Mathf.Tan((90 - _horizontalAngle) * Mathf.Deg2Rad) * _halfExtents.x;
                var nearPoint1 = _virtualCamera.transform.TransformPoint(new Vector3(_halfExtents.x, halfY, nearZ));
                var nearPoint2 = _virtualCamera.transform.TransformPoint(new Vector3(_halfExtents.x, -halfY, nearZ));
                var nearPoint3 = _virtualCamera.transform.TransformPoint(new Vector3(-_halfExtents.x, halfY, nearZ));
                var nearPoint4 = _virtualCamera.transform.TransformPoint(new Vector3(-_halfExtents.x, -halfY, nearZ));
                point1 = _virtualCamera.transform.TransformPoint(new Vector3(_halfExtents.x, halfY, halfZ));
                point2 = _virtualCamera.transform.TransformPoint(new Vector3(_halfExtents.x, -halfY, halfZ));
                point3 = _virtualCamera.transform.TransformPoint(new Vector3(-_halfExtents.x, halfY, halfZ));
                point4 = _virtualCamera.transform.TransformPoint(new Vector3(-_halfExtents.x, -halfY, halfZ));
                Gizmos.DrawLine(cameraOffsetUp, nearPoint1);
                Gizmos.DrawLine(cameraOffsetDown, nearPoint2);
                Gizmos.DrawLine(cameraOffsetUp, nearPoint3);
                Gizmos.DrawLine(cameraOffsetDown, nearPoint4);
                Gizmos.DrawLine(nearPoint1, nearPoint2);
                Gizmos.DrawLine(nearPoint2, nearPoint4);
                Gizmos.DrawLine(nearPoint4, nearPoint3);
                Gizmos.DrawLine(nearPoint3, nearPoint1);
                Gizmos.DrawLine(point1, nearPoint1);
                Gizmos.DrawLine(point2, nearPoint2);
                Gizmos.DrawLine(point3, nearPoint3);
                Gizmos.DrawLine(point4, nearPoint4);
            }
            Gizmos.DrawLine(point1, point2);
            Gizmos.DrawLine(point2, point4);
            Gizmos.DrawLine(point4, point3);
            Gizmos.DrawLine(point3, point1);
            Gizmos.DrawLine(cameraOffsetUp, cameraOffsetDown);
        }
    }
}
