using September.Common;
using System;

namespace September.Lobby
{
    public class BuildDatasRuntime
    {
        readonly BuildDataBase[] _builds;
        int _currentIndex;
        bool _selected;

        // 発火用
        event Action<int, BuildDataBase> _onMoveIndex;
        event Action<bool, int, BuildDataBase> _onSelectBuild;
        // 購読・解除用
        // 普段の「+=」「-=」をメソッドにして役割分担
        public Action OnMoveIndex(Action<int, BuildDataBase> act)
        {
            _onMoveIndex += act;
            return () => _onMoveIndex -= act;
        }
        public Action OnSelectBuild(Action<bool, int, BuildDataBase> act)
        {
            _onSelectBuild += act;
            return () => _onSelectBuild -= act;
        }

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
            if (_builds.Length == 0) return;
            _currentIndex += (int)move;
            if (_currentIndex < 0) _currentIndex = _builds.Length - 1;
            if (_currentIndex > _builds.Length - 1) _currentIndex = 0;
            _onMoveIndex?.Invoke(_currentIndex, _builds[_currentIndex]);
        }

        /// <summary>
        /// ビルドルートを決定するメソッド
        /// </summary>
        /// <returns>すでに決定しているかどうか</returns>
        public void SelectBuild()
        {
            var selected = _selected;
            if (!selected) _selected = true; // 初めて選択した時の処理

            _onSelectBuild?.Invoke(selected, _currentIndex, _builds[_currentIndex]);
        }
    }
}
