using System;
using UnityEngine;
using UnityEngine.UI;

namespace September.InGame.UI
{
    /// <summary>
    /// インタラクトUIの管理クラス
    /// </summary>
    public class InteractUi : MonoBehaviour
    {
        enum ConnectionState
        {
            Local,
            Remote,
        }
        
        [SerializeField] private Image _interactFillImage; // インタラクトの進行状況を示すUIのイメージ
        [SerializeField] private RectTransform _root;
        [SerializeField] private RectTransform _rootParentRectTransform;
        private Camera _camera;
        private readonly ConnectionState _connectionState = ConnectionState.Remote;
        private GameObject _targetObject;

        private void Awake()
        {
            if (_connectionState == ConnectionState.Local)
            {
                // ローカル接続の初期化処理
            }
            else
            {
                _camera = Camera.main;
            }
        }

        private void Start()
        {
            var canvas = _root.GetComponentInParent<Canvas>();
            var container = (_root.parent as RectTransform);
            Debug.Log($"mode={canvas.renderMode}, uiCam={(canvas.worldCamera?canvas.worldCamera.name:"null")}, parentPivot={container.pivot}, parentSize={container.rect.size}");
        }

        public void SetActive(bool isShow, GameObject target = null)
        {
            if (target)
            {
                _targetObject = target;
            }
            // インタラクトUIの表示/非表示を切り替えるメソッド
            if (_root)
            {
                _root.gameObject.SetActive(isShow);
            }
            
        }
        
        public void SetInteractProgress(float progress)
        {
            // インタラクトの進行状況を更新するメソッド
            // progressは0から1の範囲で、0が未開始、1が完了を示す
            if (_interactFillImage)
            {
                _interactFillImage.fillAmount = Mathf.Clamp01(progress);
            }
        }

        void LateUpdate()
        {
            if (!_root || !_targetObject) return;

            var container = _root.parent as RectTransform;   // = InteractUI
            if (!container) return;

            var worldCam = Camera.main; // ← ここ超重要：実カメラ
            if (!worldCam) return;

            // ★ 頭上に出したいなら、ここでオフセット or バウンズ上端に差し替え
            Vector3 world = _targetObject.transform.position; // or GetHeadTop(_target)

            // World -> Screen
            Vector3 sp = worldCam.WorldToScreenPoint(world);
            if (sp.z < 0f) { _root.gameObject.SetActive(false); return; } // カメラ背面なら非表示など
            if (!_root.gameObject.activeSelf) _root.gameObject.SetActive(true);

            // Screen -> Container(Local) （Overlayは cam=null）
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(container, sp, null, out var local))
            {
                _root.anchoredPosition = local; // 親pivot=0.5/子anchor=0.5なら補正不要
            }
        }
    }
}
