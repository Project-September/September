using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Result
{
    public class OkabeBonusRenderer : IAbilityBonusRenderer
    {
        private readonly Dictionary<ExhibitType, int> _okabeRideScores;

        public OkabeBonusRenderer(Dictionary<ExhibitType, int> scores)
        {
            _okabeRideScores = scores;
        }

        public int Render(ResultDataInbox inbox, Transform rowRoot, GameObject rowPrefab, TextMeshProUGUI abilityTitle)
        {
            int total = 0;
            abilityTitle.text = "Ride Bonus";

            foreach (var kv in _okabeRideScores)
            {
                var row = Object.Instantiate(rowPrefab, rowRoot);
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();

                int count = inbox.ExhibitCounts.GetValueOrDefault(kv.Key, 0);
                int score = count * kv.Value;

                texts[0].text = kv.Key.ToString();
                texts[1].text = $"×{count}";
                texts[2].text = score.ToString();

                total += score;
            }

            return total;
        }
    }
    
    public class HaruBonusRenderer : IAbilityBonusRenderer
    {
        private readonly Dictionary<ExhibitType, int> _haruDestroyScores;

        public HaruBonusRenderer(Dictionary<ExhibitType, int> scores)
        {
            _haruDestroyScores = scores;
        }

        public int Render(ResultDataInbox inbox, Transform rowRoot, GameObject rowPrefab, TextMeshProUGUI abilityTitle)
        {
            int total = 0;
            abilityTitle.text = "Destroy Bonus";

            foreach (var kv in _haruDestroyScores)
            {
                var row = Object.Instantiate(rowPrefab, rowRoot);
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                int count = inbox.ExhibitCounts.GetValueOrDefault(kv.Key, 0);
                int score = count * kv.Value;

                texts[0].text = kv.Key.ToString();
                texts[1].text = $"×{count}";
                texts[2].text = score.ToString();

                total += score;
            }

            return total;
        }
    }
    
    public class SarutobiBonusRenderer : IAbilityBonusRenderer
    {
        private readonly int _scorePerHook;

        public SarutobiBonusRenderer(int scorePerHook)
        {
            _scorePerHook = scorePerHook;
        }

        public int Render(ResultDataInbox inbox, Transform rowRoot, GameObject rowPrefab, TextMeshProUGUI abilityTitle)
        {
            // GrapplingHookCount を ResultDataInbox に追加する必要あり
            int count = inbox.GrapplingHookCount;
            int score = count * _scorePerHook;

            abilityTitle.text = "Grappling Bonus";

            var row = Object.Instantiate(rowPrefab, rowRoot);
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();

            texts[0].text = "グラップリングフック使用";
            texts[1].text = $"×{count}";
            texts[2].text = score.ToString();

            return score;
        }
    }
    
    public class TanihiraBonusRenderer : IAbilityBonusRenderer
    {
        private readonly int _scorePerFriend;

        public TanihiraBonusRenderer(int scorePerFriend)
        {
            _scorePerFriend = scorePerFriend;
        }

        public int Render(ResultDataInbox inbox, Transform rowRoot, GameObject rowPrefab, TextMeshProUGUI abilityTitle)
        {
            // FriendExhibitCount を ResultDataInbox に追加する必要あり
            int count = inbox.FriendExhibitCount;
            int score = count * _scorePerFriend;

            abilityTitle.text = "Friend Bonus";

            var row = Object.Instantiate(rowPrefab, rowRoot);
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();

            texts[0].text = "オトモダチ増加";
            texts[1].text = $"×{count}";
            texts[2].text = score.ToString();

            return score;
        }
    }
}