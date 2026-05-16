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
                var colors = _button.colors;
                colors.normalColor = Color.red;
                colors.selectedColor = Color.red;
                _button.colors = colors;
                var player = NetworkRunner.GetRunnerForScene(SceneManager.GetActiveScene()).LocalPlayer;
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
