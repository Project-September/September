using UnityEngine;

namespace September.Lobby
{
    /// <summary>ロビーのビルド決定UIを管理するクラス</summary>
    public class BuildCanvas : MonoBehaviour
    {
        [SerializeField] BuildViewBase _buildView;
        [SerializeField] BuildFactory _buildFactory;
        BuildPresenter _presenter;

        private void Awake()
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
