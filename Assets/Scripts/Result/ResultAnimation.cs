using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using September.Common; // PlayerDatabase

namespace Result
{
    [Serializable]
    public struct CharacterIconPair
    {
        public CharacterType Type;
        public Sprite Icon;
    }

    public class ResultAnimation : MonoBehaviour
    {
        [Header("Prefab / Root")]
        [SerializeField] private ResultUIRootRefs _resultUIRootPrefab;
        private ResultUIRootRefs _resultUIRoot;

        [Header("Delays")]
        [SerializeField] private float _startDelay = 0.5f;

        [Header("Text Animation")]
        [SerializeField] private float _fadeInDuration = 0.6f;
        [SerializeField] private float _scaleDuration = 0.6f;
        [SerializeField] private float _scaleTarget = 1.8f;
        [SerializeField] private Ease _scaleEase = Ease.OutBack;
        [SerializeField] private float _holdDuration = 1.0f;
        [SerializeField] private float _fadeOutDuration = 1.0f;

        [Header("Background Fade")]
        [SerializeField] private Ease _bgEase = Ease.Linear;
        [SerializeField] private float _bgFadeDuration = 1.0f;

        [Header("List Animation")]
        [SerializeField] private float _rowStagger = 0.1f;
        [SerializeField] private float _rowFadeDuration = 0.25f;
        [SerializeField] private float _rowSlideOffset = 40f;
        [SerializeField] private float _scoreRollDuration = 1.2f;
        [SerializeField] private bool _scoreUseThousands = true;

        [Header("Sort Animation")]
        [SerializeField] private float _sortMoveDuration = 0.5f;
        [SerializeField] private float _sortStagger = 0.06f;

        [Header("Your Rank")]
        [SerializeField] private float _yourRankFadeDuration = 0.35f;
        [SerializeField] private Vector2 _yourRankOffset = new(0f, 20f);

        [Header("Icons")]
        [SerializeField] private CharacterIconPair[] _iconTable;
        [SerializeField] private Sprite _defaultIcon;

        // --- Cached UI refs
        private TextMeshProUGUI _finishText;
        private TextMeshProUGUI _resultText;
        private Image _resultBg;
        private RectTransform _rowsRoot;
        private TextMeshProUGUI[] _nameSlots;
        private TextMeshProUGUI[] _scoreSlots;
        private TextMeshProUGUI[] _rankSlots;
        private Image[] _iconSlots;
        private TextMeshProUGUI _yourRankText;

        private Dictionary<CharacterType, Sprite> _iconMap;

        // === Entry ===
        public async UniTask Play(ResultUIRootRefs resultUIRoot)
        {
            _resultUIRoot = resultUIRoot;
            CacheRefs();

            await UniTask.Delay(TimeSpan.FromSeconds(_startDelay));

            await AnimateIntroTexts();
            await AnimateBackground();
            await AnimateRows();
            await AnimateYourRank();
        }

        private void CacheRefs()
        {
            _finishText = _resultUIRoot.FinishText;
            _resultText = _resultUIRoot.ResultText;
            _resultBg = _resultUIRoot.ResultBg;
            _rowsRoot = _resultUIRoot.RowsRoot;
            _nameSlots = _resultUIRoot.NameSlots;
            _scoreSlots = _resultUIRoot.ScoreSlots;
            _rankSlots = _resultUIRoot.RankSlots;
            _iconSlots = _resultUIRoot.IconSlots;
            _yourRankText = _resultUIRoot.YourRankText;

            _finishText.gameObject.SetActive(false);
            _resultText.gameObject.SetActive(false);
            _resultBg.gameObject.SetActive(false);
            _yourRankText.gameObject.SetActive(false);

            foreach (var slot in _nameSlots)
            {
                if (!slot) continue;
                (slot.transform.parent?.gameObject ?? slot.gameObject).SetActive(false);
            }

            _iconMap = _iconTable?.GroupBy(x => x.Type).ToDictionary(g => g.Key, g => g.Last().Icon)
                       ?? new Dictionary<CharacterType, Sprite>();
        }

        // ========== Animations ==========
        private async UniTask AnimateIntroTexts()
        {
            await ShowTextAnimation(_finishText);
            await ShowTextAnimation(_resultText);
        }

        private async UniTask ShowTextAnimation(TextMeshProUGUI target)
        {
            target.gameObject.SetActive(true);
            target.color = new Color(target.color.r, target.color.g, target.color.b, 0f);
            target.transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();
            seq.Append(target.DOFade(1f, _fadeInDuration))
               .Join(target.transform.DOScale(_scaleTarget, _scaleDuration).SetEase(_scaleEase))
               .AppendInterval(_holdDuration)
               .Append(target.DOFade(0f, _fadeOutDuration));

            await seq.AsyncWaitForCompletion();
        }

        private async UniTask AnimateBackground()
        {
            _resultBg.gameObject.SetActive(true);
            _resultBg.color = new Color(_resultBg.color.r, _resultBg.color.g, _resultBg.color.b, 0f);

            await _resultBg.DOFade(1f, _bgFadeDuration)
                .SetEase(_bgEase)
                .AsyncWaitForCompletion();
        }

        private async UniTask AnimateRows()
        {
            var db = PlayerDatabase.Instance;
            if (!db) return;

            // ネット同期済みの全員スコアで初期並びを作る（降順）
            var data = db.PlayerDataDic
                .Select(kv => kv.Value)
                .OrderByDescending(v => v.Score)
                .Select(d => (
                    name: d.DisplayNickName,
                    icon: _iconMap.GetValueOrDefault(d.CharacterType, _defaultIcon),
                    score: d.Score))
                .ToList();

            int rowCount = Mathf.Min(data.Count, _nameSlots.Length, _scoreSlots.Length, _rankSlots.Length, _iconSlots.Length);

            for (int i = 0; i < rowCount; i++)
            {
                var rowData = data[i];
                var nameLabel = _nameSlots[i];
                var scoreLabel = _scoreSlots[i];
                var rankLabel = _rankSlots[i];
                var iconImage = _iconSlots[i];

                var row = nameLabel.transform.parent?.gameObject ?? nameLabel.gameObject;
                row.SetActive(true);

                nameLabel.text = rowData.name;
                scoreLabel.text = "";
                rankLabel.text = "";
                iconImage.sprite = rowData.icon;

                var cg = row.GetComponent<CanvasGroup>() ?? row.AddComponent<CanvasGroup>();
                cg.alpha = 0f;

                var rt = row.transform as RectTransform;
                var endPos = rt.anchoredPosition;
                rt.anchoredPosition = endPos + new Vector2(-_rowSlideOffset, 0f);

                DOTween.Sequence()
                    .AppendInterval(_rowStagger * i)
                    .Append(cg.DOFade(1f, _rowFadeDuration))
                    .Join(rt.DOAnchorPos(endPos, _rowFadeDuration).SetEase(Ease.OutQuad));

                // スコアを 0 → 最終値へロールアップ
                await UniTask.Delay(TimeSpan.FromSeconds(_rowStagger * i));
                await RollupScore(scoreLabel, rowData.score, _scoreRollDuration, _scoreUseThousands);
            }

            await AnimateSortByScoreAsync(rowCount);
        }

        private async UniTask RollupScore(TextMeshProUGUI label, int target, float duration, bool useThousands)
        {
            label.DOKill();
            int val = 0;
            Tween t = DOTween.To(() => val, x =>
            {
                val = x;
                label.text = useThousands ? val.ToString("N0") : val.ToString();
            }, target, duration).SetEase(Ease.OutCubic);

            await t.AsyncWaitForCompletion();
        }

        private async UniTask AnimateSortByScoreAsync(int rowCount)
        {
            var rows = new List<(RectTransform rt, TextMeshProUGUI score, TextMeshProUGUI rank, int scoreVal)>();

            for (int i = 0; i < rowCount; i++)
            {
                var name = _nameSlots[i];
                if (!name) continue;
                var rt = name.transform.parent as RectTransform;
                if (!rt) continue;

                int.TryParse(_scoreSlots[i].text.Replace(",", ""), out var scoreVal);
                rows.Add((rt, _scoreSlots[i], _rankSlots[i], scoreVal));
            }

            if (rows.Count <= 1) return;

            // 現在のYレーンを記録
            float[] lanesY = rows.OrderByDescending(r => r.rt.position.y)
                                 .Select(r => r.rt.anchoredPosition.y).ToArray();

            // スコア降順で並び替え
            var sorted = rows.OrderByDescending(x => x.scoreVal).ToList();

            for (int newIndex = 0; newIndex < sorted.Count; newIndex++)
            {
                var row = sorted[newIndex];
                row.rank.text = (newIndex + 1).ToString();
                row.rank.transform.DOKill();
                row.rank.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 8, 0.8f);

                float targetY = lanesY[newIndex];
                row.rt.DOAnchorPosY(targetY, _sortMoveDuration)
                      .SetEase(Ease.InOutCubic)
                      .SetDelay(_sortStagger * newIndex);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_sortMoveDuration + _sortStagger * (sorted.Count - 1)));

            // 確定
            for (int i = 0; i < sorted.Count; i++)
                sorted[i].rt.SetSiblingIndex(i);
        }

        private async UniTask AnimateYourRank()
        {
            if (!_yourRankText) return;

            var db = PlayerDatabase.Instance;
            if (!db) return;

            string localName = db.PlayerDataDic.TryGet(db.Runner.LocalPlayer, out var d)
                ? d.DisplayNickName
                : null;
            
            var ranking = db.PlayerDataDic
                .Select(kv => kv.Value)
                .OrderByDescending(v => v.Score)
                .ToList();
            
            int rank = ranking.FindIndex(p => p.DisplayNickName == localName);

            _yourRankText.text = $"あなたの順位は {rank + 1} 位です";
            _yourRankText.gameObject.SetActive(true);

            var cg = _yourRankText.GetComponent<CanvasGroup>() ?? _yourRankText.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var rt = _yourRankText.rectTransform;
            var end = rt.anchoredPosition;
            rt.anchoredPosition = end + _yourRankOffset;

            await DOTween.Sequence()
                .Append(cg.DOFade(1f, _yourRankFadeDuration))
                .Join(rt.DOAnchorPos(end, _yourRankFadeDuration).SetEase(Ease.OutQuad))
                .Append(rt.DOPunchScale(Vector3.one * 0.08f, 0.2f, 8, 0.9f))
                .AsyncWaitForCompletion();
        }
    }
}
