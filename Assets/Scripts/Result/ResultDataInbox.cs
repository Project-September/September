using System;
using System.Collections.Generic;
using UnityEngine;

namespace Result
{
    public class ResultDataInbox : SingletonMonoBehaviour<ResultDataInbox>
    {

        public int RoundId { get; private set; }
        public int StunCount { get; private set; }
        public int PageTotal { get; private set; } // ページ2の合計（サーバー計の自分の最終合計）
        public IReadOnlyDictionary<ExhibitType, int> ExhibitCounts => _exhibitCounts;

        private readonly Dictionary<ExhibitType, int> _exhibitCounts = new();

        public event Action OnChanged;


        public void Clear()
        {
            RoundId = 0;
            StunCount = 0;
            PageTotal = 0;
            _exhibitCounts.Clear();
            OnChanged?.Invoke();
        }

        // "V1|E:Pteranodon=3,...|S:2"
        public void LoadFromEncoded(int roundId, string payload, int pageTotal)
        {
            RoundId = roundId;
            PageTotal = pageTotal;
            _exhibitCounts.Clear();
            StunCount = 0;

            if (string.IsNullOrEmpty(payload))
            {
                Debug.LogWarning("[ResultDataInbox] Empty payload");
                OnChanged?.Invoke();
                return;
            }

            try
            {
                var blocks = payload.Split('|'); // V1 | E:... | S:...
                foreach (var b in blocks)
                {
                    if (string.IsNullOrEmpty(b)) continue;
                    if (b.StartsWith("V")) continue;

                    if (b.StartsWith("E:"))
                    {
                        var items = b.Substring(2).Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var it in items)
                        {
                            var kv = it.Split('=', StringSplitOptions.RemoveEmptyEntries);
                            if (kv.Length != 2) continue;

                            if (Enum.TryParse(kv[0], out ExhibitType t) && int.TryParse(kv[1], out var c))
                            {
                                _exhibitCounts[t] = Mathf.Max(0, c);
                            }
                        }
                    }
                    else if (b.StartsWith("S:"))
                    {
                        var s = b.Substring(2);
                        if (int.TryParse(s, out var stun)) StunCount = Mathf.Max(0, stun);
                    }
                }

                // デバッグ出力
                Debug.Log($"[ResultDataInbox] Round {RoundId} received. Stun={StunCount}, PageTotal={PageTotal}");
                foreach (var kv in _exhibitCounts)
                    Debug.Log($"[ResultDataInbox]  Exhibit {kv.Key} x{kv.Value}");

                OnChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResultDataInbox] Decode error: {ex}");
            }
        }
    }
}
