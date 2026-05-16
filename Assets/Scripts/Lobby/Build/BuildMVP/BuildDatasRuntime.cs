using September.Common;
using System;

namespace September.Lobby
{
    public class BuildDatasRuntime
    {
        int _buildArrLength;
        int _currentIndex;
        int _selectedIndex;
        bool _selected;

        // 発火用
        event Action<int> _onMoveIndex;
        event Action<bool, int> _onSelectBuild;
        // 購読・解除用
        // 普段の「+=」「-=」をメソッドにして役割分担
        public ActionDeregistration OnMoveIndex(Action<int> act)
        {
            _onMoveIndex += act;
            return new(() => _onMoveIndex -= act);
        }
        public ActionDeregistration OnSelectBuild(Action<bool, int> act)
        {
            _onSelectBuild += act;
            return new(() => _onSelectBuild -= act);
        }

        public BuildDatasRuntime(BuildDatas data)
        {
            _buildArrLength = data.Builds.Count;
        }

        /// <summary>
        /// 選択を切り替えるメソッド
        /// </summary>
        /// <param name="move">選択を切り替える方向</param>
        /// <returns>選択後の要素番号</returns>
        public void MoveIndex(BuildIndexMoveType move)
        {
            _currentIndex += (int)move;
            if (_currentIndex < 0) _currentIndex = _buildArrLength - 1;
            if (_currentIndex > _buildArrLength - 1) _currentIndex = 0;
            _onMoveIndex?.Invoke(_currentIndex);
        }

        /// <summary>
        /// ビルドルートを決定するメソッド
        /// </summary>
        /// <returns>すでに決定しているかどうか</returns>
        public void SelectBuild()
        {
            var selected = _selected;
            if (!selected)
            {
                // 初めて選択した時の処理
                _selected = true;
                _selectedIndex = _currentIndex;
            }
            _onSelectBuild(selected, _selectedIndex);
        }
    }
}
