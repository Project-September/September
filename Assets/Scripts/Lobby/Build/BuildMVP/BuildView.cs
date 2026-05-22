using Fusion;
using September.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace September.Lobby
{
    public class BuildView : BuildViewBase
    {
        [SerializeField] Text _buildName;
        [SerializeField] Text _buildInfo;
        [SerializeField] Button _button;
        BuildPresenter _presenter;
        NetworkRunner _networkRunner;

        public override void SelectBuild()
        {
            //自分自身の確保
            if (!_networkRunner) _networkRunner = NetworkRunner.GetRunnerForScene(SceneManager.GetActiveScene());
            if (_networkRunner == null || PlayerDatabase.Instance == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("サーバー接続に失敗しました");
#endif
                return;
            }

            //操作主が見つかったら決定処理
            base.SelectBuild();
        }

        public override void VisualizeBuildInfo(int index)
        {
            var build = _build.Builds[index];
            _buildName.text = build.BuildName;
            _buildInfo.text = build.BuildInfo;
#if UNITY_EDITOR
            Debug.Log($"選択中 => {build.BuildName}\n説明 => {build.BuildInfo}");
#endif
        }

        public override void VisualizeSelection(bool selected, int index)
        {
            if (!selected)
            {
                //仮の決定描画
                var colors = _button.colors;
                colors.normalColor = Color.red;
                colors.selectedColor = Color.red;
                colors.highlightedColor = Color.red;
                _button.colors = colors;
                //NetworkRunnerとPlayerDatabaseはnullじゃない前提
                var player = _networkRunner.LocalPlayer;
                PlayerDatabase.Instance.Rpc_SetBuild(player, _build.Builds[index].BuildType);
            }
            base.VisualizeSelection(selected, index);
        }

        protected override void Init()
        {
            //仮でインスタンス生成
            _presenter = new(_build, this);
            VisualizeBuildInfo(0);
        }

        private void OnDisable()
        {
            _presenter?.Dispose();
        }
    }
}
