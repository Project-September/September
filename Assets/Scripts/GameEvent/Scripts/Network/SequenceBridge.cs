using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// シーン間で処理する何かを橋渡しするクラス
/// </summary>
public class SequenceBridge
{
    //このクラスはstatic的に機能する
    static SequenceBridge _instance = new SequenceBridge();
    static public SequenceBridge Instance => _instance;
    private SequenceBridge() { }

    //シーケンスデータ
    Dictionary<string, SequencePackage> _sequenceDeck = new Dictionary<string, SequencePackage>();

    /// <summary>
    /// シーケンス登録
    /// </summary>
    static public bool RegisterSequence(string key, SequencePackage task)
    {
        if (_instance._sequenceDeck.ContainsKey(key))
        {
            Debug.Log("重複して登録している");
            return false;
        }

        _instance._sequenceDeck.Add(key, task);
        return true;
    }

    /// <summary>
    /// シーケンスを貰う
    /// </summary>
    static public T GetSequencePackage<T>(string key) where T : SequencePackage
    {
        if (!_instance._sequenceDeck.ContainsKey(key))
        {
            Debug.Log("登録されていない");
            return default;
        }

        return _instance._sequenceDeck[key] as T;
    }

    /// <summary>
    /// 準備完了状態のシーケンスを貰う非同期処理
    /// </summary>
    static public async UniTask<T> GetSequencePackageWaitForReady<T>(string key) where T : SequencePackage
    {
        if (!_instance._sequenceDeck.ContainsKey(key))
        {
            Debug.Log("登録されていない");
            return default;
        }

        T ret = _instance._sequenceDeck[key] as T;
        await UniTask.WaitUntil(() => ret.IsReady);
        return ret;
    }

    /// <summary>
    /// シーケンスの削除
    /// </summary>
    static public bool DeleteSequence(string key)
    {
        if (!_instance._sequenceDeck.ContainsKey(key))
        {
            Debug.Log("登録されていない");
            return false;
        }

        _instance._sequenceDeck[key].Dispose();
        _instance._sequenceDeck.Remove(key);
        return true;
    }
}

/// <summary>
/// 派生前提のデータ格納パッケージ
/// </summary>
public class SequencePackage
{
    CancellationToken _token;
    UniTask _task;

    public bool IsReady { get; set; }

    protected virtual void Awake() { }

    public void Dispose()
    {
        _token.ThrowIfCancellationRequested();
    }

    static public SequencePackage Create<T>(UniTask task) where T : SequencePackage, new()
    {
        T package = new T();
        package._token = new CancellationToken();
        package.Awake();
        package._task = task;
        task.AttachExternalCancellation(package._token);
        task.Forget();
        package.IsReady = false;
        return package;
    }
}