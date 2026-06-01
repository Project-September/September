using September.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace September.Lobby
{
    public class BuildView : BuildViewBase
    {
        [Header("表示に必要なUI群")]
        [SerializeField] BuildSelectObject[] _buildObjects;
        [SerializeField] Button _decisionButton;
        [SerializeField] TextMeshProUGUI _buildName;
        [SerializeField] TextMeshProUGUI _buildInfo;
        int _currentSelectIndex;

        public override void Init(BuildDataBase[] builds)
        {
            // 仮UI処理
            if (_buildObjects != null && builds != null)
            {
                // UIへの範囲外アクセスを防止したfor文
                for (int i = 0; i < Mathf.Min(_buildObjects.Length, builds.Length); i++)
                {
                    var obj = _buildObjects[i];
                    if (obj == null) continue;
                    obj.Init();
                    // ボタンにインデックスを直接選択できるように
                    var index = i;
                    obj.RegisterAction(() => MoveIndexForButton(index));
                    // 最初のUI以外を見えなくする
                    if (i == 0)
                        obj.Select();
                    else
                        obj.Unselect();
                }
            }

            if (_buildName && _buildInfo)
            {
                _buildName.text = builds[0].BuildName;
                _buildInfo.text = builds[0].BuildInfo;
            }
        }

        public override void VisualizeBuildInfo(int index, BuildDataBase build)
        {
            // 受け取ったBuildDataBaseは
            // アイコンだけ表示していて
            // 選択中のビルドルートに関しては詳細を表示する場合とかに使えるのでは

            // 直前に表示していたものを見えなくする
            _buildObjects[_currentSelectIndex].Unselect();

            // 選択要素を見えるようにする
            _buildObjects[index].Select();

            // 選択中のビルドルート詳細を表示
            if (_buildName && _buildInfo)
            {
                _buildName.text = build.BuildName;
                _buildInfo.text = build.BuildInfo;
            }

            // 直前のインデックスを保存
            _currentSelectIndex = index;
        }

        public override void VisualizeSelection(int index)
        {
            // 仮の決定描画
            var colors = _decisionButton.colors;
            colors.normalColor = Color.red;
            colors.selectedColor = Color.red;
            colors.highlightedColor = Color.red;
            _decisionButton.colors = colors;

            // 現在選択中のものの描画処理
            _buildObjects[_currentSelectIndex]?.Decision();

            // 直前に選択していたものの描画処理
            if (index < 0) return;
            _buildObjects[index]?.Cancel();
        }
    }
}
