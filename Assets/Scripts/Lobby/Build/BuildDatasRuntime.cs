using UnityEngine;

namespace September.Lobby
{
    public class BuildDatasRuntime
    {
        /// <summary>ビルドの種類の配列</summary>
        BuildDataBase[] _builds;
        int _currentIndex;
        bool _selected;

        public BuildDataBase CurrentBuildData => _builds[_currentIndex] ?? throw new System.InvalidOperationException();

        public BuildDatasRuntime(BuildDatas data)
        {
            _builds = data.Builds;
        }

        /// <summary>
        /// 選択を切り替えるメソッド
        /// </summary>
        /// <param name="move">選択を切り替える方向</param>
        /// <returns>選択後の要素番号</returns>
        public void MoveIndex(BuildIndexMoveType move)
        {
            _currentIndex += (int)move;
            if (_currentIndex < 0) _currentIndex = _builds.Length - 1;
            if (_currentIndex > _builds.Length - 1) _currentIndex = 0;
        }

        /// <summary>
        /// ビルドルートを決定するメソッド
        /// </summary>
        /// <returns>ビルドルートを初めて決定したか</returns>
        public bool SelectBuild()
        {
            if (!_selected)
            {
                //初めて選択した時の処理
                _selected = true;
                return true;
            }
            return false;
        }
    }
}
