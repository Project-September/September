using UnityEngine;

namespace September.Lobby
{
    public class BuildPresenter
    {
        BuildDatasRuntime _runtime;
        IBuildView _view;

        public BuildPresenter(BuildDatas data, IBuildView view)
        {
            _runtime = new(data);
            _view = view;
            if (_runtime == null) throw new System.NullReferenceException(nameof(_runtime));
            if (_view == null) throw new System.NullReferenceException(nameof(_view));
            _view.VisulaizeBuildInfo(_runtime.CurrentBuildData);
        }

        /// <summary>
        /// 選択中を切り替えるメソッド
        /// </summary>
        /// <param name="move">選択を切り替える方向</param>
        public void MoveIndex(BuildIndexMoveType move)
        {
            _runtime.MoveIndex(move);
            _view.VisulaizeBuildInfo(_runtime.CurrentBuildData);
        }

        /// <summary>
        /// ビルドを選択するメソッド
        /// </summary>
        public void SelectBuild()
        {
            var firstSelect = _runtime.SelectBuild();
            if (firstSelect)
            {
                //サーバーにデータを送る？？
                //var build = _runtime.CurrentBuildData;
            }
            _view.VisualizeSelection(firstSelect);
        }
    }
}
