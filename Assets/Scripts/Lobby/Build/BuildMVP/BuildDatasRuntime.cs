using Fusion;
using September.Common;
using System;
using UnityEngine.SceneManagement;

namespace September.Lobby
{
    public class BuildDatasRuntime
    {
        readonly BuildDataBase[] _builds;
        int _currentIndex;
        int _selectedIndex;
        NetworkRunner _networkRunner;

        // 発火用
        event Action<int, BuildDataBase> _onMoveIndex;
        event Action<int> _onSelectBuild;
        // 購読・解除用
        // 普段の「+=」「-=」をメソッドにして役割分担
        public Action OnMoveIndex(Action<int, BuildDataBase> act)
        {
            _onMoveIndex += act;
            return () => _onMoveIndex -= act;
        }
        public Action OnSelectBuild(Action<int> act)
        {
            _onSelectBuild += act;
            return () => _onSelectBuild -= act;
        }

        public BuildDatasRuntime(BuildDatas data)
        {
            _builds = data.Builds;
            _selectedIndex = -1;
            _networkRunner = NetworkRunner.GetRunnerForScene(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// 選択を切り替えるメソッド
        /// </summary>
        /// <param name="move">選択を切り替える方向</param>
        public void MoveIndex(BuildIndexMoveType move)
        {
            if (_builds.Length == 0) return;
            _currentIndex += (int)move;
            if (_currentIndex < 0) _currentIndex = _builds.Length - 1;
            if (_currentIndex > _builds.Length - 1) _currentIndex = 0;
            _onMoveIndex?.Invoke(_currentIndex, _builds[_currentIndex]);
        }

        /// <summary>
        /// ダイレクトに要素を選択して切り替えるメソッド
        /// </summary>
        /// <param name="index">切り替えるインデックス</param>
        public void MoveIndex(int index)
        {
            if (_builds.Length == 0) return;
            if (index < 0 || _builds.Length - 1 < index) return;
            _currentIndex = index;
            _onMoveIndex?.Invoke(_currentIndex, _builds[_currentIndex]);
        }

        /// <summary>
        /// ビルドルートを決定するメソッド
        /// </summary>
        public void SelectBuild()
        {
            if (_networkRunner == null || PlayerDatabase.Instance == null)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning("サーバー接続に失敗しました");
#endif
                return;
            }
            // NetworkRunnerとPlayerDatabaseはnullじゃない前提
            var player = _networkRunner.LocalPlayer;
            PlayerDatabase.Instance.Rpc_SetBuild(player, _builds[_currentIndex].BuildType);
            _onSelectBuild?.Invoke(_selectedIndex);

            // 直前に決定したインデックスを保存
            _selectedIndex = _currentIndex;
        }
    }
}
