using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using static Network.WebRequest;
using System.Net.Http;


namespace Network
{
    public class TaskRequestWorker
    {
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

        HttpClient _client = null;

        //初期化
        public TaskRequestWorker()
        {
            IsActive = false;
            _client = new HttpClient();
        }

        /// <summary>
        /// リクエスト実行
        /// </summary>
        /// <param name="method">HTTPメソッド。GETとPOSTのみ対応</param>
        /// <param name="uri">通信先のURL</param>
        /// <param name="dlg">通信完了後にデータを送るデリゲート</param>
        /// <param name="body">POSTの際に通信内容に含めるデータ</param>
        /// <param name="opt">その他ヘッダ等の付加情報</param>

        public async Task<string> PostRequest(string uri, string body, Options opt = null)
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                IsActive = true;
                using HttpResponseMessage response = await _client.PostAsync(uri, new StringContent(body, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                IsActive = false;
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Debug.Log("\nException Caught!");
                Debug.Log("Message :"+ e.Message);
                IsActive = false;
            }

            return null;
        }


        public async Task<string> GetRequest(string uri, Options opt = null)
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                IsActive = true;
                using HttpResponseMessage response = await _client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                IsActive = false;
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Debug.Log("\nException Caught!");
                Debug.Log("Message :" + e.Message);
                IsActive = false;
            }

            return null;
        }
    }
}
