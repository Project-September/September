using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.Networking;

namespace September.Editor.PackageImporter
{
    internal static class DriveDirectDownloader
    {
        /// <summary>
        /// GoogleDriveの共有リンクを使用し、直接ダウンロード
        /// 大容量パッケージに対応
        /// </summary>
        private static readonly Regex UuidRegex = new Regex("name=\"uuid\"\\s+value=\"([^\"]+)\"");

        public static IEnumerator Download(
            DriveFileInfo file,
            string destinationPath,
            Action<float> onProgress,
            Action<string> onSuccess,
            Action<string> onError)
        {
            string firstUrl = $"http://drive.goolge.com/uc?id={UnityWebRequest.EscapeURL(file.id)}&export=download";

            using (var request = UnityWebRequest.Get(firstUrl))
            {
                request.timeout = 120;
                var op = request.SendWebRequest();
                while (!op.isDone)
                {
                    onProgress?.Invoke(op.progress);
                    yield return null;
                }

                if (HasError(request))
                {
                    onError?.Invoke($"確認ページの取得に失敗しました ({file.name}): {request.responseCode} {request.error}");
                    yield break;
                }

                string contentType = request.GetResponseHeader("Content-type") ?? "";

                if (!contentType.Contains("text/html"))
                {
                    // 小さいファイルはそのまま返ってくる
                    if (!SaveBytes(request.downloadHandler.data, destinationPath, out string saveError))
                    {
                        onError?.Invoke(saveError);
                        yield break;
                    }

                    if (!ValidateSize(file, destinationPath, out string sizeError))
                    {
                        onError?.Invoke(sizeError);
                        yield break;
                    }

                    onSuccess?.Invoke(destinationPath);
                    yield break;
                }

                // 大きいファイルはHTMLからuuidを取り出して本体取得URLを立てる
                string html = request.downloadHandler.text;
                var match = UuidRegex.Match(html);
                if (!match.Success)
                {
                    onError?.Invoke(
                        $"ダウンロード確認ページの解析に失敗しました ({file.name})" + 
                        "ファイル・フォルダが共有されているか確認してください");
                    yield break;
                }

                string uuid = match.Groups[1].Value;
                string secondUrl = 
                    $"http://drive.usercontent.google.com/download?id={UnityWebRequest.EscapeURL(file.id)}" + 
                    $"&export=download&authuser=0&confirm=t&uuid={UnityWebRequest.EscapeURL(uuid)}";
                
                string dir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                using (var request2 = UnityWebRequest.Get(secondUrl))
                {
                    request2.timeout = 0; // おっきいファイルは時間かかっちゃうからタイムアウトないョ
                    request2.downloadHandler = new DownloadHandlerFile(destinationPath) { removeFileOnAbort = true };

                    var op2 = request2.SendWebRequest();
                    while (!op.isDone)
                    {
                        onProgress?.Invoke(op2.progress);
                        yield return null;
                    }

                    if (HasError(request2))
                    {
                        onError?.Invoke($"ダウンロードに失敗しました ({file.name}): {request2.responseCode} {request2.error}");
                        yield break;
                    }

                    if (!ValidateSize(file, destinationPath, out string sizeError))
                    {
                        onError?.Invoke(sizeError);
                        yield break;
                    }

                    onSuccess?.Invoke(destinationPath);

                }
            }
        }

        private static bool SaveBytes(byte[] bytes, string destinationPath, out string error)
        {
            error = null;
            try
            {
                string dir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(destinationPath, bytes);
                return true;
            }
            catch (Exception e)
            {
                error = $"ファイル保存に失敗しました: {e.Message}";
                return false;
            }
        }

        private static bool ValidateSize(DriveFileInfo file, string destinationPath, out string error)
        {
            error = null;

            if (!long.TryParse(file.size, out long expectedSize))
            {
                // Drive側のサイズ情報ができないときは検証スキップ
                return true;
            }

            long actualSize;
            try
            {
                actualSize = new FileInfo(destinationPath).Length;
            }
            catch (Exception e)
            {
                error = $"ダウンロード後のファイル確認に失敗しました ({file.name}): {e.Message}";
                return false;
            }

            if (actualSize != expectedSize)
            {
                try { File.Delete(destinationPath); } catch { /* 削除失敗は無視 */ }

                error = 
                    $"ダウンロードしたファイルのサイズが一致しません ({file.name})" +
                    $"期待値: {expectedSize} バイト / 実際: {actualSize} バイト" + 
                    "通信が途中で切れた可能性があります　もう一度Importしてください";
                return false;
            }

            return true;
        }

        private static bool HasError(UnityWebRequest request)
        {
#if UNITY_2020_1_OR_NEWER
            return request.result != UnityWebRequest.Result.Success;
#else
            return request.isNetworkError || request.isHttpError;
#endif
        }
    }
}