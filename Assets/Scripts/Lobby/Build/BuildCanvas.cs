using UnityEngine;

namespace September.Lobby
{
    /// <summary>ロビーのビルド決定UIを管理するクラス</summary>
    public class BuildCanvas : MonoBehaviour
    {
        [SerializeField] BuildViewBase _buildView;
        [SerializeField] BuildFactory _buildFactory;
        [Header("自分で初期化をするかどうか")]
        [SerializeField] bool _selfInitialization;
        BuildPresenter _presenter;

        private void Awake()
        {
            if (_selfInitialization) Init();
        }

        [ContextMenu("Init")]
        public void Init()
        {
            // ビルドのUIを動くようにする
            _presenter = _buildFactory?.CreateBuild(_buildView);
        }

        private void OnDisable()
        {
            _presenter?.Dispose();
        }
    }
}
