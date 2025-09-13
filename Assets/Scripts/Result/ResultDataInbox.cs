using System;
using System.Collections.Generic;
using UnityEngine;

namespace Result
{
    public class ResultDataInbox : SingletonMonoBehaviour<ResultDataInbox>
    {
        private readonly Dictionary<ExhibitType, int> _exhibitCounts = new();
        public IReadOnlyDictionary<ExhibitType, int> ExhibitCounts => _exhibitCounts;
        
        private readonly Dictionary<ExhibitType, int> _destroyedExhibitCounts = new();
        public IReadOnlyDictionary<ExhibitType, int> DestroyedExhibitCounts => _destroyedExhibitCounts;

        // その他のカウント
        public int GrapplingHookCount { get; private set; }
        public int FriendExhibitCount { get; private set; }

        public event Action OnChanged;

        /// <summary>
        /// すべてのデータをリセット
        /// </summary>
        public void Clear()
        {
            GrapplingHookCount = 0;
            FriendExhibitCount = 0;
            _exhibitCounts.Clear();
            _destroyedExhibitCounts.Clear();
            OnChanged?.Invoke();
        }

        /// <summary>
        /// サーバーから送られてきた文字列をデコードして保存
        /// 文字は頭文字
        /// </summary>
        public void LoadFromEncoded(string payload, int pageTotal)
        {
            _exhibitCounts.Clear();
            _destroyedExhibitCounts.Clear();
            GrapplingHookCount = 0;
            FriendExhibitCount = 0;

            if (string.IsNullOrEmpty(payload))
            {
                Debug.LogWarning("[ResultDataInbox] Empty payload");
                OnChanged?.Invoke();
                return;
            }

            try
            {
                string[] blocks = payload.Split('|', StringSplitOptions.RemoveEmptyEntries);

                foreach (string b in blocks)
                {
                    if (string.IsNullOrEmpty(b))
                        continue;

                    if (b.StartsWith("V"))
                        continue;

                    if (b.StartsWith("E:"))
                    {
                        string[] items = b[2..].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (string it in items)
                        {
                            string[] kv = it.Split('=', StringSplitOptions.RemoveEmptyEntries);
                            if (kv.Length != 2) 
                                continue;

                            if (Enum.TryParse(kv[0], out ExhibitType t) &&
                                int.TryParse(kv[1], out int c))
                            {
                                _exhibitCounts[t] = Mathf.Max(0, c);
                            }
                        }
                    }
                    else if (b.StartsWith("D:"))
                    {
                        string[] items = b[2..].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (string it in items)
                        {
                            string[] kv = it.Split('=', StringSplitOptions.RemoveEmptyEntries);
                            if (kv.Length != 2) continue;

                            if (Enum.TryParse(kv[0], out ExhibitType t) &&
                                int.TryParse(kv[1], out int c))
                            {
                                _destroyedExhibitCounts[t] = Mathf.Max(0, c);
                            }
                        }
                    }
                    else if (b.StartsWith("S:")) 
                    {
                        string s = b[2..];
                        if (int.TryParse(s, out int stun))
                            Mathf.Max(0, stun);
                    }
                    else if (b.StartsWith("G:")) 
                    {
                        if (int.TryParse(b[2..], out int g))
                            GrapplingHookCount = Mathf.Max(0, g);
                    }
                    else if (b.StartsWith("F:"))
                    {
                        if (int.TryParse(b[2..], out int f))
                            FriendExhibitCount = Mathf.Max(0, f);
                    }
                }

                OnChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResultDataInbox] Decode error: {ex}");
            }
        }
    }
}
