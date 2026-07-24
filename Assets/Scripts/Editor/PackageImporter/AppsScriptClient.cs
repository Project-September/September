using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace September.Editor.PackageImporter
{
    [Serializable]
    public class DriveFileInfo
    {
        public string id;
        public string name;
        public string modifiedTime;
        public string size;
    }

    [Serializable]
    internal class ListResponse
    {
        public List<DriveFileInfo> files;
        public string error;
    }

    /// <summary>
    /// GAS経由でDriveのフォルダを一覧取得する
    /// </summary>
    internal static class AppsScriptClient
    {
        public static IEnumerator ListUnityPackages(
            string webAppUrl,
            string folderId,
            Action<List<DriveFileInfo>> onSuccess,
            Action<string> onError)
        {
            if (string.IsNullOrEmpty(webAppUrl))
            {
                onError?.Invoke("Web App URLが設定されていません");
                yield break;
            }

            string url = $"{webAppUrl}?action=list&folderId={UnityWebRequest.EscapeURL(folderId)}";

            using (var request = UnityWebRequest.Get(url))
            {
                var op = request.SendWebRequest();
                while (!op.isDone) yield return null;

                if (HasError(request))
                {
                    onError?.Invoke($"一覧取得に失敗しました: {request.responseCode} {request.error}");
                    yield break;
                }

                ListResponse response;
                try
                {
                    response = JsonUtility.FromJson<ListResponse>(request.downloadHandler.text);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"レスポンスの解析に失敗しました: {e.Message}\n{Truncate(request.downloadHandler.text)}");
                    yield break;
                }

                if (!string.IsNullOrEmpty(response?.error))
                {
                    onError?.Invoke($"Apps Script側エラー: {response.error}");
                    yield break;
                }

                onSuccess?.Invoke(response?.files ?? new List<DriveFileInfo>());
            }
        }

        private static bool HasError(UnityWebRequest request)
        {
#if UNITY_2020_1_OR_NEWER
            return request.result != UnityWebRequest.Result.Success;
#else
            return request.isNetworkError || request.isHttpError;
#endif
        }

        private static string Truncate(string s, int max = 300)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }
    }
}