using September.Common;
using System;
using UnityEngine;

namespace September.Lobby
{
    public abstract class BuildViewBase : MonoBehaviour
    {
        // 発火用
        event Action<BuildIndexMoveType> _onMoveIndex;
        event Action _onSelectBuild;
        // 購読・解除用
        // 普段の「+=」「-=」をメソッドにして役割分担
        public Action OnMoveIndex(Action<BuildIndexMoveType> act)
        {
            _onMoveIndex += act;
            return () => _onMoveIndex -= act;
        }
        public Action OnSelectBuild(Action act)
        {
            _onSelectBuild += act;
            return () => _onSelectBuild -= act;
        }

        public abstract void Init(BuildDataBase[] builds);

        #region 入力
        [ContextMenu("Next")]
        public void NextIndex()
        {
            _onMoveIndex?.Invoke(BuildIndexMoveType.Next);
        }

        [ContextMenu("Back")]
        public void BackIndex()
        {
            _onMoveIndex?.Invoke(BuildIndexMoveType.Back);
        }

        [ContextMenu("Select")]
        public virtual void SelectBuild()
        {
            _onSelectBuild?.Invoke();
        }
        #endregion

        /// <summary>ビルドルートの選択中かどうかの表示を切り替えるメソッド</summary>
        /// <param name="index">選択中のビルドルートのインデックス</param>
        /// <param name="build">選択中のビルドルートの詳細</param>
        public abstract void VisualizeBuildInfo(int index, BuildDataBase build);

        /// <summary>決定したビルドルートの表示を切り替えるメソッド</summary>
        /// <param name="selected">すでに決定されているかどうか</param>
        public abstract void VisualizeSelection(bool selected);
    }
}
