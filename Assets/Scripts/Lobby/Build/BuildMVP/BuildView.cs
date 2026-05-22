using Fusion;
using September.Common;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace September.Lobby
{
    public class BuildView : BuildViewBase
    {
        [SerializeField] TextMeshProUGUI[] _buildNames;
        [SerializeField] TextMeshProUGUI[] _buildInfos;
        [SerializeField] Button _button;
        [SerializeField] BuildFactory _build;
        BuildPresenter _presenter; // MVPに反した参照ではない
        NetworkRunner _networkRunner;
        int _preSelectIndex;

        private void Awake()
        {
            // 仮でインスタンス生成
            _presenter = _build.CreateBuild(this);
        }

        public override void Init(BuildDataBase[] builds)
        {
            // 仮UI処理
            if (_buildNames != null && _buildInfos != null && builds != null)
            {
                // UIへの範囲外アクセスを防止したfor文
                for (int i = 0; i < Mathf.Min(_buildNames.Length, _buildInfos.Length, builds.Length); i++)
                {
                    _buildNames[i].text = builds[i].BuildName;
                    _buildInfos[i].text = builds[i].BuildInfo;
                    if (i != 0)
                    {
                        // 最初のUI以外を見えなくする
                        _buildNames[i].color = Color.clear;
                        _buildInfos[i].color = Color.clear;
                    }
                }
            }
        }

        public override void SelectBuild()
        {
            // 自分自身の確保
            if (!_networkRunner) _networkRunner = NetworkRunner.GetRunnerForScene(SceneManager.GetActiveScene());
            if (_networkRunner == null || PlayerDatabase.Instance == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("サーバー接続に失敗しました");
#endif
                return;
            }

            // 操作主が見つかったら決定処理
            base.SelectBuild();
        }

        public override void VisualizeBuildInfo(int index, BuildDataBase build)
        {
            // 受け取ったBuildDataBaseは
            // アイコンだけ表示していて選択中のビルドルートに関しては詳細を表示する場合とかに使えるのでは

            // 直前に表示していたものを見えなくする
            _buildNames[_preSelectIndex].color = Color.clear;
            _buildInfos[_preSelectIndex].color = Color.clear;

            // 選択要素を見えるようにする
            _buildNames[index].color = Color.white;
            _buildInfos[index].color = Color.white;

            // 直前のインデックスを保存
            _preSelectIndex = index;
        }

        public override void VisualizeSelection(bool selected, int index, BuildDataBase build)
        {
            if (!selected)
            {
                // 仮の決定描画
                var colors = _button.colors;
                colors.normalColor = Color.red;
                colors.selectedColor = Color.red;
                colors.highlightedColor = Color.red;
                _button.colors = colors;
                // NetworkRunnerとPlayerDatabaseはnullじゃない前提
                var player = _networkRunner.LocalPlayer;
                //PlayerDatabase.Instance.Rpc_SetBuild(player, build.BuildType);
            }
        }

        private void OnDisable()
        {
            _presenter?.Dispose();
        }
    }
}
