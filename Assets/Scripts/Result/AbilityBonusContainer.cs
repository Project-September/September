using System.Collections.Generic;
using System.Linq;
using September.Common;
using TMPro;
using UnityEngine;

namespace Result
{
    public static class AbilityBonusContainer
    {
        private static ExhibitScoreConfig _okabeRideConfig;
        private static ExhibitScoreConfig _haruDestroyConfig;
        private static int _sarutobiBonusScore;
        private static ExhibitScoreConfig _tanihiraFriendConfig;

        public static void Init(
            ExhibitScoreConfig okabe,
            ExhibitScoreConfig haru,
            int sarutobi,
            ExhibitScoreConfig tanihira)
        {
            _okabeRideConfig = okabe;
            _haruDestroyConfig = haru;
            _sarutobiBonusScore = sarutobi;
            _tanihiraFriendConfig = tanihira;
        }
        
        public static int Render(CharacterType type, ResultDataInbox inbox,
            Transform rowRoot, GameObject rowPrefab, TextMeshProUGUI abilityTitle)
        {
            switch (type)
            {
                case CharacterType.OkabeWright:
                    abilityTitle.text = "Ride Bonus";
                    return RenderExhibitBonus(inbox, rowRoot, rowPrefab, _okabeRideConfig);

                case CharacterType.HulkTheButcher: 
                    abilityTitle.text = "Destroy Bonus";
                    return RenderExhibitBonus(inbox, rowRoot, rowPrefab, _haruDestroyConfig, destroyed: true);

                case CharacterType.Sarutobi:
                    abilityTitle.text = "Hook Bonus";
                    return RenderSimpleBonus(rowRoot, rowPrefab, "GrapplingHook", inbox.GrapplingHookCount, _sarutobiBonusScore);

                case CharacterType.Tanihira:
                    abilityTitle.text = "Friend Bonus";
                    return RenderExhibitBonus(inbox, rowRoot,rowPrefab, _tanihiraFriendConfig);

                default:
                    abilityTitle.text = "No Bonus";
                    return 0;
            }
        }
        
        private static int RenderExhibitBonus(ResultDataInbox inbox, Transform root, GameObject prefab,
            ExhibitScoreConfig config, bool destroyed = false)
        {
            int total = 0;
            
            foreach (ExhibitScoreEntry kv in config.Entries)
            {
                int count = destroyed
                    ? inbox.DestroyedExhibitCounts.GetValueOrDefault(kv.Type, 0)
                    : inbox.ExhibitCounts.GetValueOrDefault(kv.Type, 0);

                int score = count * kv.Points;

                GameObject row = Object.Instantiate(prefab, root);
                TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                texts[0].text = kv.Type.ToDisplayName();
                texts[1].text = $"×{count}";
                texts[2].text = score.ToString();

                total += score;
            }
            return total;
        }

        private static int RenderSimpleBonus(Transform root, GameObject prefab,
            string label, int count, int point)
        {
            int total = count * point;
            var row = Object.Instantiate(prefab, root);
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            texts[0].text = label;
            texts[1].text = $"×{count}";
            texts[2].text = total.ToString();
            return total;
        }
        
        public static int CalcBonus(CharacterType type, ScoreTracker inbox)
        {
            switch (type)
            {
                case CharacterType.OkabeWright:
                    return _okabeRideConfig.Entries.Sum(e =>
                        inbox.ExhibitCounts.GetValueOrDefault(e.Type, 0) * e.Points);

                case CharacterType.HulkTheButcher:
                    return _haruDestroyConfig.Entries.Sum(e =>
                        inbox.DestroyedExhibitCounts.GetValueOrDefault(e.Type, 0) * e.Points);

                case CharacterType.Sarutobi:
                    return inbox.GrapplingHookCount * _sarutobiBonusScore;

                case CharacterType.Tanihira:
                    return _tanihiraFriendConfig.Entries.Sum(e =>
                        inbox.ExhibitCounts.GetValueOrDefault(e.Type, 0) * e.Points);

                default:
                    return 0;
            }
        }
    }
}