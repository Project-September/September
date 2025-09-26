using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// リクエスト処理クラス
    /// </summary>
    public class WebRequest
    {
        //データ処理デリゲート
        public delegate void GetData(string result);

        //リクエストヘッダ
        public class Header
        {
            public string Name;
            public string Value;
        }

        //リクエスト処理につけるオプション
        public class Options
        {
            public List<Header> Header = new List<Header>(); //リクエストヘッダー
        }

        //メソッド
        public enum RequestMethod
        {
            GET,
            POST
        }

        //このクラスはstatic的に機能する
        static WebRequest Instance = new WebRequest();

        List<TaskRequestWorker> _worker = new List<TaskRequestWorker>();
        CancellationToken ct;

        /// <summary>
        /// ワーカー設定
        /// </summary>
        static void CheckWorkerInstance()
        {
            if(Instance._worker.Count == 0)
            {
                for (int i = 0; i < 5; ++i)
                {
                    Instance._worker.Add(new TaskRequestWorker());
                }
            }

            var active = Instance._worker.Where(r => r.IsActive == false);
            if(active.Count() == 0)
            {
                Instance._worker.Add(new TaskRequestWorker());
            }
        }

        /// <summary>
        /// ワーカー設定
        /// </summary>
        static TaskRequestWorker GetWorker()
        {
            TaskRequestWorker ret = null;
            var active = Instance._worker.Where(r => r.IsActive == false);
            if (active.Count() == 0)
            {
                ret = new TaskRequestWorker();
                Instance._worker.Add(ret);
            }
            else
            {
                ret = active.First();
            }
            return ret;
        }

        /// <summary>
        /// GET通信をする(asyncラッパー)
        /// </summary>
        /// <param name="uri">通信先のURL</param>
        /// <param name="dlg">データ受信コールバック</param>
        /// <param name="opt">ヘッダなど追加で含む情報</param>
        static public async UniTask<string> GetRequest(string uri, Options opt = null)
        {
            CheckWorkerInstance();
            var worker = GetWorker();
            return await worker.GetRequest(uri, opt);
        }

        /// <summary>
        /// POST通信をする(asyncラッパー)
        /// </summary>
        /// <param name="uri">通信先のURL</param>
        /// <param name="body">サーバに送信する内容</param>
        /// <param name="dlg">データ受信コールバック</param>
        /// <param name="opt">ヘッダなど追加で含む情報</param>
        static public async UniTask<string> PostRequest<T>(string uri, T body, Options opt = null)
        {
            CheckWorkerInstance();
            var worker = GetWorker();
            string json = JsonUtility.ToJson(body);
            return await worker.PostRequest(uri, json, opt);
        }

        /// <summary>
        /// POST通信をする(asyncラッパー)
        /// </summary>
        /// <param name="uri">通信先のURL</param>
        /// <param name="body">サーバに送信する内容</param>
        /// <param name="opt">ヘッダなど追加で含む情報</param>
        static public async UniTask<string> PostRequest<T>(string uri, string body, Options opt = null)
        {
            CheckWorkerInstance();
            var worker = GetWorker();
            return await worker.PostRequest(uri, body, opt);
        }


        /// <summary>
        /// GET通信をする(コールバック運用)
        /// </summary>
        /// <param name="uri">通信先のURL</param>
        /// <param name="dlg">データ受信コールバック</param>
        /// <param name="opt">ヘッダなど追加で含む情報</param>
        static public void GetRequest(string uri, GetData dlg, Options opt = null)
        {
            CheckWorkerInstance();
            UniTask.RunOnThreadPool(async() =>
            {
                var worker = GetWorker();
                Debug.Log(uri);
                string result = await worker.GetRequest(uri, opt);
                UniTask.Post(() =>
                {
                    dlg?.Invoke(result);
                });
            }).Forget();
        }

        /// <summary>
        /// POST通信をする(コールバック運用)
        /// </summary>
        /// <param name="uri">通信先のURL</param>
        /// <param name="body">サーバに送信する内容</param>
        /// <param name="dlg">データ受信コールバック</param>
        /// <param name="opt">ヘッダなど追加で含む情報</param>
        static public void PostRequest<T>(string uri, T body, GetData dlg, Options opt = null)
        {
            CheckWorkerInstance();
            UniTask.RunOnThreadPool(async () =>
            {
                var worker = GetWorker();
                string json = JsonUtility.ToJson(body);
                string result = await worker.PostRequest(uri, json, opt);
                UniTask.Post(() =>
                {
                    dlg?.Invoke(result);
                });
            }).Forget();
        }

        /// <summary>
        /// POST通信をする(コールバック運用)
        /// </summary>
        /// <param name="uri">通信先のURL</param>
        /// <param name="body">サーバに送信する内容</param>
        /// <param name="dlg">データ受信コールバック</param>
        /// <param name="opt">ヘッダなど追加で含む情報</param>
        static public void PostRequest<T>(string uri, string body, GetData dlg, Options opt = null)
        {
            CheckWorkerInstance();
            UniTask.RunOnThreadPool(async () =>
            {
                var worker = GetWorker();
                string result = await worker.PostRequest(uri, body, opt);
                UniTask.Post(() =>
                {
                    dlg?.Invoke(result);
                });
            }).Forget();
        }
    }
}
