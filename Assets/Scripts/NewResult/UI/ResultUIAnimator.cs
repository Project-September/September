using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace September.NewResult
{
    public class ResultUIAnimator : MonoBehaviour
    {
        [Header("Winner UI")]
        [SerializeField] private Animator _winnerUIAnimator;
        [SerializeField] private string _winnerUIStateName;
        
        [Header("Menu UI")]
        [SerializeField] private Animator _menuUIAnimator;
        [SerializeField] private string _menuUIStateName;

        [Header("Ranking UI Animation")]
        [SerializeField] private RankingUIAnimator.RankingUIAnimatorContext _rankingUIAnimatorContext;
        
        public async UniTask ShowWinner()
        {
            await _winnerUIAnimator.PlayAsync(_winnerUIStateName);
        }

        public async UniTask ShowMenu()
        {
            await _menuUIAnimator.PlayAsync(_menuUIStateName);
        }

        public async UniTask ShowRankingItems()
        {
            await new RankingUIAnimator(_rankingUIAnimatorContext).ShowRankingItems();
        }
        
        private void Start()
        {
            new RankingUIAnimator(_rankingUIAnimatorContext).InitializeItems().Forget();
        }
    }

    public class RankingUIAnimator
    {
        [System.Serializable]
        public class RankingUIAnimatorContext
        {
            [SerializeField] private Transform _rankingRoot;
            [SerializeField] private float _positionXOffset;
            [SerializeField] private float _rankingInDelaySeconds = 0.05f;
            [SerializeField] private float _rankingInDuration = 0.1f;
            [SerializeField] private Ease _easeType = Ease.Linear;
            
            public Transform RankingRoot => _rankingRoot;
            public float PositionXOffset => _positionXOffset;
            public float RankingInDelaySeconds => _rankingInDelaySeconds;
            public float RankingInDuration => _rankingInDuration;
            public Ease EaseType => _easeType;
        }
        
        private readonly RankingUIAnimatorContext _context;

        public RankingUIAnimator(RankingUIAnimatorContext context)
        {
            _context = context;
        }

        public async UniTask ShowRankingItems()
        {
            var items = _context.RankingRoot.GetComponentsInChildren<RankingItemView>();
            for (int i = 0; i < items.Length; i++)
            {
                var rectTransform = items[i].GetComponent<RectTransform>();
                var endX = rectTransform.anchoredPosition.x - _context.PositionXOffset;
                var item = AnimationRankingItem(rectTransform, endX, _context.RankingInDuration, i * _context.RankingInDelaySeconds);
                if (i == items.Length - 1) await item;
            }

            return;

            async UniTask AnimationRankingItem(RectTransform rectTransform, float endX, float duration,
                float delaySeconds)
            {
                await UniTask.Delay((int)(delaySeconds * 1000));
                await rectTransform.DOAnchorPosX(endX, duration).SetEase(_context.EaseType);
            }
        }

        public async UniTask InitializeItems()
        {
            await UniTask.DelayFrame(1);
            var items = _context.RankingRoot.GetComponentsInChildren<RankingItemView>();

            foreach (var item in items)
            {
                InitRankingItem(item);
            }

            _context.RankingRoot.GetComponent<VerticalLayoutGroup>().enabled = false;
        }
        
        private void InitRankingItem(RankingItemView item)
        {
            var r = item.GetComponent<RectTransform>();
            var p = r.anchoredPosition;
            p.x += _context.PositionXOffset;
            r.anchoredPosition = p;
        }
    }
}