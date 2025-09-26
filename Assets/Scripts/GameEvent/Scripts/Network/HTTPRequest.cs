using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

using static Network.WebRequest;
using System.Text;

/// <summary>
/// リクエストを処理するワーカースクリプト
/// NOTE: MonoBehaviourとして機能し、並列に実行できる
/// NOTE: UniTaskで書き直せたらやってみよう
/// </summary>
public class HTTPRequest : MonoBehaviour
{
    //リトライ回数
    int RetryCount = 0;

    //処理中かどうか
    public bool IsActive { get; private set; }

    //通信管理オブジェクト
    class Packet
    {
        public string Uri;
        public byte[] Body = null;
        public RequestMethod Method;
        public GetData Delegate;
        public Options Opt = null;
    }

    //初期化
    void Awake()
    {
        IsActive = false;
    }
    
    /// <summary>
    /// リクエスト実行
    /// </summary>
    /// <param name="method">HTTPメソッド。GETとPOSTのみ対応</param>
    /// <param name="uri">通信先のURL</param>
    /// <param name="dlg">通信完了後にデータを送るデリゲート</param>
    /// <param name="body">POSTの際に通信内容に含めるデータ</param>
    /// <param name="opt">その他ヘッダ等の付加情報</param>
    public void Request(RequestMethod method, string uri, GetData dlg, byte[] body = null, Options opt = null)
    {
        IsActive = true;
        Packet p = new Packet();
        p.Uri = uri;
        p.Delegate = dlg;
        p.Method = method;
        p.Body = body;
        p.Opt = opt;
        StartCoroutine(Send(p));
    }

    /// <summary>
    /// リクエスト処理コルーチン
    /// </summary>
    /// <param name="p">通信情報</param>
    /// <returns></returns>
    IEnumerator Send(Packet p)
    {
        UnityWebRequest req = null;
        if (p.Method == RequestMethod.GET)
        {
            req = UnityWebRequest.Get(p.Uri);
        }
        if (p.Method == RequestMethod.POST)
        {
            req = new UnityWebRequest(p.Uri, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(p.Body),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");
        }
        if (p.Opt != null)
        {
            p.Opt.Header.ForEach(h => req.SetRequestHeader(h.Name, h.Value));
        }
        yield return req.SendWebRequest();

        if (req.error != null)
        {
            RetryCount++;
            if(RetryCount > 5)
            {
                IsActive = false;
                yield break;
            }
            Debug.LogError(req.uri + ":" + req.error);
            yield return new WaitForSeconds(1);
            Request(p.Method, p.Uri, p.Delegate, p.Body, p.Opt);
        }
        else
        {
            DataParse(p, req);
            IsActive = false;
        }
    }

    /// <summary>
    /// データ処理コールバック
    /// </summary>
    /// <param name="p"></param>
    /// <param name="req"></param>
    void DataParse(Packet p, UnityWebRequest req)
    {
        p.Delegate(Encoding.UTF8.GetString(req.downloadHandler.data));
        IsActive = false;
    }    
}