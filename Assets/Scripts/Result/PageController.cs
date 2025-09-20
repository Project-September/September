using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
using NaughtyAttributes;
using September.Common;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Result
{
    public class PageController : MonoBehaviour
    {
        [Header("Anim Settings")]
        [SerializeField] private float _slideDuration = 0.45f;
        [SerializeField] private float _pageGap = 40f;
        [SerializeField] private Ease _ease = Ease.OutCubic;

        private GameInput _gameInput;
        private readonly Stack<RectTransform> _stack = new();
        private bool _isActive;
        private bool _isAnimating;
        private float _canvasWidth;

        [Header("Pages")]
        [SerializeField] private RectTransform[] _pages;

        [Header("2ページ目: スタン")]
        [SerializeField] private Transform _stunRowRoot;
        [SerializeField] private TextMeshProUGUI _stunTotalText;
        [SerializeField,Label("スタンさせたときのスコア")] private int StunPoint = 150;

        [Header("3ページ目　インタラクト")]
        [SerializeField] private Transform _exhibitRowRoot;
        [SerializeField] private GameObject _exhibitRowPrefab;
        [SerializeField] private TextMeshProUGUI _exhibitTotalText;
        [SerializeField] private ExhibitScoreConfig _exhibitScoreConfig;
        
        [Header("4ページ目: アビリティボーナス")]
        [SerializeField] private Transform _abilityRowRoot;
        [SerializeField] private GameObject _abilityRowPrefab;
        [SerializeField] private TextMeshProUGUI _abilityTitle;
        [SerializeField] private TextMeshProUGUI _abilityTotalText;
        
        private ResultDataInbox _resultDataInbox;
        private bool _isFinish;

        private void Awake()
        {
            _isFinish = false;
        }

        private void Update()
        {
            if (!_isActive || _isAnimating) 
                return;

            // リザルト画面に以降する
            if (_isFinish && _gameInput.Result.Finish.triggered)
            {
                SceneManager.UnloadSceneAsync("Field");
                SceneManager.LoadSceneAsync("Result", LoadSceneMode.Single);
            }

            if (_gameInput.UI.PageSlide.triggered)
            {
                RectTransform next = GetNextPage();
                if (next)
                {
                    next.gameObject.SetActive(true);
                    PushAsync(next).Forget();
                }
            }

            if (!_gameInput.UI.PageSlideBack.triggered && !_gameInput.UI.Cancel.triggered)
                return;
            
            if (_stack.Count > 1)
                PopAsync().Forget();
        }

        public void Initialize()
        {
            _gameInput = GameInput.I;
            _gameInput.Enable();
            _isActive = true;
            _resultDataInbox = ResultDataInbox.I;
            
            _resultDataInbox.OnChanged += SetExhibitPage;

            RectTransform rootRt = (RectTransform)transform;
            _canvasWidth = rootRt.rect.width;
            _gameInput.ToggleMoveInput(false);

            for (int i = 0; i < _pages.Length; i++)
            {
                RectTransform p = _pages[i];
                if (!p) 
                    continue;

                EnsureCanvasGroup(p).alpha = i == 0 ? 1f : 0f;
                p.gameObject.SetActive(true);
                p.anchoredPosition = i == 0 ? Vector2.zero : OffRight();
            }

            if (_pages.Length > 0)
                _stack.Push(_pages[0]);
            
            SetStunPage();
            SetExhibitPage();
            SetAbilityBonusPage();
            _isFinish = true;
        }

        /// <summary>
        /// 2ページ目: 自分が気絶させた相手一覧と合計
        /// </summary>
        private void SetStunPage()
        {
            PlayerDatabase db = PlayerDatabase.Instance;
            if (!db)
                return;

            if (!db.PlayerDataDic.TryGet(db.Runner.LocalPlayer, out SessionPlayerData localData))
            {
                Debug.LogWarning("[SetStunPage] Local player data not found");
                return;
            }
            
            int totalScore = 0;

            int i = 0;
            foreach (var kv in localData.StunData)
            {
                PlayerRef targetRef = kv.Key;
                int count = kv.Value;

                string targetName = db.PlayerDataDic.TryGet(targetRef, out SessionPlayerData targetData)
                    ? targetData.DisplayNickName
                    : $"Player {targetRef.RawEncoded}";

                int score = count * StunPoint;

                // rowRoot に並んでる子オブジェクトを使う
                if (i < _stunRowRoot.childCount)
                {
                    Transform row = _stunRowRoot.GetChild(i);
                    row.gameObject.SetActive(true);

                    TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);
                    if (texts.Length >= 3)
                    {
                        texts[0].text = targetName;
                        texts[1].text = $"{count}回";
                        texts[2].text = $"{score}点";
                    }
                    else
                        Debug.LogError("[SetStunPage] Row に Text が3つ無い");
                }
                
                totalScore += score;
                i++;
            }

            // 余った Row は非表示に
            for (; i < _stunRowRoot.childCount; i++)
            {
                _stunRowRoot.GetChild(i).gameObject.SetActive(false);
            }

            if (_stunTotalText)
                _stunTotalText.text = totalScore.ToString();
        }
        
        private void SetExhibitPage()
        {
            // 既存の行を消去
            foreach (Transform child in _exhibitRowRoot)
                Destroy(child.gameObject);
            
            Debug.Assert(_resultDataInbox);
            
            int totalScore = 0;
            
            // インタラクトできる種類とスコアを取得
            foreach (ExhibitScoreEntry entry in _exhibitScoreConfig.Entries)
            {
                // 展示物の種類　例　プテラ
                ExhibitType type = entry.Type;
                // 登録した種類
                int point = entry.Points;
                // 何回インタラクトしたか
                int count = _resultDataInbox.ExhibitCounts.GetValueOrDefault(type, 0);
                // スコアとインタラクト回数を乗算
                int score = count * point;

                GameObject row = Instantiate(_exhibitRowPrefab, _exhibitRowRoot);
                TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);

                if (texts.Length >= 3)
                {
                    texts[0].text = type.ToDisplayName();  
                    texts[1].text = $"×{count}";
                    texts[2].text = score.ToString();
                }
                else
                    Debug.LogError("[SetExhibitPage] Prefab に Text が3つ無い");
                
                totalScore += score;
            }

            if (_exhibitTotalText)
                _exhibitTotalText.text = totalScore.ToString();
        }
        
        private void SetAbilityBonusPage()
        {
            foreach (Transform child in _abilityRowRoot)
                Destroy(child.gameObject);

            ResultDataInbox inbox = ResultDataInbox.I;
            if (!inbox)
                return;

            PlayerDatabase db = PlayerDatabase.Instance;
            if (!db.PlayerDataDic.TryGet(db.Runner.LocalPlayer, out SessionPlayerData localData))
            {
                Debug.LogWarning("[SetAbilityBonusPage] Local player data not found");
                return;
            }

            CharacterType chara = localData.CharacterType;

            // AbilityBonusContainer に「描画付き」メソッドを用意して呼ぶ
            int totalScore = AbilityBonusContainer.Render(chara, inbox, _abilityRowRoot, _abilityRowPrefab, _abilityTitle);

            if (_abilityTotalText)
                _abilityTotalText.text = totalScore.ToString();
        }

        
        // ページ遷移制御
        private RectTransform GetNextPage()
        {
            if (_pages == null || _pages.Length == 0) 
                return null;
            RectTransform top = _stack.Peek();
            int idx = Array.IndexOf(_pages, top);
            int next = Mathf.Clamp(idx + 1, 0, _pages.Length - 1);
            
            return next == idx ? null : _pages[next];
        }

        private async UniTask PushAsync(RectTransform nextPage, bool first = false)
        {
            if (_isAnimating)
                return;
            _isAnimating = true;

            RectTransform current = _stack.Count > 0 ? _stack.Peek() : null;
            CanvasGroup nextCg = EnsureCanvasGroup(nextPage);
            nextCg.alpha = 0f;
            nextPage.anchoredPosition = OffRight();

            Sequence seq = DOTween.Sequence();

            if (current && !first)
            {
                CanvasGroup curCg = EnsureCanvasGroup(current);
                seq.Join(current.DOAnchorPos(OffLeft(), _slideDuration).SetEase(_ease));
                seq.Join(curCg.DOFade(0f, _slideDuration));
            }

            seq.Append(nextPage.DOAnchorPos(Vector2.zero, _slideDuration).SetEase(_ease));
            seq.Join(nextCg.DOFade(1f, _slideDuration));

            await seq.AsyncWaitForCompletion();

            if (current && !first)
            {
                EnsureCanvasGroup(current).alpha = 0f;
                current.anchoredPosition = OffLeft();
            }

            _stack.Push(nextPage);
            _isAnimating = false;
        }

        private async UniTask PopAsync()
        {
            if (_isAnimating) 
                return;
            _isAnimating = true;

            RectTransform current = _stack.Pop();
            RectTransform previous = _stack.Peek();

            CanvasGroup curCg = EnsureCanvasGroup(current);
            CanvasGroup prevCg = EnsureCanvasGroup(previous);

            prevCg.alpha = 1f;
            previous.anchoredPosition = OffLeft();

            Sequence seq = DOTween.Sequence()
                .Join(current.DOAnchorPos(OffRight(), _slideDuration).SetEase(_ease))
                .Join(curCg.DOFade(0f, _slideDuration))
                .Join(previous.DOAnchorPos(Vector2.zero, _slideDuration).SetEase(_ease));

            await seq.AsyncWaitForCompletion();

            curCg.alpha = 0f;
            current.anchoredPosition = OffRight();
            _isAnimating = false;
        }
        
        private Vector2 OffRight() => new(_canvasWidth * 1.05f + _pageGap, 0f);
        private Vector2 OffLeft() => new(-_canvasWidth * 1.05f - _pageGap, 0f);

        private CanvasGroup EnsureCanvasGroup(RectTransform rt)
        {
            if (!rt.TryGetComponent(out CanvasGroup cg) || !cg)
                cg = rt.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }
    }
}
