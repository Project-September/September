using Fusion;
using September.Common;
using September.Lobby;
using System;
using UnityEngine;

public abstract class BuildViewBase : MonoBehaviour, IBuildView
{
    [SerializeField] protected BuildDatas _build;

    // 発火用
    event Action<BuildIndexMoveType> _onNextIndex;
    event Action<BuildIndexMoveType> _onBackIndex;
    event Action _onSelectBuild;
    // 購読・解除用
    // 普段の「+=」「-=」をメソッドにして役割分担
    public ActionDeregistration OnNextIndex(Action<BuildIndexMoveType> act)
    {
        _onNextIndex += act;
        return new(() => _onNextIndex -= act);
    }
    public ActionDeregistration OnBackIndex(Action<BuildIndexMoveType> act)
    {
        _onBackIndex += act;
        return new(() => _onBackIndex -= act);
    }
    public ActionDeregistration OnSelectBuild(Action act)
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
        _onNextIndex?.Invoke(BuildIndexMoveType.Next);
    }

    [ContextMenu("Back")]
    public void BackIndex()
    {
        _onBackIndex?.Invoke(BuildIndexMoveType.Back);
    }

    [ContextMenu("Select")]
    public void SelectBuild()
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
