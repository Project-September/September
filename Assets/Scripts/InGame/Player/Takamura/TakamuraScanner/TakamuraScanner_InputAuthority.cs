using InGame.Interact;
using UnityEngine;

namespace InGame.Player
{
    // 入力についての処理をまとめたファイル
    public partial class TakamuraScanner
    {
        [Header("カメラスキャンの有効領域についてのパラメータ")]
        [SerializeField, Tooltip("カメラから見た領域の中心の位置")] Vector3 _centerOffset = new Vector3(0, 0, 5);
        [SerializeField, Tooltip("スキャン範囲の半分の長さ")] Vector3 _halfExtents = new Vector3(5, 2.5f, 2.5f);
        [SerializeField, Tooltip("スキャン対象のレイヤー")] LayerMask _exhibitLayer;
        Collider[] _scanedColliders;
        InteractableBase _currentScanedObject;

        /// <summary>
        /// 入力権限がある場合の初期化メソッド
        /// </summary>
        void InitInputAuthority()
        {
            _cameraController = GetComponent<CameraController>();
            _camera = Camera.main;
            _scanedColliders = new Collider[8]; // 仮で8個まで取れるようにする
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
            _tkmrMovement.SetRotationDirection(_camera.transform.forward);

            UpdateNearestExhibit();
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
            var count = Physics.OverlapBoxNonAlloc(_camera.transform.position + _centerOffset, _halfExtents, _scanedColliders, _camera.transform.rotation, _exhibitLayer);
            var minDistance = float.MaxValue;
            _currentScanedObject = null;
            for (int i = 0; i < count; i++)
            {
                if (_scanedColliders[i] == null) continue;

                // より近いオブジェクトをスキャン対象にする
                var distance = Vector3.SqrMagnitude(_scanedColliders[i].transform.position - transform.position);
                if (_currentScanedObject == null || minDistance > distance)
                {
                    _currentScanedObject = _scanedColliders[i].GetComponent<InteractableBase>();
                }
            }
        }

        /// <summary>
        /// スキャン領域を描画するメソッド
        /// </summary>
        void DrawScanArea()
        {
            Gizmos.color = Color.green;
            var pos = _camera == null ? _focusPosition : _camera.transform.position;
            var rot = _camera == null ? Quaternion.identity : _camera.transform.rotation;

            Gizmos.matrix = Matrix4x4.TRS(pos + _centerOffset, rot, Vector3.one);
            Gizmos.DrawWireCube(pos + _centerOffset, _halfExtents);
        }
    }
}
