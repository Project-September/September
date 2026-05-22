using September.Common;
using System;
using UnityEngine;

namespace September.Lobby
{
    public abstract class BuildViewBase : MonoBehaviour
    {
        [SerializeField] protected BuildDatas _build;

        // 発火用
        event Action<BuildIndexMoveType> _onMoveIndex;
        event Action _onSelectBuild;
        // 購読・解除用
        // 普段の「+=」「-=」をメソッドにして役割分担
        public ActionDisposable OnMoveIndex(Action<BuildIndexMoveType> act)
        {
            _onMoveIndex += act;
            return new(() => _onMoveIndex -= act);
        }
        public ActionDisposable OnSelectBuild(Action act)
        {
            _onSelectBuild += act;
            return new(() => _onSelectBuild -= act);
        }

        private void Awake()
        {
            Init();
        }

        protected abstract void Init();

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

        public virtual void VisualizeBuildInfo(int index)
        {
#if UNITY_EDITOR
            var build = _build.Builds[index];
            if (build == null) return;
            Debug.Log($"選択中 : {build.BuildName}\n説明 : {build.BuildInfo}");
#endif
        }

        public virtual void VisualizeSelection(bool selected, int index)
        {
#if UNITY_EDITOR
            var build = _build.Builds[index];
            if (build == null) return;
            Debug.Log(!selected ? "ビルドルート決定" : "ビルドルートは決定されています"
            + $"\n決定ビルド : {build.BuildName}");
#endif
        }
    }
}
