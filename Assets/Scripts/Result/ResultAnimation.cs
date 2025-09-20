using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using September.Common;

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
        [SerializeField] private PageController _pageController;
        private ResultUIRootRefs _resultUIRoot;

        [Header("Delays")] [SerializeField] private float _startDelay = 0.5f;

        [Header("Text Animation")] [SerializeField]
        private float _fadeInDuration = 0.6f;

        [SerializeField] private float _scaleDuration = 0.6f;
        [SerializeField] private float _scaleTarget = 1.8f;
        [SerializeField] private Ease _scaleEase = Ease.OutBack;
        [SerializeField] private float _holdDuration = 1.0f;
        [SerializeField] private float _fadeOutDuration = 1.0f;

        [Header("Background Fade")] [SerializeField]
        private Ease _bgEase = Ease.Linear;

        [SerializeField] private float _bgFadeDuration = 1.0f;

        [Header("List Animation")] [SerializeField]
        private float _rowStagger = 0.1f;

        [SerializeField] private float _rowFadeDuration = 0.25f;
        [SerializeField] private float _rowSlideOffset = 40f;
        [SerializeField] private float _scoreRollDuration = 1.2f;
        [SerializeField] private bool _scoreUseThousands = true;

        [Header("Sort Animation")] [SerializeField]
        private float _sortMoveDuration = 0.5f;

        [SerializeField] private float _sortStagger = 0.06f;

        [Header("Your Rank")] [SerializeField] private float _yourRankFadeDuration = 0.35f;
        [SerializeField] private Vector2 _yourRankOffset = new(0f, 20f);

        [Header("Icons")] [SerializeField] private CharacterIconPair[] _iconTable;
        [SerializeField] private Sprite _defaultIcon;

        private TextMeshProUGUI _finishText;
        private TextMeshProUGUI _resultText;
        private ResultRowRefs[] _rows;
        private Image _resultBg;
        private RectTransform _rowsRoot;
        private TextMeshProUGUI _yourRankText;

        private Dictionary<CharacterType, Sprite> _iconMap;

        // === Entry ===
        public async UniTask Play(ResultUIRootRefs resultUIRoot)
        {
            _resultUIRoot = resultUIRoot;
            Initialize();

            await UniTask.Delay(TimeSpan.FromSeconds(_startDelay));

            await AnimateIntroTexts();
            await AnimateBackground();
            await AnimateRows();
            await AnimateYourRank();
            _pageController.Initialize();
        }

        private void Initialize()
        {
            _rows = _resultUIRoot.Rows;
            _finishText = _resultUIRoot.FinishText;
            _resultText = _resultUIRoot.ResultText;
            _resultBg = _resultUIRoot.ResultBg;
            _rowsRoot = _resultUIRoot.RowsRoot;
            _yourRankText = _resultUIRoot.YourRankText;

            _finishText.gameObject.SetActive(false);
            _resultText.gameObject.SetActive(false);
            _resultBg.gameObject.SetActive(false);
            _yourRankText.gameObject.SetActive(false);

            foreach (ResultRowRefs row in _rows)
            {
                if (row) row.gameObject.SetActive(false);
            }

            _iconMap = _iconTable?.GroupBy(x => x.Type).ToDictionary(g => g.Key, g => g.Last().Icon)
                       ?? new Dictionary<CharacterType, Sprite>();
        }

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
            PlayerDatabase db = PlayerDatabase.Instance;
            if (!db)
                return;
            
            List<(string DisplayNickName, Sprite Icon, int Score, bool IsOgre)> data = db.PlayerDataDic
                .Select(kv =>
                {
                    SessionPlayerData d = kv.Value;
                    return
                    (
                        d.DisplayNickName,
                        _iconMap.GetValueOrDefault(d.CharacterType, _defaultIcon),
                        d.Score,
                        (bool)d.IsOgre
                    );
                }).ToList();
            
            var ogres = data.Where(x => x.IsOgre).ToList();
            var nonOgres = data.Where(x => !x.IsOgre).OrderByDescending(x => x.Score).ToList();
            var orderedData = nonOgres.Concat(ogres).ToList();

            int rowCount = Mathf.Min(orderedData.Count, _rows.Length);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowsRoot);
            
            for (int i = 0; i < rowCount; i++)
            {
                (string name, Sprite icon, int score, bool isOgre) rowData = orderedData[i];
                ResultRowRefs row = _rows[i];
                row.gameObject.SetActive(true);

                row.Name.text = rowData.name;
                row.Score.text = "";
                row.Rank.text = "";
                row.Icon.sprite = rowData.icon;

                CanvasGroup cg = row.GetComponent<CanvasGroup>() ?? row.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;

                RectTransform rt = row.transform as RectTransform;
                if (rt)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_rowsRoot);

                    Vector2 endPos = rt.anchoredPosition;
                    rt.anchoredPosition = endPos + new Vector2(-_rowSlideOffset, 0f);

                    DOTween.Sequence()
                        .AppendInterval(_rowStagger * i)
                        .Append(cg.DOFade(1f, _rowFadeDuration))
                        .Join(rt.DOAnchorPos(endPos, _rowFadeDuration).SetEase(Ease.OutQuad));
                }
                
                await UniTask.Delay(TimeSpan.FromSeconds(_rowStagger * i));
                await RollupScore(row.Score, rowData.score, _scoreRollDuration, _scoreUseThousands);
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
            var rows =
                new List<(RectTransform rt, TextMeshProUGUI score, TextMeshProUGUI rank, int scoreVal, bool isOgre,
                    string name)>();

            PlayerDatabase db = PlayerDatabase.Instance;

            for (int i = 0; i < rowCount; i++)
            {
                ResultRowRefs row = _rows[i];
                if (!row || !row.Score)
                    continue;

                RectTransform rt = row.transform as RectTransform;
                if (!rt)
                    continue;

                int.TryParse(row.Score.text.Replace(",", ""), out int scoreVal);
                
                bool isOgre = false;
                string playerName = row.Name.text;
                SessionPlayerData playerData = db.PlayerDataDic.FirstOrDefault(kv => kv.Value.DisplayNickName == playerName).Value;
                isOgre = playerData.IsOgre;

                rows.Add((rt, row.Score, row.Rank, scoreVal, isOgre, playerName));
            }

            if (rows.Count <= 1)
                return;

            float[] lanesY = rows.OrderByDescending(r => r.rt.position.y)
                .Select(r => r.rt.anchoredPosition.y).ToArray();
            
            var sorted = rows
                .Where(x => !x.isOgre)
                .OrderByDescending(x => x.scoreVal)
                .Concat(rows.Where(x => x.isOgre))
                .ToList();

            
            for (int newIndex = 0; newIndex < sorted.Count; newIndex++)
            {
                (RectTransform rt, TextMeshProUGUI score, TextMeshProUGUI rank, int scoreVal, bool isOgre, string name) row = sorted[newIndex];
                
                row.rank.text = row.isOgre ? $"{sorted.Count}" : (newIndex + 1).ToString();

                row.rank.transform.DOKill();
                row.rank.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 8, 0.8f);

                float targetY = lanesY[newIndex];
                row.rt.DOAnchorPosY(targetY, _sortMoveDuration)
                    .SetEase(Ease.InOutCubic)
                    .SetDelay(_sortStagger * newIndex);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_sortMoveDuration + _sortStagger * (sorted.Count - 1)));

            for (int i = 0; i < sorted.Count; i++)
                sorted[i].rt.SetSiblingIndex(i);
        }

        private async UniTask AnimateYourRank()
        {
            if (!_yourRankText)
                return;

            PlayerDatabase db = PlayerDatabase.Instance;
            if (!db)
                return;

            string localName = db.PlayerDataDic.TryGet(db.Runner.LocalPlayer, out SessionPlayerData d)
                ? d.DisplayNickName
                : null;

            List<(string DisplayNickName, int Total, bool IsOgre)> ranking = db.PlayerDataDic
                .Select(kv =>
                {
                    SessionPlayerData data = kv.Value;
                    return (data.DisplayNickName, Total: data.Score, IsOgre: (bool)data.IsOgre);
                })
                .OrderBy(x => x.IsOgre)
                .ThenByDescending(x => x.Total)
                .ToList();

            int rank = ranking.FindIndex(p => p.DisplayNickName == localName);

            _yourRankText.text = $"あなたの順位は {rank + 1} 位です";
            _yourRankText.gameObject.SetActive(true);

            CanvasGroup cg = _yourRankText.GetComponent<CanvasGroup>() ??
                             _yourRankText.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            RectTransform rt = _yourRankText.rectTransform;
            Vector2 end = rt.anchoredPosition;
            rt.anchoredPosition = end + _yourRankOffset;

            await DOTween.Sequence()
                .Append(cg.DOFade(1f, _yourRankFadeDuration))
                .Join(rt.DOAnchorPos(end, _yourRankFadeDuration).SetEase(Ease.OutQuad))
                .Append(rt.DOPunchScale(Vector3.one * 0.08f, 0.2f, 8, 0.9f))
                .AsyncWaitForCompletion();
        }
    }
}