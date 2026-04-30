using UnityEngine;

/// <summary>ビルドシステムの機能を定義したインターフェース</summary>
public interface IBuild
{
    /// <summary>この強化要素を選択した時に最初にやること</summary>
    void Entry();
    void Build();
    void Exit();
}
